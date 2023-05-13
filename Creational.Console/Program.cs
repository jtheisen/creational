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
    services.AddTransient<WikiPageLeanProcessor>();
    serviceProvider = services.BuildServiceProvider();
}

var fileName = @"c:\users\jens\downloads\dewiki-20230501-pages-articles.xml.bz2";

var importer = serviceProvider.GetRequiredService<WikiDumpImporter>();

var processor = serviceProvider.GetRequiredService<WikiPageProcessor>();
var leanProcessor = serviceProvider.GetRequiredService<WikiPageLeanProcessor>();

var analyzer = serviceProvider.GetRequiredService<TaxoboxSpaceAnalyzer>();

//importer.Import(fileName, dryRun: false);
//processor.ProcessAll();
leanProcessor.ProcessAll();
analyzer.Analyze(resetPagesInError: true);

log.Info("done");
