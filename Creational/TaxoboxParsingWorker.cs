using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NLog;

namespace Creational;

public class TaxoboxParsingWorker
{
    static Logger log = LogManager.GetCurrentClassLogger();

    private readonly IDbContextFactory<ApplicationDb> dbFactory;
    private readonly TaxoboxParser taxoboxParser;

    public TaxoboxParsingWorker(IDbContextFactory<ApplicationDb> dbFactory)
    {
        this.dbFactory = dbFactory;

        taxoboxParser = new TaxoboxParser();
    }

    public void ProcessAll()
    {
        var db = dbFactory.CreateDbContext();

        var pages = db.Pages
            .Include(p => p.Taxobox)
            .Where(p => p.Step == Step.ToParseTaxobox && p.Type == PageType.Content)
            .ToArray()
            ;

        log.Info("Taxoboxes loaded ({count})", pages.Length);

        if (pages.Length == 0) return;

        var parsingErrors = 0;

        var parsingResults = (
            from p in pages
            select ParseTaxobox(p, ref parsingErrors)
        ).ToArray();

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
            }
            else
            {
                page.Step = Step.ToParseTaxobox.AsFailedStep();
            }
        }

        using var transaction = db.Database.BeginTransaction();

        db.Database.ExecuteSqlInterpolated(@$"
delete r
from ParsingResults r
join Pages p on r.Title = p.Title and p.Step = {Step.ToParseTaxobox} and p.Type = {PageType.Content}
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
            taxoboxParser.GetEntries(result, taxobox);
        }
        catch (Exception ex)
        {
            result.Exception = ex.Message;

            ++parsingErrors;
        }

        return result;
    }
}
