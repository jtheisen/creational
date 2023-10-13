using Creational;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Extensions.Logging;
using Microsoft.Extensions.Logging;

var log = LogManager.GetLogger("Program.cs");

IServiceProvider serviceProvider;
{
    var connectionString = @"Server=.\;Database=creational;integrated security=true";
    var services = new ServiceCollection();

    services.AddLogging(logger => {
        logger.ClearProviders();
        logger.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
        logger.AddNLog();
    });
    
    services.AddDbContextFactory<ApplicationDb>(o => o.UseSqlServer(connectionString));
    services.AddTransient<TaxoboxSpaceAnalyzer>();
    
    // Creates Page and PageContent from the import file
    services.AddTransient<WikiDumpImporter>();

    // Extracts Taxobox (text) and ImageLink from the page contents
    services.AddTransient<ContentExtractionWorker>();

    services.AddTransient<TaxoboxParsingWorker>();
    services.AddTransient<TaxoboxImageCurator>();
    services.AddTransient<WikiImageResolver>();
    services.AddTransient<SiteArchiveWriter>();
    services.AddTransient<WikiImageDownloader>();
    serviceProvider = services.BuildServiceProvider();
}

var deFileName = @"c:\users\jens\downloads\dewiki-20230501-pages-articles.xml.bz2";
var enFileName = @"c:\users\jens\downloads\enwiki-20231001-pages-articles.xml.bz2";

var lang = "en";
var fileName = enFileName;

var importer = serviceProvider.GetRequiredService<WikiDumpImporter>();

var extractionWorker = serviceProvider.GetRequiredService<ContentExtractionWorker>();
var parsingWorker = serviceProvider.GetRequiredService<TaxoboxParsingWorker>();
var taxoboxImageCurator = serviceProvider.GetRequiredService<TaxoboxImageCurator>();
var imageResolver = serviceProvider.GetRequiredService<WikiImageResolver>();
var imageDownloader = serviceProvider.GetRequiredService<WikiImageDownloader>();
var siteArchiveWriter = serviceProvider.GetRequiredService<SiteArchiveWriter>();

var analyzer = serviceProvider.GetRequiredService<TaxoboxSpaceAnalyzer>();

//importer.Import(fileName, lang, skip: 0, dryRun: false);

//extractionWorker.ProcessAll(lang);
parsingWorker.ProcessAll(lang);
//analyzer.Analyze(lang);

//taxoboxImageCurator.Curate();
//imageResolver.ResolveAllImages();
//imageDownloader.DownloadAllThumbs();


//siteArchiveWriter.WriteArchive(@"C:\Users\jens\Documents\Projects\creationaljs\src\site-archive-data.json", 100);

log.Info("done");
