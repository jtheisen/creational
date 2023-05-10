using System.Xml.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace Creational;

[XmlRoot("page", Namespace = "http://www.mediawiki.org/xml/export-0.10/")]
public class XPage
{
    [XmlElement("title")]
    public String Title { get; set; }

    [XmlElement("ns")]
    public Int32 Ns { get; set; }

    [XmlElement("id")]
    public Int32 Id { get; set; }

    [XmlElement("revision")]
    public XRevision Revision { get; set; }
}

[XmlType("revision", Namespace = "http://www.mediawiki.org/xml/export-0.10/")]
public class XRevision
{
    [XmlElement("id")]
    public Int32 Id { get; set; }

    [XmlElement("text")]
    public String Text { get; set; }

    [XmlElement("model")]
    public String Model { get; set; }
    [XmlElement("format")]
    public String Format { get; set; }
    [XmlElement("sha1")]
    public String Sha1 { get; set; }
}

public static class XmlExtensions
{
    public static IEnumerable<XPage> StreamElements(this XmlReader reader)
    {
        var serializer = new XmlSerializer(typeof(XPage));

        reader.MoveToContent();

        while (reader.Read())
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    if (reader.LocalName == "page")
                    {
                        var page = (XPage)serializer.Deserialize(reader);

                        yield return page;
                    }
                    break;
            }
        }
    }
}
