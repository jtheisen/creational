using Humanizer;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Creational;

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

public class TaxoboxParser
{
    //Regex infoboxSimpleMatcher = new Regex(@"^{{Taxobox.*");
    Regex regexTaxobox = new Regex(@"^{{Taxobox\n(\|\s*(\w+)\s*=\s*([^\n]+)\n)*}}", RegexOptions.Multiline, TimeSpan.FromMilliseconds(100));
    Regex regexXmlComment = new Regex(@"<!--.*?-->", RegexOptions.Singleline);
    Regex regexDewikifiy = new Regex(@"(\[\[[^\]]*?([^\]|]*)\]\])");

    public record Result(TaxoboxEntry[] TaxoboxEntries, TaxonomyEntry[] TaxonomyEntries);

    public String Dewikify(String text)
    {
        return regexDewikifiy.Replace(text, me => me.Groups[2].Value);
    }

    public String RemoveXmlComments(String text)
    {
        return regexXmlComment.Replace(text, "");
    }

    public void GetEntries(ParsingResult result, String text)
    {
        text = text.ReplaceLineEndings("\n");

        text = RemoveXmlComments(text);

        var matches = regexTaxobox.Match(text);

        var groups = matches.Groups;

        var keys = groups[2].Captures;
        var values = groups[3].Captures;

        var taxoboxEntries = new List<TaxoboxEntry>();

        for (var i = 0; i < keys.Count; ++i)
        {
            var key = keys[i].Value;
            var value = values[i].Value;
            taxoboxEntries.Add(new TaxoboxEntry { Key = key, Value = value.Truncate(80) });
        }

        var taxonomyEntries = new TaxonomyEntry[10];

        var taxonomyEntriesToWrite = new List<TaxonomyEntry>();
        foreach (var entry in taxoboxEntries)
        {
            if (!entry.Key.StartsWith("taxon", StringComparison.InvariantCultureIgnoreCase)) continue;

            ParseAndFillTaxoEntry(taxonomyEntries, entry, out var haveTruncationIssue);

            if (haveTruncationIssue)
            {
                result.HasTruncationIssue = true;
            }
        }

        foreach (var te in taxonomyEntries)
        {
            if (te?.Name is null) continue;

            taxonomyEntriesToWrite.Add(te);
        }

        result.WithTaxobox = taxoboxEntries.Count > 0;
        result.TaxoboxEntries = taxoboxEntries;
        result.TaxonomyEntries = taxonomyEntriesToWrite;
    }

    void ParseAndFillTaxoEntry(TaxonomyEntry[] taxonomyEntries, TaxoboxEntry taxoboxEntry, out Boolean haveTruncationIssue)
    {
        haveTruncationIssue = false;

        if (ParseTaxoKey(taxoboxEntry.Key, out var i, out var fieldName))
        {
            var field = ParseTaxoboxFieldName(fieldName);

            if (field is null) return;

            ref var target = ref taxonomyEntries[i];

            target ??= new TaxonomyEntry();

            target.No = i;

            var value = Dewikify(taxoboxEntry.Value).Truncate(50);

            if (value.Length == 50)
            {
                haveTruncationIssue = true;
            }

            field.property.SetValue(target, value);
        }
    }

    enum TaxoboxField
    {
        Unknown,
        Rank,
        Name,
        NameDe
    }

    record TaxonomyEntryProperty(TaxoboxField field, String deName, PropertyInfo property);

    static TaxonomyEntryProperty[] taxonomyEntryProperties;

    static TaxoboxParser()
    {
        var type = typeof(TaxonomyEntry);

        TaxonomyEntryProperty GetProp(String propertyName, TaxoboxField field, String deName)
        {
            var property = type.GetProperty(propertyName) ?? throw new Exception("Such such property");

            return new TaxonomyEntryProperty(field, deName, property);
        }

        taxonomyEntryProperties = new[]
        {
            GetProp(nameof(TaxonomyEntry.Rank), TaxoboxField.Rank, "rang"),
            GetProp(nameof(TaxonomyEntry.Name), TaxoboxField.Rank, "wissname"),
            GetProp(nameof(TaxonomyEntry.NameDe), TaxoboxField.Rank, "name")
        };
    }

    TaxonomyEntryProperty ParseTaxoboxFieldName(String field)
    {
        foreach (var prop in taxonomyEntryProperties)
        {
            if (!field.Equals(prop.deName, StringComparison.InvariantCultureIgnoreCase)) continue;

            return prop;
        }

        return null;
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

    

    //public List<TaxoboxEntry> GetEntries(String text)
    //{
    //    text = text.ReplaceLineEndings("\n");


    //}
}
