using RedditAnalyzer.Models;

namespace RedditAnalyzer.Services;

public class AnalysisService
{
    private readonly RedditService _redditService;
    private readonly ILogger<AnalysisService> _logger;

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp"
    };

    public AnalysisService(RedditService redditService, ILogger<AnalysisService> logger)
    {
        _redditService = redditService;
        _logger = logger;
    }

    public async Task<Dictionary<string, List<PostResult>>> AnalyzeAsync(AnalysisRequest request)
    {
        var result = new Dictionary<string, List<PostResult>>();

        // Concurrency — all subreddits are loaded concurrently
        var tasks = request.Items.Select(async item =>
        {
            var posts = await _redditService.GetPostsAsync(item.Subreddit, request.Limit);

            var filtered = posts
                .Where(p => item.Keywords.Any(k =>
                    // Filter by title
                    p.Title.Contains(k, StringComparison.OrdinalIgnoreCase) ||
                    // Filter by post body
                    p.SelfText.Contains(k, StringComparison.OrdinalIgnoreCase)))
                .Select(p => new PostResult
                {
                    Title = p.Title,
                    // Check if the post has an image
                    HasImage = IsImage(p)
                })
                .ToList();

            _logger.LogInformation(
                "Subreddit {Sub}: знайдено {Count} постів",
                item.Subreddit, filtered.Count);

            return (item.Subreddit, filtered);
        });

        var results = await Task.WhenAll(tasks);

        foreach (var (subreddit, posts) in results)
        {
            result[$"/r/{subreddit.Replace("r/", "")}"] = posts;
        }

        return result;
    }

    private bool IsImage(RedditPostRaw post)
    {
        // Check by post_hint
        if (post.PostHint == "image") return true;

        // Check by URL extension
        if (string.IsNullOrEmpty(post.Url)) return false;

        var uri = new Uri(post.Url);
        var extension = Path.GetExtension(uri.AbsolutePath);

        return ImageExtensions.Contains(extension);
    }
}