using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NLog;

namespace Creational;

public class TaxoboxParsingWorker
{
    static Logger log = LogManager.GetCurrentClassLogger();

    private readonly IDbContextFactory<ApplicationDb> dbFactory;
    private readonly HeuristicTaxoboxParser taxoboxParser;

    public TaxoboxParsingWorker(IDbContextFactory<ApplicationDb> dbFactory)
    {
        this.dbFactory = dbFactory;

        taxoboxParser = new HeuristicTaxoboxParser();
    }

    public void ProcessAll(String lang)
    {
        var (processed, _, total) = GetPageStats(lang);

        while (true)
        {
            var justProcessed = ProcessBatch(lang);

            if (justProcessed == 0) break;

            processed += justProcessed;

            log.Info($"Parsed {processed} of {total} pages", processed, total);
        }

        LogParsingSummary(lang);
    }

    void LogParsingSummary(String lang)
    {
        var db = dbFactory.CreateDbContext();

        var parsingResultSummary = (
            from r in db.ParsingResults
            where r.Lang == lang
            group r by r.Exception into g
            select new { Exception = g.Key, Count = g.Count(), Sample = g.First().Title }
        ).ToArray();

        var report = String.Join("\n ", from s in parsingResultSummary select $"{s.Count:d}: {s.Exception ?? "(fine)"}, eg. '{s.Sample}'");

        log.Info("All parsed results report:\n\n {parsingErrorReport}");
    }

    IQueryable<WikiPage> GetPageQuery(ApplicationDb db, String lang)
    {
        return db.Pages.Where(p => p.Lang == lang && p.Type == PageType.Content);
    }

    public (Int32 alreadyParsed, Int32 inError, Int32 total) GetPageStats(String lang)
    {
        var db = dbFactory.CreateDbContext();

        var inParsingError = GetPageQuery(db, lang).Count(p => p.Step == Step.ToParseTaxobox.AsFailedStep());
        var toParse = GetPageQuery(db, lang).Count(p => p.Step == Step.ToParseTaxobox);
        var alreadyParsed = GetPageQuery(db, lang).Count(p => p.Step > Step.ToParseTaxobox);

        return (alreadyParsed, inParsingError, toParse + inParsingError + alreadyParsed);
    }

    public Int32 ProcessBatch(String lang, Int32 batchSize = 1000)
    {
        var db = dbFactory.CreateDbContext();

        var pages = GetPageQuery(db, lang)
            .Where(p => p.Step == Step.ToParseTaxobox)
            .Include(p => p.Parsed)
            .Include(p => p.Taxobox)
            .OrderBy(p => p.Lang)
            .ThenBy(p => p.Step)
            .Take(batchSize)
            .ToArray()
            ;

        log.Debug("Taxobox batch loaded ({count})", pages.Length);

        if (pages.Length == 0) return 0;

        using var transaction = db.Database.BeginTransaction();

        var currentParsingResults = from p in pages where p.Parsed != null select p.Parsed;

        db.ParsingResults.RemoveRange(currentParsingResults);

        db.SaveChanges();

        if (currentParsingResults.Count() != 0) throw new Exception("Expected to no longer have any parsing results");

        var parsingErrors = 0;

        var parsingResults = (
            from p in pages
            select ParseTaxobox(p, ref parsingErrors)
        ).ToArray();

        foreach (var result in parsingResults)
        {
            var page = result.Page;
            page.Parsed = result;

            if (result.Exception == null)
            {
                page.Step = Step.Finished;
                page.StepError = null;
            }
            else
            {
                page.Step = Step.ToParseTaxobox.AsFailedStep();
                page.StepError = result.Exception;
            }
        }

        db.SaveChanges();

        log.Debug("Changes saved");

        transaction.Commit();

        return pages.Length;
    }


    ParsingResult ParseTaxobox(WikiPage page, ref Int32 parsingErrors)
    {
        var result = new ParsingResult();

        result.Lang = page.Lang;
        result.Title = page.Title;
        result.Page = page;

        if (page.Taxobox == null)
        {
            result.Exception = "no taxobox row";

            return result;
        }

        var taxobox = page.Taxobox.Taxobox;

        if (taxobox == null)
        {
            result.Exception = "no taxobox content";

            return result;
        }

        try
        {
            //taxoboxParser.ParseIntoParsingResult(result, taxobox); // old version
            result.FillByWikiParser(taxobox); // new version

            result.HasTruncationIssue = false;

            if (result.Exception is null)
            {
                // Used to be in ParseIntoParsingResult (formerly GetEntries) and needs adjusting too
                result.TaxonomyEntries = taxoboxParser.GetTaxonomyEntries(result.TaxoboxEntries, out var haveTruncationIssue);

                result.HasTruncationIssue = haveTruncationIssue;
            }
        }
        catch (Exception ex)
        {
            result.Exception = ex.Message;

            ++parsingErrors;
        }

        return result;
    }
}
