namespace Creational;

public static class ImageUrls
{
    const String expectedPrefix = "https://upload.wikimedia.org/wikipedia/commons/";

    public static Boolean TryParseWikiImageUrl(String url, out String container, out String file)
    {
        container = file = null;

        if (!url.StartsWith(expectedPrefix)) return false;

        if (url.Length <= expectedPrefix.Length + 5) return false;

        var tail = url[expectedPrefix.Length..];

        if (tail[1] != '/') return false;
        if (tail[4] != '/') return false;

        container = tail[..4];
        file = tail[5..];

        return true;
    }

    public static String GetThumbnailUrl(String container, String file)
        => $"{expectedPrefix}thumb/{container}/{file}/220px-{file}";

    public static String GetSignificantSegments(String url)
    {
        if (TryParseWikiImageUrl(url, out var container, out var file))
        {
            return $"{container}/{file}";
        }
        else
        {
            return null;
        }
    }

    public static String GetThumbnailImageUrl(this WikiResolvedImage image)
    {
        if (!TryParseWikiImageUrl(image.Uri, out var container, out var file)) return null;

        return $"https://upload.wikimedia.org/wikipedia/commons/thumb/{container}/{file}/220px-{file}";
    }
}
