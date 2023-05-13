using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NLog;

namespace Creational;

public class WikiPageLeanProcessor
{
    static Logger log = LogManager.GetCurrentClassLogger();

    private readonly IDbContextFactory<ApplicationDb> dbFactory;
    private readonly TaxoboxParser taxoboxParser;

    public WikiPageLeanProcessor(IDbContextFactory<ApplicationDb> dbFactory)
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
            log.Info($"Save {p} of {toParse + finished} taxoboxes, had {errors} errors");
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

        return (counts.GetValueOrDefault(Step.ToRead), counts.GetValueOrDefault(Step.ToExtractTaxobox), counts.GetValueOrDefault(Step.Finished));
    }

    public Int32 ProcessBatch(ref Int32 errors, Int32 batchSize = 10)
    {
        try
        {
            return InnerProcessBatch(ref errors, batchSize);
        }
        catch (Exception)
        {
            var dummy = 0;

            for (var i = 0; i < batchSize; i++)
            {
                InnerProcessBatch(ref dummy, 1, logEntry: true);
            }

            throw;
        }
    }

    public Int32 InnerProcessBatch(ref Int32 errors, Int32 batchSize = 10, Boolean logEntry = false)
    {
        var db = dbFactory.CreateDbContext();

        using var transaction = db.Database.BeginTransaction();

        var batch = db.Pages
            .Include(p => p.Content)
            .Include(p => p.Taxobox)
            .Where(p => p.Step == Step.ToExtractTaxobox && p.Type == PageType.Content)
            .Take(batchSize)
            .ToArray()
            ;

        if (batch.Length == 0) return 0;

        db.Taxoboxes.RemoveRange(batch.Select(p => p.Taxobox).Where(p => p != null));

        db.SaveChanges();

        foreach (var page in batch)
        {
            if (page.Content is null)
            {
                page.Step = Step.ToRead;

                continue;
            }

            var text = page.Content.Text;

            var taxobox = new WikiTaxobox
            {
                Title = page.Title,
                Sha1 = page.Content.Sha1
            };

            try
            {
                taxobox.Taxobox = taxoboxParser.GetTaxobox(text);
            }
            catch (Exception ex)
            {
                ++errors;

                taxobox.Taxobox = "Exception: " + ex.Message;
            }

            if (logEntry)
            {
                var json = JsonConvert.SerializeObject(taxobox, Formatting.Indented);

                log.Info($"About to save taxobox:\n\n{json}");
            }

            db.Taxoboxes.Add(taxobox);

            page.Step = Step.Finished;
        }

        db.SaveChanges();

        transaction.Commit();

        return batch.Length;
    }
}
