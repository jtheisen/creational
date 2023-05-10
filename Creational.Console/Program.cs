using ICSharpCode.SharpZipLib.BZip2;
using System.Xml;
using Creational;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using NLog;
using Newtonsoft.Json;
using static System.Net.Mime.MediaTypeNames;

var fileName = @"c:\users\jens\downloads\dewiki-20230501-pages-articles.xml.bz2";

var fileLength = new FileInfo(fileName).Length;

using var zippedStream = new FileStream(@"c:\users\jens\downloads\dewiki-20230501-pages-articles.xml.bz2", FileMode.Open, FileAccess.Read);
using var xmlStream = new BZip2InputStream(zippedStream);
var xmlReader = XmlReader.Create(xmlStream);

var elements = xmlReader.StreamElements();

IServiceProvider serviceProvider;
{
    var connectionString = @"Server=.\;Database=creational;integrated security=true";
    var services = new ServiceCollection();
    services.AddDbContextFactory<ApplicationDb>(o => o.UseSqlServer(connectionString));
    serviceProvider = services.BuildServiceProvider();
}

var dbFactory = serviceProvider.GetRequiredService<IDbContextFactory<ApplicationDb>>();

var log = LogManager.GetLogger("main");

WikiPage GetPage(XPage element)
{
    var title = element.Title;
    var rev = element.Revision;

    return new WikiPage
    {
        Title = title,
        Id = element.Id,
        Ns = element.Ns,
        Content = new WikiPageContent
        {
            Text = rev.Text,
            Model = rev.Model,
            Format = rev.Format,
            Sha1 = rev.Sha1
        }
    };
}

log.Info($"Starting");

var i = 0;
var boxes = 0;
foreach (var element in elements)
{
    ++i;

    if (element is null) continue;

    var percent = zippedStream.Position * 100 / fileLength;

    var text = element.Revision.Text;

    if (text.Contains("taxobox", StringComparison.InvariantCultureIgnoreCase))
    {
        ++boxes;

        var title = element.Title;

        var db = dbFactory.CreateDbContext();

        using var transaction = db.Database.BeginTransaction();

        try
        {
            db.Database.ExecuteSqlRaw(
                $"delete from {nameof(ApplicationDb.Pages)} where {nameof(WikiPage.Title)} = @title",
                new SqlParameter("@title", title));

            db.Pages.Add(GetPage(element));

            db.SaveChanges();

            transaction.Commit();
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Failed to write to database, entity is:\n\n{JsonConvert.SerializeObject(GetPage(element))}\n");
        }
    }

    if (i % 1000 == 0) log.Info($"at #{i} with {boxes} taxoboxes ({percent:d}% processed)");
}

log.Info($"Read {i} elements with {boxes} taxoboxes");
