using Creational.Migrations;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NLog;
using static Creational.HeuristicTaxoboxParser;

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

    public void ProcessAll(String lang, PageType? parseOnly = null)
    {
        var (processed, _, total) = GetPageStats(lang, parseOnly);

        log.Info("# Parsing pages");

        if (parseOnly.HasValue)
        {
            log.Info($"Restricting processing to {parseOnly}");
        }

        while (true)
        {
            var justProcessed = ProcessBatch(lang, parseOnly);

            if (justProcessed == 0) break;

            processed += justProcessed;

            log.Info($"Parsed {processed} of {total} pages", processed, total);
        }
    }

    public void LogParsingSummary(String lang)
    {
        var db = dbFactory.CreateDbContext();

        var parsingResultSummary = (
            from r in db.ParsingResults
            where r.Lang == lang
            group r by r.Exception into g
            select new { Exception = g.Key, Count = g.Count(), Sample = g.First().Title }
        ).ToArray();

        var report = String.Join("\n ", from s in parsingResultSummary select $"{s.Count:d}: {s.Exception ?? "(fine)"}, eg. '{s.Sample}'");

        log.Info("All parsed results report:\n\n {report}", report);
    }

    IQueryable<WikiPage> GetPageQuery(ApplicationDb db, String lang, PageType? parseOnly)
    {
        var result = db.Pages.Where(p => p.Lang == lang);

        if (parseOnly is PageType ot)
        {
            result = result.Where(p => p.Type == ot);
        }
        else
        {
            result = result.Where(p => p.Type >= PageType.Content && p.Type <= PageType.TaxoTemplate);
        }

        return result;
    }

    public (Int32 alreadyParsed, Int32 inError, Int32 total) GetPageStats(String lang, PageType? parseOnly)
    {
        var db = dbFactory.CreateDbContext();

        var inParsingError = GetPageQuery(db, lang, parseOnly).Count(p => p.Step == Step.ToParseTaxobox.AsFailedStep());
        var toParse = GetPageQuery(db, lang, parseOnly).Count(p => p.Step == Step.ToParseTaxobox);
        var alreadyParsed = GetPageQuery(db, lang, parseOnly).Count(p => p.Step > Step.ToParseTaxobox);

        return (alreadyParsed, inParsingError, toParse + inParsingError + alreadyParsed);
    }

    public Int32 ProcessBatch(String lang, PageType? parseOnly, Int32 batchSize = 1000)
    {
        var db = dbFactory.CreateDbContext();

        var pages = GetPageQuery(db, lang, parseOnly)
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
                page.StepError = SimplifyErrorMessage(result.Exception);
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
        result.Type = page.Type;
        result.Page = page;
        result.HasTruncationIssue = false;

        if (IsHandledTaxoTemplateRoot(result))
        {
            return result;
        }

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
            switch (page.Type)
            {
                case PageType.TaxoTemplate:
                    ProcessTaxoTemplate(result, taxobox);
                    break;
                case PageType.Content:
                    ProcessTaxobox(result, taxobox);
                    break;
                default:
                    throw new Exception($"Unknown type {page.Type}");
            }
        }
        catch (Exception ex)
        {
            result.Exception = ex.Message;

            ++parsingErrors;
        }

        return result;
    }

    Boolean IsHandledTaxoTemplateRoot(ParsingResult result)
    {
        if (result.Lang.Equals("en", StringComparison.InvariantCultureIgnoreCase) &&
            result.Title.Equals("Template:Taxonomy/Life", StringComparison.InvariantCultureIgnoreCase))
        {
            result.TaxoTemplateValues = new TaxoTemplateValues
            {
                Rank = "root"
            };

            return true;
        }
        else
        {
            return false;
        }
    }

    void ProcessTaxoTemplate(ParsingResult result, String text)
    {
        if (text.StartsWith(TaxoTemplateRedirectPrefix))
        {
            ProcessTaxoTemplateRedirect(result, text);
        }
        else
        {
            ProcessProperTaxoTemplate(result, text);
        }
    }

    void ProcessTaxoTemplateRedirect(ParsingResult result, String text)
    {
        if (!text.StartsWith(TaxoTemplateRedirectLead))
        {
            throw new Exception($"Redirect starts with unexpected lead");
        }

        var ci = text.IndexOf(']', TaxoTemplateRedirectLead.Length);

        if (ci < 0)
        {
            throw new Exception($"Expected redirection to terminate on an ']'");
        }

        var name = text[TaxoTemplateRedirectLead.Length..ci];

        result.Redirection = $"Template:Taxonomy/{name}";
        result.TaxoTemplateValues = new TaxoTemplateValues
        {
            SameAs = name
        };
    }

    void ProcessProperTaxoTemplate(ParsingResult result, String taxotemplate)
    {
        taxoboxParser.ParseIntoParsingResult(result, PrepareTaxoTemplate(taxotemplate));

        String GetValue(String key, Boolean toleratePipes = false)
        {
            var value = result.TaxoboxEntries
                .FirstOrDefault(e => e.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase))
                ?.Value;

            if (!toleratePipes && value?.Contains('|') == true)
            {
                throw new Exception($"Value contained '|', likely a parsing error");
            }

            return value;
        }

        var values = result.TaxoTemplateValues = new TaxoTemplateValues
        {
            Rank = GetValue("rank"),
            Parent = GetValue("parent"),
            SameAs = GetValue("same_as") ?? GetValue("same as")
        };
        
        if (GetValue("link", true) is String link)
        {
            values.FillLinkValues(link);
        }
    }

    void ProcessTaxobox(ParsingResult result, String taxobox)
    {
        result.FillByWikiParser(taxobox); // new version
    }

    static readonly String TaxoTemplateRedirectPrefix = "#REDIRECT";
    static readonly String TaxoTemplateRedirectLead = "#REDIRECT [[Template:Taxonomy/";
    static readonly String TaxoTemplatePrefix = "{{Don't edit this line {{{machine code|}}}\n";
    static readonly String TaxoTemplateReplacementPrefix = "{{Taxotemplate\n";

    String PrepareTaxoTemplate(String taxobox)
    {
        // only necessary when text was inserted with SSMS
        taxobox = taxobox.ReplaceLineEndings("\n");

        if (taxobox.StartsWith(TaxoTemplateRedirectPrefix)) throw new Exception($"Taxo template is a redirect");

        var obi = taxobox.IndexOf(TaxoTemplatePrefix);

        if (obi < 0)
        {
            throw new Exception($"Taxo template does not contain expected taxotemplate prefix");
        }
        else if (obi > 0)
        {
            taxobox = taxobox.Substring(obi);
        }

        taxobox = TaxoTemplateReplacementPrefix + taxobox.Substring(TaxoTemplatePrefix.Length);

        var cbi = taxobox.LastIndexOf("}}");

        if (cbi < 0) throw new Exception("Taxo template unexpectedly did not contain a closing '}}'");

        taxobox = taxobox[0..(cbi + 2)];

        if (taxobox[taxobox.Length - 3] != '\n')
        {
            taxobox = taxobox.TrimEnd('}') + "\n}}";
        }

        return taxobox;
    }

    String SimplifyErrorMessage(String message)
    {
        var i = message.IndexOfAny("(\"".ToArray());

        if (i >= 0)
        {
            message = message.Substring(0, i) + "...";
        }

        return message;
    }
}
