using Microsoft.Playwright;
using Microsoft.Extensions.Logging;
using RedditAnalyzer.Services.Utils;

namespace RedditAnalyzer.Services;


public class RedditHtmlParser
{
    private readonly ILogger<RedditHtmlParser> _logger;

    private readonly int PageTimeoutMs = 30000;
    private readonly int ScrollDelayMs = 2000;
    private readonly int MaxScrolls = 15;

    private readonly SemaphoreSlim _semaphore = new(3); 

    public RedditHtmlParser(ILogger<RedditHtmlParser> logger)
    {
        _logger = logger;
    }

    // =========================================================================
    // PUBLIC
    // =========================================================================

    public async Task<List<RedditPostRaw>> GetPostsAsync(
        string subreddit,
        int limit,
        IEnumerable<string>? keywords = null)
    {
        var name = subreddit.Replace("r/", "").Replace("/", "").Trim();
        var url = $"https://www.reddit.com/r/{name}/hot/";
        var kwList = keywords?
                        .Where(k => !string.IsNullOrWhiteSpace(k))
                        .Select(k => k.Trim())
                        .ToList()
                     ?? new List<string>();
        _logger.LogInformation("Keywords received: [{Keywords}]", string.Join(", ", kwList));

        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        try
        {
            // ── Context A : listing ──────────────────────────────────────────
            var listingContext = await CreateContextAsync(browser);
            var listingPage = await listingContext.NewPageAsync();

            _logger.LogInformation("Opening r/{Subreddit}", name);

            await NavigateAsync(listingPage, url);
            _logger.LogInformation("Page loaded: {Url}", url);

            var postsFound = await WaitForPostsAsync(listingPage);
            if (!postsFound)
            {
                _logger.LogWarning("No posts found, stopping");
                return new List<RedditPostRaw>();
            }

            _logger.LogInformation("Posts appeared on page");

            var posts = await ScrollAndCollectAsync(listingPage, limit);

            await listingContext.CloseAsync();
            _logger.LogInformation("Listing context closed");

            // ── Context B: open each post separately ───────────────────
            if (kwList.Count > 0)
                posts = await FilterByKeywordsAsync(browser, posts, kwList);

            return posts;
        }
        finally
        {
            await browser.CloseAsync();
            playwright.Dispose();
        }
    }

    // =========================================================================
    // BROWSER CONTEXT
    // =========================================================================

    private async Task<IBrowserContext> CreateContextAsync(IBrowser browser)
    {
        return await browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent =
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                "AppleWebKit/537.36 (KHTML, like Gecko) " +
                "Chrome/120.0.0.0 Safari/537.36"
        });
    }

    // =========================================================================
    // NAVIGATION
    // =========================================================================

    private async Task NavigateAsync(IPage page, string url)
    {
        await page.GotoAsync(url, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = PageTimeoutMs
        });
    }

    // =========================================================================
    // WAIT FOR POSTS
    // =========================================================================

    private async Task<bool> WaitForPostsAsync(IPage page)
    {
        try
        {
            await page.WaitForSelectorAsync("shreddit-post",
                new PageWaitForSelectorOptions { Timeout = PageTimeoutMs });

            return true;
        }
        catch
        {
            _logger.LogWarning("Posts selector not found");
            return false;
        }
    }

    // =========================================================================
    // SCROLL + COLLECT
    // =========================================================================

    private async Task<List<RedditPostRaw>> ScrollAndCollectAsync(IPage page, int limit)
    {
        var collected = new Dictionary<string, RedditPostRaw>();

        for (int scroll = 1; scroll <= MaxScrolls; scroll++)
        {
            var elements = await page.QuerySelectorAllAsync("shreddit-post");

            foreach (var element in elements)
            {
                var post = await ParsePostAsync(element);

                if (post != null && !collected.ContainsKey(post.Url))
                    collected[post.Url] = post;
            }

            _logger.LogInformation(
                "Scroll {Scroll}: visible={Visible}, collected={Collected}",
                scroll, elements.Count, collected.Count);

            if (collected.Count >= limit)
            {
                _logger.LogInformation("Reached limit {Limit}, stopping scroll", limit);
                break;
            }

            await page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");
            await Task.Delay(ScrollDelayMs);
        }

        _logger.LogInformation("Total collected: {Count}", collected.Count);

        return collected.Values.Take(limit).ToList();
    }

    // =========================================================================
    // PARSE SINGLE POST  (listing page)
    // =========================================================================

    private async Task<RedditPostRaw?> ParsePostAsync(IElementHandle element)
    {
        try
        {
            // --- Title ---
            var title = await element.GetAttributeAsync("post-title");

            if (string.IsNullOrWhiteSpace(title))
            {
                var h3 = await element.QuerySelectorAsync("h3");
                title = h3 != null ? await h3.InnerTextAsync() : string.Empty;
            }

            if (string.IsNullOrWhiteSpace(title))
                return null;

            // --- URL ---
            var permalink = await element.GetAttributeAsync("permalink");

            if (string.IsNullOrWhiteSpace(permalink))
                return null;

            var url = $"https://www.reddit.com{permalink}";

            // --- Post type ---
            var postType = await element.GetAttributeAsync("post-type") ?? string.Empty;

            // --- Derived flags ---
            var postHint = await ResolvePostHintAsync(element, url, postType);
            var hasText = postType == "self";
            var hasMedia = postType switch
            {
                "image" => "Image",
                "gallery" => "Gallery",
                "video" => "Video",
                _ => string.Empty
            };


            return new RedditPostRaw
            {
                Title = title.Trim(),
                Url = url,
                PostHint = postHint,
                HasText = hasText,
                HasMedia = hasMedia,
                SelfText = string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Error parsing post: {Message}", ex.Message);
            return null;
        }
    }

    // =========================================================================
    // RESOLVE POST HINT
    // =========================================================================

    private async Task<string> ResolvePostHintAsync(
        IElementHandle element,
        string postUrl,
        string postType)
    {
        if (postType == "image") return "image";
        if (postType == "video") return "video";
        if (postType == "gallery") return "gallery";
        if (postType == "self") return "self";

        var contentHref = await element.GetAttributeAsync("content-href") ?? string.Empty;

        if (ParserUtils.HasImageExtension(contentHref) || ParserUtils.HasImageExtension(postUrl))
            return "image";

        return string.Empty;
    }

    // =========================================================================
    // FETCH POST BODY — new isolated tab → read text → close
    // =========================================================================

    private async Task<string> FetchPostBodyAsync(IBrowser browser, string postUrl)
    {
        var context = await CreateContextAsync(browser);
        var postPage = await context.NewPageAsync();

        try
        {
            await NavigateAsync(postPage, postUrl);

            // wait until the post is loaded
            await postPage.WaitForSelectorAsync("shreddit-post",
                new PageWaitForSelectorOptions { Timeout = PageTimeoutMs });

            // read the text if present
            var body = await postPage.QuerySelectorAsync("[slot='text-body']");

            var text = body != null
                ? (await body.InnerTextAsync()).Trim()
                : string.Empty;

            _logger.LogInformation("Body length: {Length} chars", text.Length);

            return text;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Could not fetch body: {Message}", ex.Message);
            return string.Empty;
        }
        finally
        {
            await context.CloseAsync();
            _logger.LogInformation("Post context closed: {Url}", postUrl);
        }
    }

    // =========================================================================
    // PROCESS POST — open a post (with semaphore), fetch body text and check keywords
    // =========================================================================
    private async Task<RedditPostRaw?> ProcessPostAsync(
        IBrowser browser,
        RedditPostRaw post,
        List<string> keywords)
    {
        await _semaphore.WaitAsync();

        try
        {
            _logger.LogInformation("Opening post: {Title}", post.Title);

            var bodyText = await FetchPostBodyAsync(browser, post.Url);
            await Task.Delay(300);
            var titleMatch = ParserUtils.ContainsKeywords(post.Title, keywords);
            var bodyMatch = ParserUtils.ContainsKeywords(bodyText, keywords);

            if (titleMatch || bodyMatch)
            {
                _logger.LogInformation("Match found: {Title}", post.Title);

                post.SelfText = bodyText;
                post.KeywordMatched = true;

                return post;
            }

            _logger.LogDebug("No match: {Title}", post.Title);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Error processing post {Title}: {Message}", post.Title, ex.Message);
            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    // =========================================================================
    // FILTER BY KEYWORDS — open every post and search keywords in text
    // =========================================================================

    private async Task<List<RedditPostRaw>> FilterByKeywordsAsync(
        IBrowser browser,
        List<RedditPostRaw> posts,
        List<string> keywords)
    {
        var tasks = posts.Select(post => ProcessPostAsync(browser, post, keywords)).ToList();

        var results = await Task.WhenAll(tasks);

        var matched = results
            .Where(p => p != null)
            .Select(p => p!)
            .ToList();

        _logger.LogInformation("Matched {Count} posts by keywords", matched.Count);

        return matched;
    }
}