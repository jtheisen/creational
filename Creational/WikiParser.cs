using Creational.Migrations;
using Humanizer;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Xml.Linq;

namespace Creational;

/*
 * This is a proper wiki markup parser as far as I need it to parse taxoboxes.
 */

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

    public virtual void OpenArgument() { }

    public virtual void NotifyArgumentEquals() { }

    public virtual void CloseArgument() { }

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

    public override void OpenArgument()
    {
        stack.Push(new XElement("arg"));
    }

    public override void NotifyArgumentEquals()
    {
        var argElement = Top;

        var key = argElement.Value;

        argElement.RemoveAll();

        argElement.Add(new XAttribute("key", key));
    }

    public override void CloseArgument()
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

    void ParseWikiString(String stopChars = "")
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
                default:
                    if (stopChars.Contains(c))
                    {
                        FlushText(i);

                        return;
                    }
                    ++i;
                    break;
            }
        }

        FlushText(i);
    }

    static String InternalLinkStopChars = "]|";
    static String ExternalLinkStopChars = "] ";

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

        var stopChars = isInternal ? InternalLinkStopChars : ExternalLinkStopChars;

        while (true)
        {
            ParseWikiString(stopChars);

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

    //static Char[] TemplateContentTerminationCharacter = { '|', '=', '}' };

    Char LookAheadForName(out Int32 i)
    {
        var n = input.Length;

        for (i = p; ; ++i)
        {
            if (i == n) return '\0';

            var c = input[i];

            if (Char.IsLetterOrDigit(c)) continue;

            if (c == '_') continue;

            if (Char.IsWhiteSpace(c)) continue;

            return c;
        }
    }

    void ParseTemplate()
    {
        if (input.Substring(p, 2) != "{{") throw new Exception("Expected '{{'");

        var s = p;

        p += 2;

        var c = LookAheadForName(out var i);

        switch (c)
        {
            case '|':
            case '}':
                receiver.OpenTemplate(new Range(p, i));

                p = i;

                if (c == '|')
                {
                    ParseTemplateLines();
                }

                receiver.CloseTemplate(new Range(s, p));

                if (input.Substring(p, 2) != "}}") throw new Exception("Expected '}}'");

                p += 2;

                break;
            default:
                Throw($"Template name can't be followed by '{c}'", i);
                break;
        }
    }

    void ParseTemplateLines()
    {
        if (input[p] != '|') throw new Exception("Expected to be on a '|' character");

        ++p;

        var hadEquals = false;

        receiver.OpenArgument();

        while (true)
        {
            ParseWikiString("}|=");

            var c = input[p];

            switch (c)
            {
                case '}':
                    receiver.CloseArgument();
                    return;
                case '=':
                    if (hadEquals) Throw("Unexpectedly got second equals");
                    hadEquals = true;
                    receiver.NotifyArgumentEquals();
                    ++p;
                    break;
                case '|':
                    hadEquals = false;
                    receiver.CloseArgument();
                    receiver.OpenArgument();
                    ++p;
                    break;
            }
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


public static class WikiParserExtensions
{
    public static void FillByWikiParser(this ParsingResult result, String text)
    {
        XElement rootElement = null;

        try
        {
            rootElement = XmlParsingReceiver.Parse(text);

            FillInternal(result, rootElement);
        }
        catch (Exception e)
        {
            result.Exception = e.Message;
        }
    }

    static String[] ValidTaxoboxNames = new[] { "Taxobox", "Automatic_taxobox", "Speciesbox" };

    static void FillInternal(ParsingResult result, XElement rootElement)
    {
        var rootChildren = rootElement.Elements();

        if (rootChildren.Count() != 1) throw new Exception($"Root child has {rootChildren.Count()} children, expected only one, the taxobox");

        var taxoboxElement = rootElement.Elements().Single();

        var taxoboxName = taxoboxElement.Name.LocalName;

        if (!ValidTaxoboxNames.Contains(taxoboxName)) throw new Exception($"Unexpected taxobox name: {taxoboxName}");

        var taxoboxEntries = new List<TaxoboxEntry>();

        var knownKeys = new HashSet<String>();

        foreach (var line in taxoboxElement.Elements())
        {
            var key = line.Attribute("key")?.Value;

            if (key is null) throw new Exception($"Taxobox has keyless argument");

            if (!knownKeys.Add(key))
            {
                result.HasDuplicateTaxoboxEntries = true;

                continue;
            }

            if (key.Length > 60) throw new Exception("Taxobox line key too long");

            var value = line.Value;

            taxoboxEntries.Add(new TaxoboxEntry { Lang = result.Lang, Title = result.Title, Key = key, Value = value.Truncate(80) });
        }

        result.WithTaxobox = taxoboxEntries.Count > 0;
        result.TemplateName = taxoboxName;
        result.TaxoboxEntries = taxoboxEntries;
    }
}