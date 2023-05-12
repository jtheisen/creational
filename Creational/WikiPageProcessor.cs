using Microsoft.EntityFrameworkCore;
using NLog;

namespace Creational;

public class WikiPageProcessor
{
    static Logger log = LogManager.GetCurrentClassLogger();

    private readonly IDbContextFactory<ApplicationDb> dbFactory;
    private readonly TaxoboxParser taxoboxParser;

    public WikiPageProcessor(IDbContextFactory<ApplicationDb> dbFactory)
    {
        this.dbFactory = dbFactory;

        taxoboxParser = new TaxoboxParser();
    }

    public void ProcessAll()
    {
        var (_, toParse, finished) = LoadStats();

        var i = 0;
        var p = finished;

        var errors = 0;

        void Log()
        {
            log.Info($"Parsed {p} of {toParse + finished} entries, had {errors} errors");
        }

        while (true)
        {
            var b = ProcessBatch(ref errors);

            p += b;

            if (b == 0)
            {
                Log();

                break;
            }
            else  if (++i % 1 == 0)
            {
                Log();
            }
        }

        log.Info("Done");
    }

    (Int32 toLoad, Int32 toParse, Int32 finished) LoadStats()
    {
        var db = dbFactory.CreateDbContext();

        var query =
            from p in db.Pages
            group p by p.Step into g
            select new { Step = g.Key, Count = g.Count() };

        var counts = query.ToDictionary(k => k.Step, k => k.Count);

        return (counts.GetValueOrDefault(Step.ToRead), counts.GetValueOrDefault(Step.ToParse), counts.GetValueOrDefault(Step.Finished));
    }

    public Int32 ProcessBatch(ref Int32 errors)
    {
        var batchSize = 10;

        var db = dbFactory.CreateDbContext();

        var transaction = db.Database.BeginTransaction();

        var batch = db.Pages
            .Include(p => p.Content)
            .Include(p => p.Parsed)
            .Where(p => p.Step == Step.ToParse)
            .Take(batchSize)
            .ToArray()
            ;

        if (batch.Length == 0) return 0;

        db.ParsingResults.RemoveRange(batch.Select(p => p.Parsed).Where(p => p != null));

        db.SaveChanges();

        foreach (var page in batch)
        {
            if (page.Content is null)
            {
                page.Step = Step.ToRead;

                continue;
            }

            var text = page.Content.Text;

            var parsed = new ParsingResult
            {
                Title = page.Title,
                Sha1 = page.Content.Sha1
            };

            db.ParsingResults.Add(parsed);

            try
            {
                var entries = taxoboxParser.GetEntries(text);

                parsed.TaxoboxEntries = entries;
                parsed.WithTaxobox = entries.Count > 0;
            }
            catch (Exception ex)
            {
                ++errors;

                parsed.Exception = ex.Message;
            }

            page.Step = Step.Finished;
        }

        db.SaveChanges();

        transaction.Commit();

        return batch.Length;
    }
}
