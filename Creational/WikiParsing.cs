using Humanizer;
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
    Regex regex = new Regex(@"^{{Taxobox\n(\|\s*(\w+)\s*=\s*([^\n]+)\n)*}}", RegexOptions.Multiline, TimeSpan.FromMilliseconds(100));

    public List<TaxoboxEntry> GetEntries(String text)
    {
        var matches = regex.Match(text.Replace("\r\n", "\n"));

        var groups = matches.Groups;

        var keys = groups[2].Captures;
        var values = groups[3].Captures;

        var entries = new List<TaxoboxEntry>();

        for (var i = 0; i < keys.Count; ++i)
        {
            var key = keys[i].Value;
            var value = values[i].Value;
            entries.Add(new TaxoboxEntry { Key = key, Value = value.Truncate(80) });
        }

        return entries;
    }

    //public List<TaxoboxEntry> GetEntries(String text)
    //{
    //    text = text.ReplaceLineEndings("\n");


    //}
}
