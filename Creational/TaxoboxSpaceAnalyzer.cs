using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Creational.TaxoboxParser;

namespace Creational;

public class TaxoboxSpaceAnalyzer
{
    static Logger log = LogManager.GetCurrentClassLogger();

    private readonly IDbContextFactory<ApplicationDb> dbContextFactory;
    private readonly TaxoboxParser taxoboxParser;

    public TaxoboxSpaceAnalyzer(IDbContextFactory<ApplicationDb> dbContextFactory)
    {
        this.dbContextFactory = dbContextFactory;

        taxoboxParser = new TaxoboxParser();
    }

    public void Analyze(Boolean resetPagesInError = false)
    {
        var db = dbContextFactory.CreateDbContext();

        log.Info("Analyze loading data");

        var redirections = (
            from p in db.Pages
            where p.Type == PageType.Redirect
            join c in db.PageContents on p.Title equals c.Title into cs
            from c in cs
            select new { p.Title, c.RedirectTitle }
        ).ToDictionary(p => p.Title, p => p.RedirectTitle);

        log.Info("Redirections loaded");

        var pages = db.Pages
            .Include(p => p.Taxobox)
            .Where(p => p.Type == PageType.Content)
            .ToArray();

        log.Info("Taxoboxes loaded");

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

        if (resetPagesInError)
        {
            var hadError = false;

            foreach (var result in parsingResults)
            {
                if (result.Exception == null) continue;

                hadError = true;

                result.Page.Step = Step.ToExtractTaxobox;
            }

            db.SaveChanges();

            if (hadError) log.Info("Respective pages marked to have taxoboxes reextracted");
        }

        var taxonomyEntries = (
            from result in parsingResults
            where result.TaxonomyEntries is not null
            from e in result.TaxonomyEntries
            select e
        ).ToArray();

        var taxonomyEntriesByTitle = taxonomyEntries.ToLookup(e => e.Title, e => e);
        var taxonomyEntriesByName = taxonomyEntries.ToLookup(e => e.Name, e => e);
        var taxonomyEntriesByNamDe = taxonomyEntries.ToLookup(e => e.NameDe, e => e);

        log.Info("Lookups built");
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
