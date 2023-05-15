using Creational.Migrations;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using NLog;
using System.Buffers.Text;
using System.Text;

namespace Creational;

public class ContentExtractionWorker
{
    static Logger log = LogManager.GetCurrentClassLogger();

    private readonly IDbContextFactory<ApplicationDb> dbFactory;
    private readonly TaxoboxParser taxoboxParser;

    public ContentExtractionWorker(IDbContextFactory<ApplicationDb> dbFactory)
    {
        this.dbFactory = dbFactory;

        taxoboxParser = new TaxoboxParser();
    }

    public void ProcessAll()
    {
        var db = dbFactory.CreateDbContext();

        var (total, pending, inError) = db.LoadWorkingStats(Step.ToExtractContent);

        var i = 0;
        var p = 0;

        var errors = 0;

        log.Info("Will process {pending} of {total} page contents, {inError} will remain in error", pending, total, inError);

        while (true)
        {
            var b = ProcessBatch(ref errors);

            p += b;

            if (b == 0)
            {
                log.Info($"Visited all {p} of {pending} contents, had {errors} errors so far");

                break;
            }
            else if (++i % 1 == 0)
            {
                log.Info($"Saved {p} of {pending} taxoboxes, put {errors} in error");
            }
        }
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
            .Include(p => p.ImageLinks)
            .Where(p => p.Step == Step.ToExtractContent && p.Type == PageType.Content)
            .Take(batchSize)
            .ToArray()
            ;

        if (batch.Length == 0) return 0;

        db.Taxoboxes.RemoveRange(batch.Select(p => p.Taxobox).Where(p => p != null));
        db.ImageLinks.RemoveRange(batch.SelectMany(p => p.ImageLinks.EmptyIfNull()));

        db.SaveChanges();

        foreach (var page in batch)
        {
            if (page.Content is null)
            {
                page.Step = Step.ToRead;

                continue;
            }

            try
            {
                FindTaxobox(db, page);
                FindImagesLinks(db, page);

                page.StepError = null;
                page.Step = Step.ToParseTaxobox;
            }
            catch (Exception ex)
            {
                ++errors;
                page.StepError = ex.Message;
                page.Step = Step.ToExtractContent.AsFailedStep();
            }
        }

        db.SaveChanges();

        transaction.Commit();

        return batch.Length;
    }

    void FindTaxobox(ApplicationDb db, WikiPage page)
    {
        var text = page.Content.Text;

        var taxobox = new WikiTaxobox
        {
            Title = page.Title,
            Sha1 = page.Content.Sha1
        };

        taxobox.Taxobox = taxoboxParser.GetTaxobox(text);

        db.Taxoboxes.Add(taxobox);
    }

    void FindImagesLinks(ApplicationDb db, WikiPage page)
    {
        var text = page.Content.Text;

        var links = taxoboxParser.FindImageLinks(text);

        WikiImageLink MakeLink(TaxoboxParser.ImageLink l)
        {
            var fileName = l.fileName;

            if (fileName.Length > 1000)
            {
                log.Warn("Suspiciously long filename in page {page}, will throw: {filename}", page.Title, fileName.Truncate(40));

                throw new Exception("Suspiciously long filename, refusing to save");
            }

            return new WikiImageLink { Title = page.Title, Position = l.position, Filename = l.fileName };
        }

        db.ImageLinks.AddRange(links.Select(MakeLink));
    }
}
