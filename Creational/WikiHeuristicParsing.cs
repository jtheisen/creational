using Humanizer;
using System.Reflection;
using System.Text.RegularExpressions;
using static Creational.HeuristicTaxoboxParser;

namespace Creational;

/**
 * All pseudo-parsing work that is used to
 * - parse redirects
 * - quickly grab the taxoboxes from the entire page without really parsing it
 */

public class ParsingException : Exception
{
    public ParsingException(String message, String additionalMessage)
        : base($"{message} ({additionalMessage})")
    {
        SimpleMessage = message;
    }

    public ParsingException(String message)
        : base(message)
    {
        SimpleMessage = message;
    }

    public String SimpleMessage { get; }
}

public class RedirectParser
{
    Regex regex = new Regex(@"^#redirect \[\[([^\n\]]+)]]$");

    // Not sure if we want to ignore references:
    //Regex regex = new Regex(@"^#redirect \[\[([^\n\]#]+)[^\n\]]*]]$");

    public Boolean IsRedirect(String text, out String redirectTitle)
    {
        redirectTitle = null;

        var match = regex.Match(text);

        if (!match.Success) return false;

        redirectTitle = match.Groups[1].Captures.FirstOrDefault()?.Value;

        return true;
    }
}

public class HeuristicTaxoboxParser
{
    static readonly String taxoboxNamesRegex = "(?:Taxobox|Automatic[_ ]taxobox|Speciesbox)";

    //Regex infoboxSimpleMatcher = new Regex(@"^{{Taxobox.*");
    Regex regexTaxoboxWithEntries = new Regex(@"^{{Taxobox\n(\|\s*(\w+)\s*=\s*([^\n]+)\n)*}}", RegexOptions.Multiline, TimeSpan.FromMilliseconds(100));
    Regex regexImageStart = new Regex(@"\[\[(?:File|Datei):(.*?)(\||]])");
    Regex regexTaxoboxSimple = new Regex(@"{{TAXOBOX\n.*?\n\s*}}".Replace("TAXOBOX", taxoboxNamesRegex), RegexOptions.Singleline | RegexOptions.IgnoreCase);
    Regex regexTaxoboxOpener = new Regex(@"{{[ ]*TAXOBOX\b".Replace("TAXOBOX", taxoboxNamesRegex));
    Regex regexXmlComment = new Regex(@"<!--.*?-->", RegexOptions.Singleline);
    Regex regexDewikifiy = new Regex(@"(\[\[[^\]]*?([^\]|]*)\]\])");

    public String Dewikify(String text)
    {
        return regexDewikifiy.Replace(text, me => me.Groups[2].Value);
    }

    public String RemoveXmlComments(String text)
    {
        return regexXmlComment.Replace(text, "");
    }

    void Sanitize(ref String text)
    {
        text = text.ReplaceLineEndings("\n");

        text = RemoveXmlComments(text);
    }

    //public String GetTaxoboxWithRegex(String text)
    //{
    //    Sanitize(ref text);

    //    var matches = regexTaxoboxSimple.Match(text);

    //    if (!matches.Success) return null;

    //    return matches.Groups[0].Value;
    //}

    public String GetTaxoboxWithHeuristicParsing(String text)
    {
        Sanitize(ref text);

        var match = regexTaxoboxOpener.Match(text);

        if (!match.Success) return null;

        var startI = match.Index;

        var p = startI + 2;

        var level = 1;

        while (level > 0)
        {
            var openI = text.IndexOf("{{", p);
            var closeI = text.IndexOf("}}", p);

            if (closeI < 0) throw new Exception("Taxobox ending missing");

            if (openI < 0 || openI > closeI)
            {
                --level;

                p = closeI + 2;
            }
            else
            {
                ++level;

                p = openI + 2;
            }
        }

        return text.Substring(startI, p - startI);
    }
    public String GetTemplateName(String text)
    {
        Sanitize(ref text);

        if (!text.StartsWith("{{"))
        {
            throw new Exception("Taxobox doesn't start with '{{'");
        }

        var i = text.IndexOf('\n');

        return text.Substring(2, i - 2).Trim();
    }

    public void ParseIntoParsingResult(ParsingResult result, String text)
    {
        Sanitize(ref text);

        var templateName = GetTemplateName(text);

        if (templateName.Length > 60) throw new Exception($"Template name is too long");

        var lines = ParseLines(text).ToArray();

        var taxoboxEntries = new List<TaxoboxEntry>();

        var knownKeys = new HashSet<String>();

        foreach (var line in lines)
        {
            if (!knownKeys.Add(line.key))
            {
                result.HasDuplicateTaxoboxEntries = true;

                continue;
            }

            if (line.key.Length > 60) throw new Exception("Taxobox line key too long");

            taxoboxEntries.Add(new TaxoboxEntry { Lang = result.Lang, Title = result.Title, Key = line.key, Value = line.value.Truncate(80) });
        }

        result.TemplateName = templateName;
        result.TaxoboxEntries = taxoboxEntries;
    }

    public String ParseEntriesForTesting(String text)
    {
        Sanitize(ref text);

        text = text.Trim();

        return String.Join("\n", ParseLines(text).Select(p => $"{p.key} = {p.value}"));
    }

    IEnumerable<(String key, String value)> ParseLines(String text)
    {
        var head = "{{";

        if (!text.StartsWith(head)) throw new Exception($"Expected text to start with {head}");

        var p = head.Length;

        var i = 0;

        i = text.IndexOf('\n', p);
        var firstPipeI = text.IndexOf('|');

        if (firstPipeI > 0 && firstPipeI < i) throw new Exception("Found key on the Taxobox line which is unsupported");

        while (true)
        {
            i = text.IndexOf('\n', p);

            if (i < 0) yield break;

            p = i + 1;

            if (text[p] != '|') continue;

            ++p;

            i = text.IndexOf('=', p);

            if (i < 0) throw new ParsingException($"Value is missing for key", $"After <{text.Substring(p).Truncate(50)}>");

            var key = text.Substring(p, i - p).Trim();

            p = i + 1;

            i = text.IndexOf('\n', p);

            if (i < 0) throw new Exception("Taxobox ended mid-content");

            var value = text.Substring(p, i - p).Trim();

            yield return (key, value);

            p = i;
        }
    }

    enum TaxoboxField
    {
        Unknown,
        Rank,
        Name,
        NameDe
    }

    Boolean ParseTaxoKey(String key, out Int32 i, out String field)
    {
        i = 0;
        field = null;

        if (!key.StartsWith("taxon", StringComparison.InvariantCultureIgnoreCase)) return false;

        var underscoreI = key.IndexOf('_');

        if (underscoreI is not 5 and not 6) return false;

        field = key.Substring(underscoreI + 1);

        if (underscoreI == 5)
        {
            i = 1;
        }
        else
        {
            i = key[5] - '0';
        }

        return true;
    }

    public record ImageLink(String fileName, Int32 position, String wikiText);

    public String FindImageLinksForTesting(String text)
    {
        var links = FindImageLinks(text);

        return String.Join("\n", links.Select(l => l.fileName));
    }

    public IEnumerable<ImageLink> FindImageLinks(String text)
    {
        var matches = regexImageStart.Matches(text);

        var images = new List<ImageLink>();

        foreach (Match match in matches)
        {
            if (!match.Success) throw new Exception();

            var group = match.Groups[1];

            var fileName = group.Value;
            var position = group.Index;

            images.Add(new ImageLink(fileName.Trim(), position, null));
        }

        return images;
    }

    //public List<TaxoboxEntry> GetEntries(String text)
    //{
    //    text = text.ReplaceLineEndings("\n");


    //}
}
