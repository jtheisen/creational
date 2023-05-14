using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NLog;

namespace Creational;

public class TaxoboxExtractionWorker
{
    static Logger log = LogManager.GetCurrentClassLogger();

    private readonly IDbContextFactory<ApplicationDb> dbFactory;
    private readonly TaxoboxParser taxoboxParser;

    public TaxoboxExtractionWorker(IDbContextFactory<ApplicationDb> dbFactory)
    {
        this.dbFactory = dbFactory;

        taxoboxParser = new TaxoboxParser();
    }

    public void ProcessAll()
    {

        log.Info("Done");
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
