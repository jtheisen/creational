using Creational;
using System.Text;
using static Creational.Extensions;

namespace TestSuite;

[TestClass]
public class EncodingTruncatorTests
{
    [TestMethod]
    [DataRow("Th...", "...", 5, "This is a long text.")]
    [DataRow("äbc", "...", 5, "äbc")]
    [DataRow("ä...", "...", 5, "äbcde")]
    [DataRow(".", ".", 2, "äüö")]
    [DataRow("ä.", ".", 3, "äüö")]
    [DataRow("ä.", ".", 4, "äüö")]
    [DataRow("äü", ".", 4, "äü")]
    [DataRow("...", "...", 3, "This is some text")]
    public void TestUtf8Truncator(String expected, String ellipsis, Int32 maxBytes, String original)
    {
        var truncator = new EncodingTruncator(Encoding.UTF8, 1000, ellipsis);

        var actual = truncator.Truncate(original, maxBytes);

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    [DataRow("...", 2, "This is some text")]
    public void TestFailingUtf8Truncator(String ellipsis, Int32 maxBytes, String original)
    {
        var truncator = new EncodingTruncator(Encoding.UTF8, 1000, ellipsis);

        Assert.ThrowsException<Exception>(() => truncator.Truncate(original, maxBytes));
    }
}
