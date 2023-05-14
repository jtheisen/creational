using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Creational.TaxoboxParser;

namespace Creational;

public class TaxoboxSpaceAnalyzer
{
    static Logger log = LogManager.GetCurrentClassLogger();

    private readonly IDbContextFactory<ApplicationDb> dbContextFactory;
    private readonly TaxoboxParser taxoboxParser;

    public TaxoboxSpaceAnalyzer(IDbContextFactory<ApplicationDb> dbContextFactory)
    {
        this.dbContextFactory = dbContextFactory;

        taxoboxParser = new TaxoboxParser();
    }

    public class PageInfo
    {
        public WikiPage Page { get; set; }

        public String Name { get; set; }

        public PageInfo Ancestor { get; set; }

        public PageInfo RootAncestor { get; set; }

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

    public void Analyze()
    {
        var db = dbContextFactory.CreateDbContext();

        log.Info("Analyze loading data");

        var redirections = (
            from p in db.Pages
            where p.Type == PageType.Redirect
            join c in db.PageContents on p.Title equals c.Title into cs
            from c in cs
            select new { p.Title, c.RedirectTitle }
        ).ToDictionary(p => p.Title, p => p.RedirectTitle);

        log.Info("Redirections loaded");

        var pages = db.Pages
            .Include(p => p.Parsed)
            .ThenInclude(p => p.TaxonomyEntries)
            .Where(p => p.Type == PageType.Content)
            .ToArray();

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

            if (page.Parsed?.TaxonomyEntries is not ICollection<TaxonomyEntry> entries)
            {
                pageInfo.Issue = "no taxonomy entries";

                continue;
            }

            var mainEntry = entries.FirstOrDefault(e => e.No == 1);

            if (mainEntry is not null)
            {
                AddPage(pageInfo, mainEntry.Name);
                AddPage(pageInfo, mainEntry.NameDe);

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
            var ancestors = pageInfo.Result?.TaxonomyEntries?.Where(e => e.No > 1).Reverse();

            if (ancestors is not null)
            {
                foreach (var entry in ancestors)
                {
                    PageInfo ancestor = null;
                    if (pagesBySomeName.TryGetValue(entry.Name ?? "", out ancestor) ||
                        pagesBySomeName.TryGetValue(entry.NameDe ?? "", out ancestor))
                    {
                        if (ancestor == pageInfo)
                        {
                            pageInfo.Issue = "has itself as ancestor";

                            break;
                        }

                        pageInfo.Ancestor = ancestor;
                    }
                }

                if (pageInfo.Ancestor == null)
                {
                    pageInfo.AddIssue("no ancestor page match");
                }
            }
        }

        PageInfo GetRootAncestor(PageInfo page)
        {
            if (page.RootAncestor is not null) return page.RootAncestor;

            if (page.Ancestor is null) return null;

            return page.RootAncestor = GetRootAncestor(page.Ancestor) ?? page.Ancestor;
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
    }
}
