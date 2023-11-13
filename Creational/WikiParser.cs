using Creational.Migrations;
using Humanizer;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Net;
using System.Text;
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

    public virtual void AddText(String decodedText, Range textRange) { }

    public virtual void OpenLink() { }

    public virtual void AddLinkSeparator() { }

    public virtual void CloseLink(Range linkMarkup) { }

    public virtual void OpenTemplate(Range name) { }

    public virtual void OpenArgument() { }

    public virtual void NotifyArgumentEquals() { }

    public virtual void CloseArgument() { }

    public virtual void CloseTemplate(Range template) { }

    public virtual void OpenXmlTag(String name) { }

    public virtual void AddXmlAttribute(String name, String value) { }

    public virtual void CloseXmlTag(String name, Range element) { }
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

    public override void AddText(String decodedText, Range textRange)
    {
        Top.Add(decodedText.Trim());
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

    public override void OpenXmlTag(String name)
    {
        stack.Push(new XElement(name));
    }

    public override void AddXmlAttribute(String name, String value)
    {
        Top.Add(new XAttribute(name, value));
    }

    public override void CloseXmlTag(String name, Range element)
    {
        Pop(name);
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

    Boolean HaveEndOfInput => p == input.Length - 1;

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

    void AssertMoreInput()
    {
        if (HaveEndOfInput) Throw("Unexpected end of input");
    }

    Int32 LookBeyondWhitespace(String activityName)
    {
        for (var i = p; i < input.Length; ++i)
        {
            if (!Char.IsWhiteSpace(input[i])) return i;
        }

        Throw($"Unexpected end of input while {activityName}");

        return p;
    }

    void FlushText(Int32 i)
    {
        var textRange = new Range(this.p, i);

        var decodedText = WikiTextDecoder.DecodeText(input[textRange]);

        receiver.AddText(decodedText, textRange);

        this.p = i;
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
                    else
                    {
                        Throw("Unexpected single '{' found");
                    }
                    break;
                case '}':
                    if (GetNextChar(i) == '}')
                    {
                        FlushText(i);

                        return;
                    }

                    Throw("Unexpected single '}' found");
                    break;
                case '[':
                    {
                        FlushText(i);

                        ParseLink();

                        i = p;
                    }
                    break;
                case '<':
                    {
                        FlushText(i);

                        AssertMoreInput();

                        if (input[i + 1] == '/')
                        {
                            return;
                        }

                        ParseMarkup();

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

    void ParseMarkup()
    {
        if (input[p] != '<') throw new Exception("Expected an opening tag to start with '<'");

        if (input[p + 1] == '!')
        {
            if (input[p + 2] != '-' || input[p + 3] != '-') Throw("Expected '<!' to be followed by '--'");

            ParseComment();
        }
        else
        {
            ParseElement();
        }
    }

    void ParseComment()
    {
        if (!input[p..(p + 4)].StartsWith("<!--")) Throw("Expected a comment to start with '<!--'");

        var i = input.IndexOf("-->", p + 4);

        if (i < 0) Throw("Unabled to find end of comment");

        p = i + 3;
    }

    void ParseElement()
    {
        var s = p;

        var tagName = ParseOpeningTag(out var isSelfClosing);

        if (!isSelfClosing)
        {
            ParseWikiString();

            ParseClosingTag(tagName);
        }

        receiver.CloseXmlTag(tagName, new Range(s, p));
    }

    static String TagNameSpecialChars = "_-:.";

    String LookAheadForTagname(Boolean expectName = true)
    {
        var n = input.Length;

        for (var i = p; ; ++i)
        {
            if (i < n)
            {
                var c = input[i];

                if (Char.IsLetterOrDigit(c)) continue;

                if (TagNameSpecialChars.Contains(c)) continue;
            }

            if (i == p)
            {
                if (i == n)
                {
                    Throw($"Expected name character, but reached end of input");
                }
                else if (expectName)
                {
                    Throw($"Expected name character, but found '{input[p]}'");
                }
                else
                {
                    return null;
                }
            }

            return input.Substring(p, i - p);
        }
    }

    String ParseOpeningTag(out Boolean isSelfClosing)
    {
        isSelfClosing = false;

        if (input[p] != '<') throw new Exception("Expected an opening tag to start with '<'");

        ++p;

        p = LookBeyondWhitespace("parsing opening tag, leading whitespace");

        var tagName = LookAheadForTagname();

        p += tagName.Length;

        receiver.OpenXmlTag(tagName);

        ParseAttributes();

        if (input[p] == '/')
        {
            ++p;

            if (input[p] == '>')
            {
                isSelfClosing = true;

                ++p;
            }
            else
            {
                Throw("Found '/' but no '>' followed to end the tag");
            }
        }
        else if (input[p] == '>')
        {
            ++p;
        }
        else
        {
            Throw($"Expected opening tag to end with either '/>' or '>'");
        }

        AssertMoreInput();

        return tagName;
    }

    void ParseClosingTag(String tagName)
    {
        if (input[p] != '<') throw new Exception("Expected a closing tag to start with '<'");
        if (input[p + 1] != '/') throw new Exception("Expected a closing tag to start with '</'");

        p += 2;

        p = LookBeyondWhitespace("parsing closing tag");

        var actualTagName = LookAheadForTagname();

        if (tagName != actualTagName) Throw($"Expected element '{tagName}' to close, but got closing tag named '{actualTagName}'");

        p += actualTagName.Length;

        p = LookBeyondWhitespace("parsing closing tag");

        if (input[p] != '>') Throw($"Expected closing tag to end with '>'");

        ++p;
    }

    void ParseAttributes()
    {
        while (true)
        {
            p = LookBeyondWhitespace("parsing attributes, skipping to next attribute");

            var attributeName = LookAheadForTagname(expectName: false);

            if (attributeName == null)
            {
                return;
            }

            p += attributeName.Length;

            p = LookBeyondWhitespace("parsing attribute, skipping to '='");

            if (input[p] != '=') Throw("Expected '=' after attribute name");

            ++p;

            p = LookBeyondWhitespace("parsing attribute, skipping to '\"'");

            var attributeValue = input[p] == '"' ? ParseQuotedAttributeValue() : ParseUnqotedAttributeValue();

            receiver.AddXmlAttribute(attributeName, attributeValue);
        }
    }

    static String UnquotedAttributeExtraChars = "-_";

    Boolean IsUnquotedAttributeChar(Char c)
    {
        if (Char.IsLetterOrDigit(c)) return true;

        if (UnquotedAttributeExtraChars.Contains(c)) return true;

        return false;
    }

    String ParseUnqotedAttributeValue()
    {
        var i = p;

        while (i < input.Length)
        {
            var c = input[i];

            if (!IsUnquotedAttributeChar(c)) break;

            ++i;
        }

        var attributeValue = input.Substring(p, i - p);

        p = i;

        AssertMoreInput();

        return attributeValue;
    }

    String ParseQuotedAttributeValue()
    {
        if (input[p] != '"') Throw("Expected an attribute value to start with '\"'");

        AssertMoreInput();

        var i = input.IndexOf('"', p + 1);

        if (i < 0) Throw("No end of attribute value found");

        var attributeValue = input.Substring(p + 1, i - p - 1);

        p = i + 1;

        AssertMoreInput();

        return attributeValue;
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
            ParseWikiString(hadEquals ? "}|" : "}|=");

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
                default:
                    Throw("Unexpected garbage in template");
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
        try
        {
            var rootElement = XmlParsingReceiver.Parse(text);

            FillFromParsedTemplate(result, rootElement);
        }
        catch (Exception e)
        {
            result.Exception = e.Message;
        }
    }

    static String[] ValidTaxoboxNames = new[] { "Taxotemplate", "Taxobox", "Automatic_taxobox", "Speciesbox" };

    public static void FillFromParsedTemplate(this ParsingResult result, XElement rootElement)
    {
        var rootChildren = rootElement.Elements();

        if (rootChildren.Count() != 1) throw new Exception($"Root child has {rootChildren.Count()} children, expected only one, the taxobox");

        var taxoboxElement = rootElement.Elements().Single();

        var taxoboxName = taxoboxElement.Name.LocalName;

        if (!ValidTaxoboxNames.Contains(taxoboxName)) throw new Exception($"Unexpected taxobox name: {taxoboxName}");

        taxoboxElement.RemoveElementsByName("ref");

        XElement GetEntryElement(String key) => taxoboxElement.Elements()
            .SingleOrDefault(e => e.Attribute("key")?.Value.Equals(key, StringComparison.InvariantCultureIgnoreCase) == true);

        String GetStringEntry(String key, Int32 maxLength) => GetEntryElement(key)?.Value.TruncateUtf8(maxLength);

        result.TemplateName = taxoboxName;
        result.Genus = GetStringEntry("genus", 200);
        result.Species = GetStringEntry("species", 200);
        result.Taxon = GetStringEntry("taxon", 200);
        result.Parent = GetStringEntry("parent", 200);

        var imageElement = GetEntryElement("image");

        var images = GetImagesFromElement(imageElement, out var imageSituation);

        result.ImageSituation = imageSituation;

        if (images is not null)
        {
            result.TaxoboxImageEntries = images.Select(i => new TaxoboxImageEntry
            {
                Lang = result.Lang,
                Title = result.Title,
                Filename = i.Truncate(200)
            }).ToList();
        }
    }

    static IEnumerable<String> GetImagesFromElement(XElement imageElement, out PageImageSituation imageSituation)
    {
        if (imageElement is not null)
        {
            var imageList = imageElement.Element("Multiple_image");

            if (imageList is not null)
            {
                imageSituation = PageImageSituation.Multiple;

                return GetImagesFromImageList(imageList);
            }

            var text = imageElement.Value;

            if (text.Contains('|'))
            {
                imageSituation = PageImageSituation.Unsupported;

                return null;
            }

            imageSituation = PageImageSituation.Simple;

            return new[] { text };
        }
        else
        {
            imageSituation = PageImageSituation.NoEntry;
        }

        return null;
    }

    static IEnumerable<String> GetImagesFromImageList(XElement imageList)
    {
        foreach (var imageElement in imageList.Elements())
        {
            if (imageElement.Name.LocalName.StartsWith("image", StringComparison.InvariantCultureIgnoreCase))
            {
                yield return imageElement.Value;
            }
        }
    }

    public static void FillLinkValues(this TaxoTemplateValues values, String link)
    {
        var root = XmlParsingReceiver.Parse($"[[{link}]]");

        var linkElement = root.Elements().Single("Expected link parsing result to have one link element");

        var parts = linkElement.Elements().Select(e => e.Value).ToArray();

        if (parts.Length == 1)
        {
            values.PageTitle = parts[0].TruncateUtf8(200);
            values.Name = parts[0].TruncateUtf8(80);
        }
        else if (parts.Length == 2)
        {
            values.PageTitle = parts[0].TruncateUtf8(200);
            values.Name = parts[1].TruncateUtf8(80);
        }
        else
        {
            throw new Exception($"Expected link to have one or two parts (got: '{link}')");
        }
    }

    public static String StripFragment(String text)
    {
        var i = text.IndexOf('#');

        if (i >= 0)
        {
            text = text.Substring(i);
        }

        return text;
    }
}

public class WikiTextDecoder
{
    private readonly String input;

    Int32 p = 0;

    StringBuilder builder;

    public WikiTextDecoder(String input)
    {
        this.input = input;
        this.builder = new StringBuilder();
    }

    public static String DecodeText(String text)
    {
        var decoder = new WikiTextDecoder(text);

        decoder.Decode();

        return decoder.builder.ToString();
    }

    void Flush(Int32 i)
    {
        builder.Append(input[p..i]);

        p = i;
    }

    Char GetChar(Int32 i) => i < input.Length ? input[i] : '\0';

    void Decode()
    {
        Int32 i;

        while ((i = input.IndexOf('&', p)) >= 0)
        {
            Flush(i);

            ++i;

            Char c;

            while (Char.IsLetter(c = GetChar(i))) ++i;

            if (c == ';')
            {
                ++i;

                var entity = input[p..i];

                var decoded = WebUtility.HtmlDecode(entity);

                builder.Append(decoded);

                p = i;
            }
            else
            {
                Flush(i);
            }
        }

        Flush(input.Length);
    }

}