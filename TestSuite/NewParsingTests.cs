using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Creational;

[TestClass]
public class NewParsingTests
{
    [DataRow("""foo""", """foo""")]
    [DataRow("""<lang><de /><foo /></lang>""", """{{lang|de|foo}}""")]
    [DataRow("""<a><_1>Lifeform</_1></a>""", """[[Lifeform]]""")]
    [DataRow("""<a><_1>Lifeform</_1><_2>the root</_2></a>""", """[[Lifeform|the root]]""")]
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
            <Bildbeschreibung>(''<lang><la /><Geranium_pratense /></lang>'')</Bildbeschreibung>
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
            <Taxon_Name>Storchschnäbel</Taxon_Name>
            <Taxon_Autor>
                <a>
                    <_1>Carl von Linné</_1>
                    <_2>L.</_2>
                </a>
            </Taxon_Autor>
            <Taxon2_Name>Storchschnabelgewächse</Taxon2_Name>
            <Bildbeschreibung><a><_1>Wiesen-Storchschnabel</_1></a>(''<lang><la /><Geranium_pratense /></lang>'')</Bildbeschreibung>
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
