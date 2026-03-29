using Microsoft.AspNetCore.Mvc;
using RedditAnalyzer.Models;
using RedditAnalyzer.Services;
using System.Text;
using System.Text.Json;

namespace RedditAnalyzer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RedditController : ControllerBase
{
    private readonly AnalysisService _analysisService;
    private readonly ILogger<RedditController> _logger;

    public RedditController(AnalysisService analysisService, ILogger<RedditController> logger)
    {
        _analysisService = analysisService;
        _logger = logger;
    }

    // Standard JSON result
    [HttpPost("analyze")]
    public async Task<IActionResult> Analyze([FromBody] AnalysisRequest request)
    {
        if (request.Items == null || request.Items.Count == 0)
            return BadRequest("The subreddit list is empty");

        try
        {
            var result = await _analysisService.AnalyzeAsync(request);
            return Ok(result);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError("Connection error to Reddit: {Message}", ex.Message);
            return StatusCode(503, "Reddit is unavailable, please try again later");
        }
        catch (Exception ex)
        {
            _logger.LogError("Unknown error: {Message}", ex.Message);
            return StatusCode(500, "Internal server error");
        }
    }

    // Result as a JSON file
    [HttpPost("analyze/download")]
    public async Task<IActionResult> AnalyzeAndDownload([FromBody] AnalysisRequest request)
    {
        if (request.Items == null || request.Items.Count == 0)
            return BadRequest("The subreddit list is empty");

        try
        {
            var result = await _analysisService.AnalyzeAsync(request);

            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var bytes = Encoding.UTF8.GetBytes(json);
            var fileName = $"reddit_analysis_{DateTime.Now:yyyyMMdd_HHmmss}.json";

            return File(bytes, "application/json", fileName);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError("Connection error to Reddit: {Message}", ex.Message);
            return StatusCode(503, "Reddit is unavailable, please try again later");
        }
        catch (Exception ex)
        {
            _logger.LogError("Unknown error: {Message}", ex.Message);
            return StatusCode(500, "Internal server error");
        }
    }
}