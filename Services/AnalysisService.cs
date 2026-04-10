using RedditAnalyzer.Models;

namespace RedditAnalyzer.Services;

public class AnalysisService
{
    private readonly RedditService _redditService;
    private readonly RedditHtmlParser _htmlParser;
    private readonly ILogger<AnalysisService> _logger;

    public AnalysisService(
        RedditService redditService,
        RedditHtmlParser htmlParser,
        ILogger<AnalysisService> logger)
    {
        _redditService = redditService;
        _htmlParser = htmlParser;
        _logger = logger;
    }

    public async Task<Dictionary<string, List<PostResult>>> AnalyzeAsync(AnalysisRequest request)
    {
        var result = new Dictionary<string, List<PostResult>>();

        _logger.LogInformation(
            "Using {Parser} parser",
            request.UseHtmlParser ? "HTML" : "API");

        var tasks = request.Items.Select(async item =>
        {
            // Choose parser based on request
            var posts = request.UseHtmlParser
                ? await _htmlParser.GetPostsAsync(item.Subreddit, request.Limit, item.Keywords)
                : await _redditService.GetPostsAsync(item.Subreddit, request.Limit);

            var filtered = posts
                .Where(p => item.Keywords.Any(k =>
                    p.Title.Contains(k, StringComparison.OrdinalIgnoreCase) ||
                    p.SelfText.Contains(k, StringComparison.OrdinalIgnoreCase)))
                .Select(p => new PostResult
                {
                    Title = p.Title,
                    HasMedia = p.HasMedia
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
}