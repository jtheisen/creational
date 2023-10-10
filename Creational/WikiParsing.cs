using Creational.Migrations;
using Humanizer;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Creational;

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

public abstract class WikiParsingReceiver
{
    protected String Text { get; private set; }

    public virtual void Start(String text)
    {
        Text = text;
    }

    public virtual void Stop(Range range) { }

    protected String GetRange(Range range) => Text[range].Trim();

    public virtual void AddText(Range text) { }

    public virtual void OpenLink() { }

    public virtual void AddLinkSeparator() { }

    public virtual void CloseLink(Range linkMarkup) { }

    public virtual void OpenTemplate(Range name) { }

    public virtual void AddKey(Range name) { }

    public virtual void EndValue(Range valueMarkup) { }

    public virtual void CloseTemplate(Range template) { }
}

public class XmlParsingReceiver : WikiParsingReceiver
{
    public static XElement Parse(String text)
    {
        var receiver = new XmlParsingReceiver();

        var parser = new WikiParser(text, receiver);

        parser.Parse();

        return receiver.Top;
    }

    public static String ParseToString(String text)
    {
        return Parse(text).ToString();
    }

    Stack<XElement> stack;

    void Pop(String name = null)
    {
        var child = stack.Pop();

        if (name is not null)
        {
            if (child.Name.LocalName != name) throw new Exception($"Expected to have an element '{name}' on the stack");
        }

        Top.Add(child);
    }

    public XElement Top => stack.Peek();

    public override void Start(String text)
    {
        base.Start(text);

        stack = new Stack<XElement>();

        stack.Push(new XElement("root"));
    }

    public override void Stop(Range range)
    {
        if (stack.Count != 1) throw new Exception("Expected a singleton stack");

        base.Stop(range);
    }

    public override void AddText(Range text)
    {
        Top.Add(GetRange(text).Trim());
    }

    public override void OpenLink()
    {
        stack.Push(new XElement("a"));
        stack.Push(new XElement("_1"));
    }

    public override void AddLinkSeparator()
    {
        var child = stack.Pop();

        Top.Add(child);

        var previousName = child.Name.LocalName;

        var previousNumber = Int32.Parse(previousName.TrimStart('_'));

        var newName = $"_{previousNumber + 1}";

        stack.Push(new XElement(newName));
    }

    public override void CloseLink(Range linkMarkup)
    {
        Pop();
        Pop("a");
    }

    public override void OpenTemplate(Range name)
    {
        stack.Push(new XElement(GetNameRange(name)));
    }

    public override void CloseTemplate(Range template)
    {
        Pop();
    }

    public override void AddKey(Range name)
    {
        stack.Push(new XElement(GetNameRange(name)));
    }

    public override void EndValue(Range valueMarkup)
    {
        Pop();
    }

    String GetNameRange(Range range)
    {
        var name = GetRange(range).Trim().Replace(' ', '_');

        if (!Char.IsLetter(name[0]))
        {
            name = "_" + name;
        }

        return name;
    }
}

public class WikiParser
{
    readonly String input;
    readonly WikiParsingReceiver receiver;

    Int32 p;

    public WikiParser(String input, WikiParsingReceiver receiver)
    {
        this.input = input;
        this.receiver = receiver;
    }

    public void Parse()
    {
        receiver.Start(input);

        ParseWikiString();

        receiver.Stop(new Range(0, p));
    }

    void FlushText(Int32 p)
    {
        receiver.AddText(new Range(this.p, p));

        this.p = p;
    }

    Char GetNextChar(Int32 i)
    {
        return input.Length > i + 1 ? input[i + 1] : default;
    }

    void ParseWikiString(Boolean stopAtPipe = false, Boolean stopAtCloseBracket = false, Boolean stopAtSpace = false)
    {
        var i = p;

        var n = input.Length;

        if (i == n) Throw("Unexpected empty text to parse");

        while (i < n)
        {
            var c = input[i];

            switch (c)
            {
                case '{':
                    if (GetNextChar(i) == '{')
                    {
                        FlushText(i);

                        ParseTemplate();

                        i = p;
                    }
                    break;
                case '}':
                    if (GetNextChar(i) == '}')
                    {
                        FlushText(i);

                        return;
                    }
                    break;
                case '[':
                    {
                        FlushText(i);

                        ParseLink();

                        i = p;
                    }
                    break;
                case ']':
                    if (stopAtCloseBracket)
                    {
                        FlushText(i);

                        return;
                    }
                    ++i;
                    break;
                case '|':
                    if (stopAtPipe)
                    {
                        FlushText(i);

                        return;
                    }
                    ++i;
                    break;
                case ' ':
                    if (stopAtSpace)
                    {
                        FlushText(i);

                        return;
                    }
                    ++i;
                    break;
                default:
                    ++i;
                    break;
            }
        }

        FlushText(i);
    }

    void ParseLink()
    {
        var s = p;

        if (input[p] != '[') Throw("Expected link expression to start with '['");

        ++p;

        var isInternal = input[p] == '[';

        if (isInternal)
        {
            ++p;
        }

        receiver.OpenLink();

        while (true)
        {
            ParseWikiString(stopAtPipe: isInternal, stopAtCloseBracket: true, stopAtSpace: !isInternal);

            switch (input[p])
            {
                case ']':
                    ++p;
                    if (isInternal)
                    {
                        if (input[p] != ']') Throw("Expected a second ']' to close the link");
                        ++p;
                    }
                    receiver.CloseLink(new Range(s, p));
                    return;
                case '|':
                case ' ':
                    receiver.AddLinkSeparator();
                    ++p;
                    break;
                default:
                    Throw("Unexpected end of link expression");
                    break;
            }
        }
    }

    static Char[] TemplateContentTerminationCharacter = { '|', '=', '}' };

    void ParseTemplate()
    {
        if (input.Substring(p, 2) != "{{") throw new Exception("Expected '{{'");

        var s = p;

        var i = input.IndexOfAny(TemplateContentTerminationCharacter, p);

        if (i < 0) Throw("Expected to find a character to terminate template name", i);

        switch (input[i])
        {
            case '=':
                Throw("Template name can't be followed by '='", i);
                break;
            default:
                break;
        }

        receiver.OpenTemplate(new Range(s + 2, i));

        p = i;

        ParseTemplateLines();

        if (input.Substring(p, 2) != "}}") throw new Exception("Expected '}}'");

        p += 2;

        receiver.CloseTemplate(new Range(s, p));
    }

    void ParseTemplateLines()
    {
        while (true)
        {
            var i = p;

            while (input.Length > i && Char.IsWhiteSpace(input[i])) ++i;

            if (input.Length <= i) Throw("Unexpected end of unclosed template");

            var c = input[i];

            switch (c)
            {
                case '|':
                    break;
                case '}':
                    if (input.Length <= i + 1 || input[i + 1] != '}')
                    {
                        Throw("Got '}' on inner template context, but it wasn't followed up by a second '}'", i);
                    }

                    p = i;
                    return;
                default:
                    Throw($"Expected '|' to indicate a key-value pair, but got '{c}'", i);
                    break;
            }

            var ei = input.IndexOfAny(TemplateContentTerminationCharacter, i + 1);

            if (ei < 0) Throw("Expected to find a character to terminate key", ei);

            receiver.AddKey(new Range(i + 1, ei));

            p = ei;

            switch (input[ei])
            {
                case '|':
                    break;
                case '}':
                    if (input.Length <= p + 1 || input[p + 1] != '}')
                    {
                        Throw("Got '}' on inner template context, but it wasn't followed up by a second '}'", i);
                    }

                    break;
                case '=':
                    ++p;
                    ParseWikiString(stopAtPipe: true);
                    break;
            }

            receiver.EndValue(new Range(ei, p));
        }
    }

    void Throw(String message, Int32? pos = null)
    {
        var cn = 10;

        var p = pos ?? this.p;

        var pi = Math.Max(0, p - cn);
        var si = Math.Min(input.Length, p + cn);

        throw new ParsingException(message, $"at >{input[pi..p]}*{input[p..si]}<");
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

    public record Result(TaxoboxEntry[] TaxoboxEntries, TaxonomyEntry[] TaxonomyEntries);

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

    public void GetEntries(ParsingResult result, String text)
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
            if (te?.Name is null && te?.NameLocal is null) continue;

            taxonomyEntriesToWrite.Add(te);
        }

        result.WithTaxobox = taxoboxEntries.Count > 0;
        result.TemplateName = templateName;
        result.TaxoboxEntries = taxoboxEntries;
        result.TaxonomyEntries = taxonomyEntriesToWrite;
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

    void ParseAndFillTaxoEntry(TaxonomyEntry[] taxonomyEntries, TaxoboxEntry taxoboxEntry, out Boolean haveTruncationIssue)
    {
        haveTruncationIssue = false;

        if (ParseTaxoKey(taxoboxEntry.Key, out var i, out var fieldName))
        {
            var field = ParseTaxoboxFieldName(fieldName);

            if (field is null) return;

            ref var target = ref taxonomyEntries[i];

            target ??= new TaxonomyEntry();

            target.Lang = taxoboxEntry.Lang;
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

    static HeuristicTaxoboxParser()
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
            GetProp(nameof(TaxonomyEntry.NameLocal), TaxoboxField.Rank, "name")
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
