using Microsoft.EntityFrameworkCore;
using NLog;

namespace Creational;

public class TaxoboxImageCurator
{
    static Logger log = LogManager.GetCurrentClassLogger();

    private readonly IDbContextFactory<ApplicationDb> dbContextFactory;

    public TaxoboxImageCurator(IDbContextFactory<ApplicationDb> dbContextFactory)
    {
        this.dbContextFactory = dbContextFactory;
    }

    public void Curate()
    {
        var db = dbContextFactory.CreateDbContext();

        using var transaction = db.Database.BeginTransaction();

        db.Database.ExecuteSqlRaw(@"
delete TaxoboxImages;

insert TaxoboxImages (Title, [Filename])
select Title, [Value] [Filename]
from [TaxoboxEntries]
where [Key] = 'Bild' and [Value] <> '';
");

        transaction.Commit();

        log.Info("Updated taxobox images from taxobox entries");
    }
}
