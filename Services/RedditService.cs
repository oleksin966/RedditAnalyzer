namespace RedditAnalyzer.Services;

public class RedditService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RedditService> _logger;

    public RedditService(HttpClient httpClient, ILogger<RedditService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<RedditPostRaw>> GetPostsAsync(string subreddit, int limit)
    {
        var name = subreddit.Replace("r/", "").Replace("/", "");
        var url = $"https://www.reddit.com/r/{name}/hot.json?limit={limit}";

        _logger.LogInformation("Loading posts from {Subreddit}", subreddit);

        _httpClient.DefaultRequestHeaders.UserAgent
            .ParseAdd("RedditAnalyzer/1.0");

        var response = await _httpClient.GetFromJsonAsync<RedditApiResponse>(url);

        var posts = response?.Data?.Children?
        .Select(c => c.Data)
        .ToList()
        ?? new List<RedditPostRaw>();

        foreach (var post in posts)
            post.ResolveHasMedia();

        return posts;
    }
}

public class RedditApiResponse
{
    public RedditApiData? Data { get; set; }
}

public class RedditApiData
{
    public List<RedditChild>? Children { get; set; }
}

public class RedditChild
{
    public RedditPostRaw Data { get; set; } = new();
}

public class RedditPostRaw
{
    [System.Text.Json.Serialization.JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("selftext")]
    public string SelfText { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("post_hint")]
    public string PostHint { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("is_gallery")]
    public bool IsGallery { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("has_text")]
    public bool HasText { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("keyword_matched")]
    public bool KeywordMatched { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("has_media")]
    public string HasMedia { get; set; } = string.Empty;

    public void ResolveHasMedia()
    {
        if (IsGallery)
        {
            HasMedia = "Gallery";
            return;
        }

        HasMedia = PostHint switch
        {
            "image" => "Image",
            "rich:video" => "Video",
            "hosted:video" => "Video",
            _ => string.Empty
        };
    }
}


