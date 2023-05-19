using Creational;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using NLog;

var log = LogManager.GetLogger("Program.cs");

IServiceProvider serviceProvider;
{
    var connectionString = @"Server=.\;Database=creational-utf8;integrated security=true";
    var services = new ServiceCollection();
    services.AddDbContextFactory<ApplicationDb>(o => o.UseSqlServer(connectionString));
    services.AddTransient<TaxoboxSpaceAnalyzer>();
    services.AddTransient<WikiDumpImporter>();
    services.AddTransient<WikiPageProcessor>();
    services.AddTransient<ContentExtractionWorker>();
    services.AddTransient<TaxoboxParsingWorker>();
    services.AddTransient<SiteArchiveWriter>();
    serviceProvider = services.BuildServiceProvider();
}

var fileName = @"c:\users\jens\downloads\dewiki-20230501-pages-articles.xml.bz2";

var importer = serviceProvider.GetRequiredService<WikiDumpImporter>();

var processor = serviceProvider.GetRequiredService<WikiPageProcessor>(); // no longer used
var extractionWorker = serviceProvider.GetRequiredService<ContentExtractionWorker>();
var parsingWorker = serviceProvider.GetRequiredService<TaxoboxParsingWorker>();
var siteArchiveWriter = serviceProvider.GetRequiredService<SiteArchiveWriter>();

var analyzer = serviceProvider.GetRequiredService<TaxoboxSpaceAnalyzer>();

//importer.Import(fileName, dryRun: false);

extractionWorker.ProcessAll();
parsingWorker.ProcessAll();
//analyzer.Analyze();

siteArchiveWriter.WriteArchive(@"C:\Users\jens\Documents\Projects\creationaljs\src\site-archive-data.json", 100);

log.Info("done");
