using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using YourApp.Services;
using YourApp.Models;

namespace YourApp.Controllers
{
    [ApiController]
    [Route("api/")]
    public class LeakController : ControllerBase
    {
        private readonly LeakOsintService _leakService;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<LeakController> _logger;

        public LeakController(
            LeakOsintService leakService,
            AppDbContext dbContext,
            ILogger<LeakController> logger)
        {
            _leakService = leakService;
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] SearchRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Query))
                {
                    return BadRequest(new { error = "Query is required" });
                }

                var queryType = DetectQueryType(request.Query);

                if (!string.IsNullOrEmpty(request.QueryType))
                {
                    queryType = request.QueryType;
                }

                var result = await _leakService.SearchAndSaveAsync(
                    request.Query,
                    request.Limit ?? 100,
                    request.Lang ?? "en"
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in search endpoint");
                return StatusCode(500, new { error = "An error occurred while processing your request" });
            }
        }

        [HttpPost("search-multiple")]
        public async Task<IActionResult> SearchMultiple([FromBody] MultipleSearchRequest request)
        {
            try
            {
                if (request.Queries == null || !request.Queries.Any())
                {
                    return BadRequest(new { error = "At least one query is required" });
                }

                var result = await _leakService.SearchMultipleAsync(
                    request.Queries,
                    request.Limit ?? 100,
                    request.Lang ?? "en"
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in multiple search endpoint");
                return StatusCode(500, new { error = "An error occurred while processing your request" });
            }
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] int limit = 50)
        {
            try
            {
                var history = await _dbContext.Searches
                    .OrderByDescending(s => s.CreatedAt)
                    .Take(limit)
                    .Select(s => new
                    {
                        s.Id,
                        s.Query,
                        s.QueryType,
                        s.Status,
                        s.Limit,
                        s.Language,
                        s.CreatedAt,
                        DatabaseCount = s.Databases.Count
                    })
                    .ToListAsync();

                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching search history");
                return StatusCode(500, new { error = "Failed to fetch history" });
            }
        }

        [HttpGet("history/full")]
        public async Task<IActionResult> GetFullHistory([FromQuery] int limit = 50)
        {
            try
            {
                // First get the data with includes
                var searches = await _dbContext.Searches
                    .Include(s => s.Databases)
                        .ThenInclude(d => d.Records)
                    .OrderByDescending(s => s.CreatedAt)
                    .Take(limit)
                    .ToListAsync();

                // Then transform the data in memory
                var history = searches.Select(s => new
                {
                    s.Id,
                    s.Query,
                    s.QueryType,
                    s.Status,
                    s.Limit,
                    s.Language,
                    s.CreatedAt,
                    DatabaseCount = s.Databases.Count,
                    Databases = s.Databases.Select(d => new
                    {
                        d.DatabaseName,
                        d.InfoLeak,
                        d.RecordCount,
                        Records = d.Records.Select(r =>
                            JsonSerializer.Deserialize<Dictionary<string, object>>(
                                r.RawData.RootElement.GetRawText()
                            )
                        ).ToList()
                    }).ToList()
                }).ToList();

                return Ok(new
                {
                    success = true,
                    total = history.Count,
                    history = history
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching full history");
                return StatusCode(500, new { error = "Failed to fetch history" });
            }
        }

        [HttpGet("result/{id}")]
        public async Task<IActionResult> GetSearchResult(long id)
        {
            try
            {
                var search = await _dbContext.Searches
                    .Include(s => s.Databases)
                        .ThenInclude(d => d.Records)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (search == null)
                {
                    return NotFound(new { error = "Search not found" });
                }

                var result = new
                {
                    ErrorCode = (string?)null,
                    List = search.Databases.ToDictionary(
                        d => d.DatabaseName,
                        d => new
                        {
                            InfoLeak = d.InfoLeak,
                            Data = d.Records.Select(r =>
                                JsonSerializer.Deserialize<Dictionary<string, object>>(
                                    r.RawData.RootElement.GetRawText()
                                )
                            ).ToList()
                        }
                    )
                };

                return Ok(new
                {
                    success = true,
                    searchId = search.Id,
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching search result");
                return StatusCode(500, new { error = "Failed to fetch search result" });
            }
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var totalSearches = await _dbContext.Searches.CountAsync();
                var completedSearches = await _dbContext.Searches.CountAsync(s => s.Status == "completed");
                var failedSearches = await _dbContext.Searches.CountAsync(s => s.Status == "failed");
                var totalRecords = await _dbContext.Records.CountAsync();
                var totalDatabases = await _dbContext.Databases.CountAsync();

                return Ok(new
                {
                    success = true,
                    stats = new
                    {
                        totalSearches,
                        completedSearches,
                        failedSearches,
                        totalRecords,
                        totalDatabases,
                        successRate = totalSearches > 0
                            ? Math.Round((double)completedSearches / totalSearches * 100, 2)
                            : 0
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching stats");
                return StatusCode(500, new { error = "Failed to fetch stats" });
            }
        }

        private string DetectQueryType(string query)
        {
            if (query.StartsWith("+") && query.Replace("+", "").Replace(" ", "").All(char.IsDigit))
            {
                return "phone";
            }
            else if (query.Contains("@") && query.Contains("."))
            {
                return "email";
            }
            else if (query.Contains(".") && !query.Contains(" ") && !query.Contains("@"))
            {
                return "domain";
            }
            return "text";
        }
    }

    public class SearchRequest
    {
        public string Query { get; set; } = string.Empty;
        public string? QueryType { get; set; }
        public int? Limit { get; set; } = 100;
        public string? Lang { get; set; } = "en";
    }

    public class MultipleSearchRequest
    {
        public List<string> Queries { get; set; } = new();
        public int? Limit { get; set; } = 100;
        public string? Lang { get; set; } = "en";
    }
}