using Creational;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Extensions.Logging;
using Microsoft.Extensions.Logging;

var log = LogManager.GetLogger("Program.cs");

IServiceProvider serviceProvider;
{
    var connectionString = @"Server=.\;Database=creational-utf8;integrated security=true";
    var services = new ServiceCollection();

    services.AddLogging(logger => {
        logger.ClearProviders();
        logger.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
        logger.AddNLog();
    });
    
    services.AddDbContextFactory<ApplicationDb>(o => o.UseSqlServer(connectionString));
    services.AddTransient<TaxoboxSpaceAnalyzer>();
    services.AddTransient<WikiDumpImporter>();
    services.AddTransient<WikiPageProcessor>();
    services.AddTransient<ContentExtractionWorker>();
    services.AddTransient<TaxoboxParsingWorker>();
    services.AddTransient<TaxoboxImageCurator>();
    services.AddTransient<WikiImageResolver>();
    services.AddTransient<SiteArchiveWriter>();
    services.AddTransient<WikiImageDownloader>();
    serviceProvider = services.BuildServiceProvider();
}

var fileName = @"c:\users\jens\downloads\dewiki-20230501-pages-articles.xml.bz2";

var importer = serviceProvider.GetRequiredService<WikiDumpImporter>();

var processor = serviceProvider.GetRequiredService<WikiPageProcessor>(); // no longer used
var extractionWorker = serviceProvider.GetRequiredService<ContentExtractionWorker>();
var parsingWorker = serviceProvider.GetRequiredService<TaxoboxParsingWorker>();
var taxoboxImageCurator = serviceProvider.GetRequiredService<TaxoboxImageCurator>();
var imageResolver = serviceProvider.GetRequiredService<WikiImageResolver>();
var imageDownloader = serviceProvider.GetRequiredService<WikiImageDownloader>();
var siteArchiveWriter = serviceProvider.GetRequiredService<SiteArchiveWriter>();

var analyzer = serviceProvider.GetRequiredService<TaxoboxSpaceAnalyzer>();

//importer.Import(fileName, dryRun: false);

//extractionWorker.ProcessAll();
//parsingWorker.ProcessAll();
//analyzer.Analyze();

//taxoboxImageCurator.Curate();
//imageResolver.ResolveAllImages();
imageDownloader.DownloadAllThumbs();


siteArchiveWriter.WriteArchive(@"C:\Users\jens\Documents\Projects\creationaljs\src\site-archive-data.json", 100);

log.Info("done");
