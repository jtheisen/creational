using System.Net;

namespace Creational.Server;

public static class PageExtensions
{
    public static String GetPageUrl(this WikiPage page)
    {
        if (page.Lang == "en")
        {
            var title = page.Title.Replace(' ', '_');

            return $"https://en.wikipedia.org/wiki/{WebUtility.UrlEncode(title)}";
        }
        else
        {
            return null;
        }
    }
}
