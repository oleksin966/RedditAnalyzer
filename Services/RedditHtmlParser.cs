using HtmlAgilityPack;
using RedditAnalyzer.Services;

namespace RedditAnalyzer.Services;

public class RedditHtmlParser
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RedditHtmlParser> _logger;

    public RedditHtmlParser(HttpClient httpClient, ILogger<RedditHtmlParser> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<RedditPostRaw>> GetPostsAsync(string subreddit, int limit)
    {
        var name = subreddit.Replace("r/", "").Replace("/", "");
        var url = $"https://old.reddit.com/r/{name}/hot/";

        _logger.LogInformation("HTML parsing posts from {Subreddit}", subreddit);

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.UserAgent
            .ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

        try
        {
            var html = await _httpClient.GetStringAsync(url);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var posts = doc.DocumentNode
                .SelectNodes("//div[contains(@class,'thing') and contains(@class,'link')]");

            if (posts == null)
            {
                _logger.LogWarning("No posts found in HTML for {Subreddit}", subreddit);
                return new List<RedditPostRaw>();
            }

            var result = new List<RedditPostRaw>();

            foreach (var post in posts.Take(limit))
            {
                // title post 
                var titleNode = post.SelectSingleNode(".//a[contains(@class,'title')]");
                var title = titleNode?.InnerText?.Trim() ?? string.Empty;

                // URL post
                var url_post = titleNode?.GetAttributeValue("href", string.Empty) ?? string.Empty;

                if (url_post.StartsWith("/"))
                    url_post = $"https://old.reddit.com{url_post}";

                var postHint = string.Empty;
                var thumbnail = post.SelectSingleNode(".//img[@class='thumbnail']");
                if (thumbnail != null)
                {
                    var src = thumbnail.GetAttributeValue("src", "");
                    if (!string.IsNullOrEmpty(src) &&
                        !src.Contains("self") &&
                        !src.Contains("default") &&
                        !src.Contains("nsfw"))
                    {
                        postHint = "image";
                    }
                }

                if (!string.IsNullOrEmpty(title))
                {
                    result.Add(new RedditPostRaw
                    {
                        Title = HtmlEntity.DeEntitize(title), //decode &amp;
                        Url = url_post,
                        SelfText = string.Empty, // HTML has not text post
                        PostHint = postHint
                    });
                }
            }

            _logger.LogInformation(
                "HTML parser: знайдено {Count} постів з {Subreddit}",
                result.Count, subreddit);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError("HTML fetch error for {Subreddit}: {Message}", subreddit, ex.Message);
            throw;
        }
    }
}
