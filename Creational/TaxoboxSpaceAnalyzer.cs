using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using NLog;
using NLog.Targets;
using System.Data;
using System.Linq;

namespace Creational;

public class TaxoboxSpaceAnalyzer
{
    static Logger log = LogManager.GetCurrentClassLogger();

    private readonly IDbContextFactory<ApplicationDb> dbContextFactory;
    private readonly HeuristicTaxoboxParser taxoboxParser;

    public TaxoboxSpaceAnalyzer(IDbContextFactory<ApplicationDb> dbContextFactory)
    {
        this.dbContextFactory = dbContextFactory;

        taxoboxParser = new HeuristicTaxoboxParser();
    }

    public class PageInfo
    {
        public WikiPage Page { get; set; }

        public String Name { get; set; }

        public PageInfo Ancestor { get; set; }

        public Int32 AncestorEntryNo { get; set; }

        public PageInfo RootAncestor { get; set; }

        public Int32 RootAncestorCalculationPhase { get; set; }

        public List<String> Names { get; } = new List<String>();

        public WikiTaxobox Taxobox => Page?.Taxobox;

        public ParsingResult Result => Page?.Parsed;

        public String Issue { get; set; }

        public void AddIssue(String issue)
        {
            if (Issue is null)
            {
                Issue = issue;
            }
            else
            {
                Issue += "; " + issue;
            }
        }

        public override String ToString()
        {
            return $"{Page.Title} [{String.Join("; ", Names)}]";
        }
    }

    public record TaxoTemplateTree(TaxoTemplateTreeNode Root, Int32 NoOfDescendants);

    public class TaxoTemplateTreeNode
    {
        public static String Prefix = "Template:Taxonomy/";

        public Int32 Level { get; set; }

        public Boolean IsInitialized { get; set; }

        public TaxoTemplateTreeNode Parent { get; set; }

        public TaxoTemplateTreeNode Root { get; set; }

        public String MissingParentName { get; set; }

        public Int32 NoOfDescendants { get; set; }

        public Boolean HasProperTitle { get; }

        public String NameFromTitle { get; }

        public Boolean IsAlias => Values.SameAs is not null;

        public TaxoTemplateValues Values { get; }

        public List<TaxoTemplateTreeNode> Nodes { get; }

        public static String ExtractProperNameOrNot(String templateTitle)
        {
            if (templateTitle.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            {
                return templateTitle.Substring(Prefix.Length)/*.ToLowerInvariant()*/;
            }
            else
            {
                return null;
            }
        }

        public TaxoTemplateTreeNode(TaxoTemplateValues values)
        {
            Nodes = new List<TaxoTemplateTreeNode>();
            Values = values;

            if (ExtractProperNameOrNot(values.Title) is String nameFromTitle)
            {
                NameFromTitle = nameFromTitle;
                HasProperTitle = true;
            }
            else
            {
                NameFromTitle = values.Title;
            }
        }

        public IEnumerable<String> AllNames
        {
            get
            {
                yield return NameFromTitle;
                yield return Values.Name;
                yield return Values.Title;
                yield return Values.PageTitle;
            }
        }

        String OptionalParentSuffix => MissingParentName is not null ? $", missing parent '{MissingParentName}'" : "";
        String DescendantsSummary => NoOfDescendants > 0 ? NoOfDescendants.ToString() : "leaf";

        public override String ToString()
        {
            return $"{NameFromTitle}, {DescendantsSummary}{OptionalParentSuffix}";
        }
    }

    public void AnalyzeTaxoTemplates(String lang)
    {
        log.Info($"Analyzing TaxoTemplates");

        var db = dbContextFactory.CreateDbContext();

        var allKnownTaxoTemplatePages = db.Pages
            .Where(t => t.Lang == lang)
            .Where(t => t.Title.StartsWith("Template:Taxonomy/"))
            .ToArray()
            ;

        var allKnownTaxoTemplateNamesWithErrors = (
            from p in allKnownTaxoTemplatePages
            let name = TaxoTemplateTreeNode.ExtractProperNameOrNot(p.Title)
            let error = p.Step < 0 ? p.StepError : null
            where name is not null
            select (name, error)
        ).ToDictionary(e => e.name, e => e.error);

        var taxoTemplateValues = db.TaxoTemplateValues
            .Where(t => t.Lang == lang)
            .ToArray();

        var titleToTaxoTemplateNode = taxoTemplateValues
            .Select(v => new TaxoTemplateTreeNode(v))
            .ToDictionary(n => n.NameFromTitle);

        TaxoTemplateTreeNode GetNode(String title)
        {
            if (titleToTaxoTemplateNode.TryGetValue(title, out var node))
            {
                if (node.Values.SameAs is String sameAs)
                {
                    return GetNode(sameAs);
                }
                else
                {
                    return node;
                }
            }
            else
            {
                return null;
            }
        }

        var roots = new List<TaxoTemplateTreeNode>();

        var stack = new Stack<TaxoTemplateTreeNode>();

        void Initialize(TaxoTemplateTreeNode node)
        {
            if (node.IsInitialized) return;

            if (node.IsAlias) throw new Exception("Aliases should never be initialized");

            node.IsInitialized = true;

            var parentTitle = node.Values.Parent/*?.ToLowerInvariant()*/;

            if (parentTitle is null)
            {
                roots.Add(node);
            }
            else if (GetNode(parentTitle) is not TaxoTemplateTreeNode parentNode)
            {
                roots.Add(node);
                node.MissingParentName = parentTitle;
            }
            else
            {
                if (stack.Contains(parentNode))
                {
                    throw new Exception($"Circular reference found ('{node.NameFromTitle}' and '{parentNode.NameFromTitle}')");
                }

                node.Parent = parentNode;
                parentNode.Nodes.Add(node);

                stack.Push(node);

                Initialize(parentNode);

                stack.Pop();

                node.Root = parentNode.Root;
                node.Level = parentNode.Level + 1;
            }
        }

        foreach (var node in titleToTaxoTemplateNode.Values)
        {
            if (node.Values.SameAs is not null) continue;

            Initialize(node);
        }

        if (stack.Count > 0) throw new Exception($"Unexpected stack size");

        void SetNumberOfDescendants(TaxoTemplateTreeNode node)
        {
            node.NoOfDescendants = node.Nodes.Count;

            if (stack.Contains(node)) throw new Exception($"Unexpected cycle");

            stack.Push(node);

            foreach (var child in node.Nodes)
            {
                SetNumberOfDescendants(child);

                node.NoOfDescendants += child.NoOfDescendants;
            }

            stack.Pop();
        }

        foreach (var root in roots)
        {
            SetNumberOfDescendants(root);
        }

        IEnumerable<(TaxoTemplateTreeNode node, String name)> FindNames(String name)
        {
            foreach (var node in titleToTaxoTemplateNode.Values)
            {
                foreach (var nodeName in node.AllNames)
                {
                    if (nodeName is null) continue;

                    if (nodeName.Contains(name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        yield return (node, nodeName);
                    }
                }
            }
        }

        var firstRoots = roots.OrderByDescending(r => r.NoOfDescendants).Take(100).ToArray();



        for (var i = 0; i < firstRoots.Length; ++i)
        {
            var root = firstRoots[i];

            if (i == firstRoots.Length - 1)
            {
                log.Info("and more...");
            }
            else
            {
                log.Info(" - {item}", root);

                if (root.MissingParentName is String mpn)
                {
                    if (allKnownTaxoTemplateNamesWithErrors.TryGetValue(mpn, out var error))
                    {
                        log.Info("   have an unparsed parent candidate ({error})", error);
                    }
                    else
                    {
                        var (candidate, candidateName) = FindNames(mpn).FirstOrDefault();

                        if (candidate is null)
                        {
                            log.Info("   no parent candidate");
                        }
                        else
                        {
                            log.Info("   - parent candidate: {candidate}", candidate);
                        }
                    }
                }
            }
        }
    }

    (String genus, String species, String error) SplitTaxon(String taxon)
    {
        var parts = taxon
            .Split(' ')
            .Where(p => !String.IsNullOrWhiteSpace(p))
            .ToArray();

        if (parts.Length == 1)
        {
            return (parts[0], parts[0], null);
        }
        else if (parts.Length == 2)
        {
            return (parts[0], parts[1], null);
        }
        else
        {
            return (parts[0], String.Join(" ", parts[1..]), null);
        }
    }

    public void AnalyzeSpecies(String lang)
    {
        var db = dbContextFactory.CreateDbContext();

        log.Info("AnalyzeSpecies loading data");

        var allKnownTaxoTemplatePages = db.Pages
            .Include(p => p.Parsed)
                .ThenInclude(r => r.TaxoTemplateValues)
            .Where(t => t.Lang == lang)
            .Where(t => t.Title.StartsWith("Template:Taxonomy/"))
            .ToArray()
            ;

        var allKnownTaxoTemplatePagesByName = (
            from p in allKnownTaxoTemplatePages
            let name = TaxoTemplateTreeNode.ExtractProperNameOrNot(p.Title)
            where name is not null
            select (name, p)
        ).ToDictionary(p => p.name, p => p.p);

        var parsedSpecies = db.ParsingResults
            .Where(r => r.Page.Step >= Step.Finished && r.Page.Lang == lang && r.Page.Type == PageType.Content && r.TemplateName == "speciesbox")
            .ToArray()
            ;

        (WikiPage template, String error) FindTaxoTemplateByName(String name, String error)
        {
            if (allKnownTaxoTemplatePagesByName.TryGetValue(name, out var template))
            {
                if (template?.Parsed?.TaxoTemplateValues is TaxoTemplateValues values)
                {
                    return (template, null);
                }
                else
                {
                    return (null, "matched template wasn't parsed");
                }
            }
            else
            {
                return (null, error);
            }
        }

        (WikiPage template, String error) FindTaxoTemplate(ParsingResult parsed)
        {
            if (parsed.Taxon is String taxon && !String.IsNullOrWhiteSpace(taxon))
            {
                var (genus, species, error) = SplitTaxon(taxon);

                if (genus is not null)
                {
                    return FindTaxoTemplateByName(genus, "taxon not found");
                }
                else
                {
                    return (null, error ?? "unknown error");
                }
            }
            else if (parsed.Genus is String genus && !String.IsNullOrWhiteSpace(genus))
            {
                return FindTaxoTemplateByName(genus, "genus not found");
            }
            else
            {
                return (null, "neither parent nor genus set");
            }
        }

        log.Info("Matching species to templates:");

        var groupedByError = (
            from s in parsedSpecies
            let p = FindTaxoTemplate(s)
            group s by p.error into g
            select new
            {
                Error = g.Key,
                Species = g.ToArray()
            }
        ).ToArray();

        foreach (var grp in groupedByError)
        {
            log.Info(" - {count}: {error}; eg. {example}", grp.Species.Length, grp.Error ?? "fine", grp.Species[0].Title);
        }

    }

    public void Analyze(String lang)
    {
        var db = dbContextFactory.CreateDbContext();

        log.Info("Analyze loading data");

        var redirections = (
            from p in db.Pages
            where p.Lang == lang && p.Type == PageType.Redirect
            join c in db.PageContents on new { p.Lang, p.Title } equals new { c.Lang, c.Title } into cs
            from c in cs
            select new { p.Title, c.RedirectTitle }
        ).ToDictionary(p => p.Title, p => p.RedirectTitle);

        log.Info("Redirections loaded");

        var pageQuery = db.Pages
            .Where(p => p.Lang == lang && p.Type == PageType.Content);

        var pages = pageQuery
            .Include(p => p.Parsed)
                .ThenInclude(p => p.TaxonomyEntries)
            .ToArray();

        //pageQuery.Include(p => p.Taxobox).ToArray();
        //pageQuery.Include(p => p.Parsed).ThenInclude(p => p.TaxoboxEntries).ToArray();

        log.Info("Taxonomy loaded");

        var pageInfosByTitle = new Dictionary<String, PageInfo>();
        var pagesBySomeName = new Dictionary<String, PageInfo>();

        PageInfo GetPageInfo(WikiPage page)
        {
            var title = page.Title;
            if (!pageInfosByTitle.TryGetValue(title, out var pageInfo))
            {
                pageInfosByTitle[title] = pageInfo = new PageInfo { Page = page };
            }
            return pageInfo;
        }

        void AddPage(PageInfo page, String name)
        {
            if (String.IsNullOrWhiteSpace(name)) return;

            if (pagesBySomeName.TryGetValue(name, out var previous))
            {
                if (page != previous)
                {
                    page.Issue = "duplicate name";
                }
            }
            else
            {
                pagesBySomeName[name] = page;

                if (name != page.Page.Title)
                {
                    page.Names.Add(name);
                }
            }
        }

        foreach (var page in pages)
        {
            var pageInfo = GetPageInfo(page);

            AddPage(pageInfo, page.Title);

            if (page.Parsed?.Exception is String parsingException)
            {
                pageInfo.Issue = $"parsing: {parsingException}";

                continue;
            }

            if (page.Parsed?.TaxonomyEntries is not ICollection<TaxonomyEntry> entries)
            {
                pageInfo.Issue = "no taxonomy entries";

                continue;
            }

            var mainEntry = entries.FirstOrDefault(e => e.No == 1);

            if (mainEntry is not null)
            {
                AddPage(pageInfo, mainEntry.Name);
                AddPage(pageInfo, mainEntry.NameLocal);

                if (!String.IsNullOrWhiteSpace(mainEntry.Name))
                {
                    pageInfo.Name = mainEntry.Name;
                }
            }
            else
            {
                pageInfo.Issue = "no main taxonomy entry";
            }
        }

        foreach (var pageInfo in pageInfosByTitle.Values)
        {
            var ancestors = pageInfo.Result?.TaxonomyEntries
                ?.Where(e => e.No > 1)
                .OrderBy(e => e.No)
                .ToArray();

            if (ancestors is not null)
            {
                foreach (var entry in ancestors)
                {
                    PageInfo ancestor = null;
                    if (pagesBySomeName.TryGetValue(entry.Name ?? "", out ancestor) ||
                        pagesBySomeName.TryGetValue(entry.NameLocal ?? "", out ancestor))
                    {
                        if (ancestor == pageInfo)
                        {
                            pageInfo.Issue = "has itself as ancestor";

                            break;
                        }

                        pageInfo.Ancestor = ancestor;
                        pageInfo.AncestorEntryNo = entry.No;

                        break;
                    }
                }

                if (pageInfo.Ancestor == null)
                {
                    pageInfo.AddIssue("no ancestor page match");
                }
            }
        }

        var stack = new Stack<PageInfo>();

        PageInfo GetRootAncestor(PageInfo pageInfo)
        {
            if (pageInfo.RootAncestorCalculationPhase == 2) return pageInfo.RootAncestor;

            if (pageInfo.Ancestor is null) return null;

            if (pageInfo.RootAncestorCalculationPhase == 1)
            {
                var stackText = String.Join(" > ", stack.Select(p => p.Page.Title));

                pageInfo.AddIssue("cycle in hierarchy");

                pageInfo.RootAncestorCalculationPhase = 2;
            }
            else
            {

                pageInfo.RootAncestorCalculationPhase = 1;

                stack.Push(pageInfo);

                try
                {
                    var parentRootAncestor = GetRootAncestor(pageInfo.Ancestor);

                    pageInfo.RootAncestor = parentRootAncestor ?? pageInfo.Ancestor;
                }
                finally
                {
                    stack.Pop();
                }
            }

            pageInfo.RootAncestorCalculationPhase = 2;

            return pageInfo.RootAncestor;
        }

        foreach (var pageInfo in pageInfosByTitle.Values)
        {
            if (pageInfo.Ancestor is null) continue;

            var rootAncestor = GetRootAncestor(pageInfo);

            if (rootAncestor is null) throw new Exception();

            if (rootAncestor.Page.Title != "Lebewesen")
            {
                pageInfo.AddIssue($"root ancestor was no 'Lebewesen'");
            }
        }

        foreach (var pageInfo in pageInfosByTitle.Values)
        {
            if (pageInfo.Issue != null) continue;

            if (pageInfo.Name is null)
            {
                pageInfo.Issue = "no scientific name";
            }
        }

        var pageIssueGroupReport = String.Join("\n ",
            from p in pageInfosByTitle.Values
            group p by p.Issue into g
            select $"{g.Count():d}: {g.Key ?? "(fine)"}, eg. '{g.First()}'"
        );

        log.Info($"Page issue report:\n\n {pageIssueGroupReport}\n\n");

        var rootAncestorReport = String.Join("\n ",
            from p in pageInfosByTitle.Values
            let rootAncestor = p.RootAncestor?.Page
            group p by rootAncestor into g
            orderby g.Count() descending
            select $"{g.Count():d}: {g.Key?.Title ?? "(none)"}, eg. '{g.First()}'"
        );

        log.Info($"Root ancestor report:\n\n {rootAncestorReport}\n\n");

        var relations = new List<TaxonomyRelation>();

        foreach (var pageInfo in pageInfosByTitle.Values)
        {
            if (pageInfo.Ancestor is null) continue;

            relations.Add(new TaxonomyRelation
            {
                Lang = pageInfo.Page.Lang,
                Descendant = pageInfo.Page.Title,
                Ancestor = pageInfo.Ancestor.Page.Title,
                No = pageInfo.AncestorEntryNo
            });
        }

        var data = relations.ToDataTable();

        log.Info("Relations fed into a data table, writing it out");

        var connection = (SqlConnection)db.Database.GetDbConnection();

        connection.Open();

        using var transaction = connection.BeginTransaction();

        using var command = connection.CreateCommand();

        command.CommandText = $"delete from {nameof(ApplicationDb.TaxonomyRelations)} where lang = '{lang}'";
        command.Transaction = transaction;
        command.ExecuteNonQuery();

        using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction);

        bulkCopy.DestinationTableName = nameof(ApplicationDb.TaxonomyRelations);

        foreach (var col in new[] {
            nameof(TaxonomyRelation.Lang),
            nameof(TaxonomyRelation.Descendant),
            nameof(TaxonomyRelation.Ancestor),
            nameof(TaxonomyRelation.No)
        })
        {
            bulkCopy.ColumnMappings.Add(new SqlBulkCopyColumnMapping(col, col));
        }

        bulkCopy.WriteToServer(data);

        transaction.Commit();

        log.Info("Relations written");
    }
}
