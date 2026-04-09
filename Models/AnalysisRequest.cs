namespace RedditAnalyzer.Models;

public class AnalysisRequest
{
    public List<SubredditQuery> Items { get; set; } = new();
    public int Limit { get; set; } = 25;
    public bool UseHtmlParser { get; set; } = false; 

}

public class SubredditQuery
{
    public string Subreddit { get; set; } = string.Empty;
    public List<string> Keywords { get; set; } = new();
}