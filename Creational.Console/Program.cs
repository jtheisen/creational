using Creational;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using NLog;

IServiceProvider serviceProvider;
{
    var connectionString = @"Server=.\;Database=creational;integrated security=true";
    var services = new ServiceCollection();
    services.AddDbContextFactory<ApplicationDb>(o => o.UseSqlServer(connectionString));
    services.AddTransient<WikiDumpImporter>();
    services.AddTransient<WikiPageProcessor>();
    serviceProvider = services.BuildServiceProvider();
}

var fileName = @"c:\users\jens\downloads\dewiki-20230501-pages-articles.xml.bz2";

var importer = serviceProvider.GetRequiredService<WikiDumpImporter>();

var processor = serviceProvider.GetRequiredService<WikiPageProcessor>();


importer.Import(fileName, dryRun: true);
//processor.ProcessAll();
