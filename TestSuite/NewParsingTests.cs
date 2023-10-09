using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Creational;

public class XmlParsingReceiver : WikiParsingReceiver
{
    Stack<XElement> stack;

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

    public override void OpenTemplate(Range name)
    {
        stack.Push(new XElement(GetRange(name)));
    }

    public override void CloseTemplate(Range template)
    {
        var child = stack.Pop();

        Top.Add(child);
    }

    public override void AddKey(Range name)
    {
        stack.Push(new XElement(GetRange(name).Trim()));
    }

    public override void EndValue(Range valueMarkup)
    {
        var child = stack.Pop();

        Top.Add(child);
    }
}

[TestClass]
public class NewParsingTests
{
    [DataRow("""foo""", """foo""")]
    [DataRow("""
        <Taxobox>
            <x>Anguilla-anguilla 1.jpg</x>
        </Taxobox>
        """, """
                {{Taxobox
        | x = Anguilla-anguilla 1.jpg
        }}
        """)]
    [DataRow("""
        dirt<Taxobox><x>Anguilla-anguilla 1.jpg</x></Taxobox>dirt
        """, """
        dirt
                {{Taxobox
        | x = Anguilla-anguilla 1.jpg
        }}
        dirt        
        """)]
    [DataRow("""
        <Taxobox>
            <Taxon_Name>Storchschnäbel</Taxon_Name>
            <Taxon2_Name>Storchschnabelgewächse</Taxon2_Name>
        </Taxobox>
        """, """
        {{Taxobox
        | Taxon_Name       = Storchschnäbel
        | Taxon2_Name      = Storchschnabelgewächse
        }}        }}
        """)]
    [DataRow("""
        <Taxobox>
            <Taxon_Name>Storchschnäbel</Taxon_Name>
            <Subbox>
                <Speciesbox>
                    <Taxon>foo</Taxon>
                    <Name>bar</Name>
                </Speciesbox>
            </Subbox>
        </Taxobox>
        """, """
        {{Taxobox
        | Taxon_Name  = Storchschnäbel
        | Subbox      = {{Speciesbox
        | Taxon = foo
        | Name = bar
        }}
        }}        }}
        """)]
    [DataRow("""
        <Taxobox>
            <Taxon_Name>Storchschnäbel</Taxon_Name>
            <Taxon2_Name>Storchschnabelgewächse</Taxon2_Name>
            <Bildbeschreibung>(''<lang><la></la><Geranium pratense></Geranium pratense></lang>'')</Bildbeschreibung>
        </Taxobox>
        """, """
        {{Taxobox
        | Taxon_Name       = Storchschnäbel
        | Taxon2_Name      = Storchschnabelgewächse
        | Bildbeschreibung = (''{{lang|la|Geranium pratense}}'')
        }}        }}
        """)]
    //[DataRow("""
    //    <Taxobox>
    //        <Taxon_Name>Storchschnäbel</Taxon_Name>
    //        <Taxon_Autor>Storchschnäbel</Taxon_Autor>
    //        <Taxon2_Name>Storchschnabelgewächse</Taxon2_Name>
    //        <Bildbeschreibung>Storchschnäbel</Bildbeschreibung>
    //    </Taxobox>
    //    """, """
    //    {{Taxobox
    //    | Taxon_Name       = Storchschnäbel
    //    | Taxon_Autor      = [[Carl von Linné|L.]]
    //    | Taxon2_Name      = Storchschnabelgewächse
    //    | Bildbeschreibung = [[Wiesen-Storchschnabel]] (''{{lang|la|Geranium pratense}}'')
    //    }}        }}
    //    """)]
    [TestMethod]
    public void TestParsing(String expected, String original)
    {
        var receiver = new XmlParsingReceiver();

        var wikiParser = new WikiParser(original, receiver);

        wikiParser.Parse();

        var resultElement = receiver.Top;

        var expectedElement = XElement.Parse($"<root>{expected}</root>");

        var expectedXml = PrettyPrintXml(expectedElement);
        var resultXml = PrettyPrintXml(resultElement);

        Assert.AreEqual(expectedXml, resultXml);
    }

    static String PrettyPrintXml(XElement element)
    {
        var stringBuilder = new StringBuilder();

        var settings = new XmlWriterSettings();
        settings.OmitXmlDeclaration = true;
        settings.Indent = true;
        settings.NewLineOnAttributes = true;

        using (var xmlWriter = XmlWriter.Create(stringBuilder, settings))
        {
            element.Save(xmlWriter);
        }

        return stringBuilder.ToString();
    }
}
