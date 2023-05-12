namespace Creational;

[TestClass]
public class DewikifyTests
{
    TaxoboxParser parser = new TaxoboxParser();

    [TestMethod]
    [DataRow("Peel & Stein, 2009", "[[John S. Peel|Peel]] & [[Martin Stein|Stein]], 2009")]
    [DataRow("Guillaumin", "[[André Guillaumin|Guillaumin]]")]
    [DataRow("Baz", "[[Foo|Bar|Baz]]")]
    [DataRow("ö", "[[ä|ü|ö]]")]
    [DataRow("foo", "[[foo]]")]
    [DataRow("a\nb", "[[a\nb]]")]
    public void TestDewikify(String expected, String original)
    {
        var actual = parser.Dewikify(original);

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    [DataRow("foobar", "foo<!-- some xml comment -->bar")]
    [DataRow("foobar", "foo<!-- \nsome xml comment\n -->bar")]
    [DataRow("foobar -->", "foo<!-- some xml comment -->bar -->")]
    [DataRow("foo\nbar", "foo\n<!-- some xml comment -->bar")]
    public void TestXmlComments(String expected, String original)
    {
        var actual = parser.RemoveXmlComments(original);

        Assert.AreEqual(expected, actual);
    }
}
