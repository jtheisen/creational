using ICSharpCode.SharpZipLib.BZip2;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NLog;
using System.Xml;

namespace Creational;

public class WikiDumpImporter
{
    static Logger log = LogManager.GetCurrentClassLogger();

    private readonly IDbContextFactory<ApplicationDb> dbFactory;
    private readonly RedirectParser redirectParser;

    public WikiDumpImporter(IDbContextFactory<ApplicationDb> dbFactory)
    {
        this.dbFactory = dbFactory;

        redirectParser = new RedirectParser();
    }

    public void ImportTest(String fileName)
    {
        using var zippedStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        using var xmlStream = new BZip2InputStream(zippedStream);

        var xmlReader = XmlReader.Create(xmlStream);

        var elements = xmlReader.StreamElements();
        
        foreach (var element in elements.Take(15))
        {
            Console.WriteLine($"{element.Title} | {xmlStream.Position}");
        }
    }

    static String TaxoTemplatePrefix = "Template:Taxonomy/";

    public void Import(
        String fileName, String lang, Int32 skip = 0, Boolean dryRun = false, PageType? updateOnly = null, TextWriter titlesWriter = null)
    {
        var fileLength = new FileInfo(fileName).Length;

        using var zippedStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        using var xmlStream = new BZip2InputStream(zippedStream);

        var xmlReader = XmlReader.Create(xmlStream);

        var elements = xmlReader.StreamElements();

        WikiPage GetPage(XPage element)
        {
            var title = element.Title.TruncateUtf8(200);
            var rev = element.Revision;
            var text = rev.Text;

            // There are some incorrectly cased entries which can
            // hopefully all be ignored.

            var hasCaseIssue = title.Length > 0 && Char.IsLower(title[0]);

            var isParaHoxozoaException =
                title.Equals("Template:Taxonomy/Parahoxozoa");

            var isTaxoTemplate =
                title.Length > TaxoTemplatePrefix.Length &&
                title.StartsWith(TaxoTemplatePrefix) &&
                Char.IsUpper(title[TaxoTemplatePrefix.Length]);

            // also covers "automatic taxobox, subspeciesbox, etc.", but doesn't cover
            // some others that may be interesting, such as "paraphyletic group" and "hybridbox".
            var haveTaxobox =
                text.Contains("taxobox", StringComparison.InvariantCultureIgnoreCase)
                | text.Contains("speciesbox", StringComparison.InvariantCultureIgnoreCase);

            var isRedirect = redirectParser.IsRedirect(text, out var redirectTitle);

            if (isRedirect)
            {
                redirectTitle = redirectTitle.TruncateUtf8(200);
            }

            var type =
                isParaHoxozoaException ? PageType.Ignored :
                hasCaseIssue ? PageType.Ignored :
                isRedirect ? PageType.Redirect :
                haveTaxobox ? PageType.Content :
                isTaxoTemplate ? PageType.TaxoTemplate :
                PageType.Ignored
                ;

            return new WikiPage
            {
                Lang = lang,
                Title = title,
                Id = element.Id,
                Ns = element.Ns,
                Step = Step.ToExtractContent,
                Type = type,
                Content = new WikiPageContent
                {
                    Text = rev.Text,
                    RedirectTitle = redirectTitle,
                    Model = rev.Model,
                    Format = rev.Format,
                    Sha1 = rev.Sha1
                }
            };
        }

        log.Info($"Starting import of {fileName}");

        // !!! temporary selection
        String[] missingNames = new[] { "core eudicots", "parahoxozoa", "core genistoids" };

        Boolean ShouldUpdatePage(WikiPage page)
        {
            // !!! temporary selection
            if (page.Type == PageType.TaxoTemplate)
            {
                var suffix = page.Title.Substring(TaxoTemplatePrefix.Length);
                if (missingNames.FirstOrDefault(n => n.Equals(suffix, StringComparison.InvariantCultureIgnoreCase)) == null)
                {
                    return false;
                }
                log.Info("Got one: {name}", suffix);
            }

            if (page.Type == PageType.Ignored) return false;

            if (updateOnly.HasValue && updateOnly != page.Type) return false;

            return true;
        }

        var hadTaxoTemplate = false;

        var i = 0;
        var persisted = 0;
        foreach (var element in elements)
        {
            ++i;

            var percent = zippedStream.Position * 100 / fileLength;

            if (i <= skip)
            {
                if (i % 1000 == 0) log.Info($"at #{i} skipping ({percent:d}% processed)");

                continue;
            }

            if (i % 1000 == 0) log.Info($"at #{i} with {persisted} persisted ({percent:d}% processed)");

            if (element is null) continue;

            var text = element.Revision.Text;

            var page = GetPage(element);

            titlesWriter?.WriteLine(page.Title);

            if (dryRun) continue;

            if (ShouldUpdatePage(page))
            {
                ++persisted;

                var title = element.Title;

                void UpdatePage()
                {
                    var db = dbFactory.CreateDbContext();

                    using var transaction = db.Database.BeginTransaction();

                    db.Database.ExecuteSqlRaw(
                        $"delete from {nameof(ApplicationDb.Pages)} where {nameof(WikiPage.Title)} = @title",
                        new SqlParameter("@title", title));

                    db.Pages.Add(page);

                    db.SaveChanges();

                    transaction.Commit();
                }

                var attempt = 1;

                var hasFailed = false;

                do
                {
                    hasFailed = false;

                    try
                    {
                        UpdatePage();
                    }
                    catch (Exception ex)
                    {
                        hasFailed = true;

                        ++attempt;

                        log.Error(ex, $"Failed to write to database, entity is:\n\n{JsonConvert.SerializeObject(GetPage(element))}\n");

                        if (attempt == 3)
                        {
                            log.Error($"Giving up on attempt #{attempt}");

                            throw;
                        }
                    }
                }
                while (hasFailed);

                if (page.Type == PageType.TaxoTemplate)
                {
                    hadTaxoTemplate = true;
                }
            }
        }

        log.Info($"Read {i} elements with {persisted} persisted");

        if (hadTaxoTemplate)
        {
            var db = dbFactory.CreateDbContext();

            db.Database.ExecuteSqlInterpolated(
            $@"
merge Taxoboxes t
using (
	select p.Lang, p.Title, c.Sha1, c.[Text]
	from Pages p
	join PageContents c on p.lang = c.lang and p.Title = c.Title
	where p.[Type] = {(Int32)PageType.TaxoTemplate} and p.Lang = {lang} and p.Step >= {(Int32)Step.ToExtractContent}
) s
on (t.Lang = s.Lang and t.Title = s.Title)
when matched then
	update set Sha1 = s.Sha1, [Taxobox] = s.[Text]
when not matched then
	insert (Lang, Title, Sha1, [Taxobox])
	values (s.Lang, s.Title, s.Sha1, s.[Text]);
");

            db.Database.ExecuteSqlInterpolated($@"
    update p
    set [Step] = {(Int32)Step.ToParseTaxobox}
	from Pages p
	join PageContents c on p.lang = c.lang and p.Title = c.Title
	where p.[Type] = {(Int32)PageType.TaxoTemplate} and p.Lang = {lang} and p.Step >= {(Int32)Step.ToExtractContent}
");

            log.Info($"Updated taxobox rows for modified taxo template pages");
        }
    }
}
