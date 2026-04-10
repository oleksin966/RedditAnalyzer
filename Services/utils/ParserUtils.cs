namespace RedditAnalyzer.Services.Utils;

public static class ParserUtils
{
    private static readonly string[] ImageExtensions =
        { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    public static bool HasImageExtension(string url) =>
        !string.IsNullOrEmpty(url) &&
        ImageExtensions.Any(ext =>
            url.EndsWith(ext, StringComparison.OrdinalIgnoreCase));

    public static bool ContainsKeywords(string text, List<string> keywords) =>
        !string.IsNullOrEmpty(text) &&
        keywords.Any(kw =>
            text.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0);
}