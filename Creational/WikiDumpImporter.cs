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

    public void Import(String fileName, String lang, Int32 skip = 0, Boolean dryRun = false)
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
                isRedirect ? PageType.Redirect :
                haveTaxobox ? PageType.Content :
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

            if (element is null) continue;

            var text = element.Revision.Text;

            var page = GetPage(element);

            if (dryRun) continue;

            if (page.Type != PageType.Ignored)
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
            }

            if (i % 1000 == 0) log.Info($"at #{i} with {persisted} persisted ({percent:d}% processed)");
        }

        log.Info($"Read {i} elements with {persisted} persisted");
    }

}
