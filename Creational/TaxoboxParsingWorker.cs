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
        var db = dbFactory.CreateDbContext();

        var pages = db.Pages
            .Include(p => p.Taxobox)
            .Where(p => p.Lang == lang && p.Step == Step.ToParseTaxobox && p.Type == PageType.Content)
            .ToArray()
            ;

        log.Info("Taxoboxes loaded ({count})", pages.Length);

        if (pages.Length == 0) return;

        var parsingErrors = 0;

        var i = 0;

        var parsingResults = new List<ParsingResult>();

        foreach (var page in pages)
        {
            ++i;

            parsingResults.Add(ParseTaxobox(page, ref parsingErrors));

            if (i % 1000 == 0) log.Info("Parsed {i}K taxoboxes", i);
        }

        var parsingErrorReport = String.Join("\n ",
            from r in parsingResults
            group r by r.Exception into g
            select $"{g.Count():d}: {g.Key ?? "(fine)"}, eg. '{g.First().Title}'"
        );

        log.Info($"Taxoboxes parsed ({parsingErrors} parsing errors):\n\n {parsingErrorReport}\n");

        foreach (var result in parsingResults)
        {
            var page = result.Page;

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

        using var transaction = db.Database.BeginTransaction();

        db.Database.ExecuteSqlInterpolated(@$"
delete r
from ParsingResults r
join Pages p on r.Title = p.Title and p.Step = {Step.ToParseTaxobox} and p.Type = {PageType.Content}
where r.Lang = {lang}
");

        log.Info("Saving steps");

        db.SaveChanges();

        db.ParsingResults.AddRange(parsingResults);

        log.Info("Saving results");

        db.SaveChanges();

        log.Info("Changes saved");

        transaction.Commit();
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
