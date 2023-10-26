using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Creational;

[TestClass]
public class NewParsingTests
{
    [DataRow("""foo""", """foo""")]
    [DataRow("""&amp;""", """&""")]
    [DataRow("""&apos;""", """'""")]
    [DataRow("""foo &amp; bar""", """foo & bar""")]
    [DataRow("""foobar""", """foo<!-- some comment -->bar""")]
    [DataRow("""
        <div>
            <span></span>
        </div>
        """, """<div><span></span></div>""")]
    [DataRow("""<div />""", """<div />""")]
    [DataRow("""
        <div>
            <span></span>
        </div>
        """, """<div>< span ></ span ></div>""")]
    [DataRow("""
        <div>
            <span>foo</span>
            <span>bar</span>
        </div>
        """, """<div><span>foo</span><span>bar</span></div>""")]
    [DataRow("""
        <div attr1="foo" attr2="bar">
            <span attr3="baz"></span>
        </div>
        """, """<div attr1="foo"  attr2 = "bar" ><span attr3="baz"></span></div>""")]
    [DataRow("""<lang><arg>de</arg><arg>foo</arg></lang>""", """{{lang|de|foo}}""")]
    [DataRow("""<a><_1>Lifeform</_1></a>""", """[Lifeform]""")]
    [DataRow("""<a><_1>Life</_1><_2>form</_2></a>""", """[Life form]""")]
    [DataRow("""<a><_1>Lifeform</_1></a>""", """[[Lifeform]]""")]
    [DataRow("""<a><_1>Life form</_1></a>""", """[[Life form]]""")]
    [DataRow("""<a><_1>Lifeform</_1><_2>the root</_2></a>""", """[[Lifeform|the root]]""")]
    [DataRow("""
        <Taxobox>
            <arg key="x">Anguilla-anguilla 1.jpg</arg>
        </Taxobox>
        """, """
                {{Taxobox
        | x = Anguilla-anguilla 1.jpg
        }}
        """)]
    [DataRow("""
        dirt<Taxobox><arg key="x">Anguilla-anguilla 1.jpg</arg></Taxobox>dirt
        """, """
        dirt
                {{Taxobox
        | x = Anguilla-anguilla 1.jpg
        }}
        dirt        
        """)]
    [DataRow("""
        <Taxobox>
            <arg key="Taxon_Name">Storchschnäbel</arg>
            <arg key="Taxon2_Name">Storchschnabelgewächse</arg>
        </Taxobox>
        """, """
        {{Taxobox
        | Taxon_Name       = Storchschnäbel
        | Taxon2_Name      = Storchschnabelgewächse
        }}        }}
        """)]
    [DataRow("""
        <Taxobox>
            <arg key="Taxon_Name">Storchschnäbel</arg>
            <arg key="Subbox">
                <Speciesbox>
                    <arg key="Taxon">foo</arg>
                    <arg key="Name">bar</arg>
                </Speciesbox>
            </arg>
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
            <arg key="Taxon_Name">Storchschnäbel</arg>
            <arg key="Taxon2_Name">Storchschnabelgewächse</arg>
            <arg key="Bildbeschreibung">(''<lang><arg>la</arg><arg>Geranium_pratense</arg></lang>'')</arg>
        </Taxobox>
        """, """
        {{Taxobox
        | Taxon_Name       = Storchschnäbel
        | Taxon2_Name      = Storchschnabelgewächse
        | Bildbeschreibung = (''{{lang|la|Geranium_pratense}}'')
        }}        }}
        """)]
    [DataRow("""
        <Taxobox>
            <arg key="Taxon_Name">Storchschnäbel</arg>
            <arg key="Taxon_Autor">
                <a>
                    <_1>Carl von Linné</_1>
                    <_2>L.</_2>
                </a>
            </arg>
            <arg key="Taxon2_Name">Storchschnabelgewächse</arg>
            <arg key="Bildbeschreibung"><a><_1>Wiesen-Storchschnabel</_1></a>(''<lang><arg>la</arg><arg>Geranium_pratense</arg></lang>'')</arg>
        </Taxobox>
        """, """
        {{Taxobox
        | Taxon_Name       = Storchschnäbel
        | Taxon_Autor      = [[Carl von Linné|L.]]
        | Taxon2_Name      = Storchschnabelgewächse
        | Bildbeschreibung = [[Wiesen-Storchschnabel]] (''{{lang|la|Geranium_pratense}}'')
        }}        }}
        """)]
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

    [DataRow("&", "&amp;")]
    [DataRow("&amp", "&amp")]
    [DataRow("&", "&")]
    [DataRow("foo & bar", "foo & bar")]
    [DataRow("<>", "&lt;&gt;")]
    [TestMethod]
    public void TestHtmlDecoding(String expected, String original)
    {
        var actual = WikiTextDecoder.DecodeText(original);

        Assert.AreEqual(expected, actual);
    }

    [DataRow("foo", "foo", "foo")]
    [DataRow("foo", "bar", "foo|bar")]
    [DataRow("Bufo", "\"Bufo\"", "Bufo|\"Bufo\"")]
    [TestMethod]
    public void TestTaxoTemplateLinkParsing(String expectedTitle, String expectedName, String original)
    {
        var values = new TaxoTemplateValues();

        values.FillLinkValues(original);

        Assert.AreEqual(expectedTitle, values.PageTitle);
        Assert.AreEqual(expectedName, values.Name);
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
