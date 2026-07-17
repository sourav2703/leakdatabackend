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
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LeakController(
            LeakOsintService leakService,
            AppDbContext dbContext,
            ILogger<LeakController> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _leakService = leakService;
            _dbContext = dbContext;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        // Helper method to get client IP
        private string GetClientIP()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return "Unknown";

            // Check for forwarded IP (when behind proxy/load balancer)
            var forwardedHeader = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedHeader))
            {
                // Get the first IP in the chain (client IP)
                return forwardedHeader.Split(',')[0].Trim();
            }

            // Check X-Real-IP
            var realIP = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIP))
            {
                return realIP;
            }

            // Fallback to connection remote IP
            return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        // Helper method to get User Agent
        private string GetUserAgent()
        {
            return _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "Unknown";
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

                var clientIP = GetClientIP();
                var userAgent = GetUserAgent();
                _logger.LogInformation($"Search '{request.Query}' from IP: {clientIP}, UserAgent: {userAgent}");

                var queryType = DetectQueryType(request.Query);

                var result = await _leakService.SearchAndSaveAsync(
                    request.Query,
                    "en",
                    100,
                    clientIP,
                    userAgent
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in search endpoint");
                return StatusCode(500, new { error = "An error occurred while processing your request" });
            }
        }

        //[HttpPost("search-multiple")]
        //public async Task<IActionResult> SearchMultiple([FromBody] MultipleSearchRequest request)
        //{
        //    try
        //    {
        //        if (request.Queries == null || !request.Queries.Any())
        //        {
        //            return BadRequest(new { error = "At least one query is required" });
        //        }

        //        var clientIP = GetClientIP();
        //        var userAgent = GetUserAgent();
        //        _logger.LogInformation($"Multiple search from IP: {clientIP}, UserAgent: {userAgent}");

        //        var result = await _leakService.SearchMultipleAsync(
        //            request.Queries,
        //            request.Limit ?? 100,
        //            request.Lang ?? "en",
        //            clientIP,
        //            userAgent
        //        );

        //        return Ok(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error in multiple search endpoint");
        //        return StatusCode(500, new { error = "An error occurred while processing your request" });
        //    }
        //}

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] int limit = 50)
        {
            try
            {
                var clientIP = GetClientIP();
                _logger.LogInformation($"GetHistory called from IP: {clientIP}");

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
                        s.ClientIP,
                        s.UserAgent,
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
                var clientIP = GetClientIP();
                _logger.LogInformation($"GetFullHistory called from IP: {clientIP}");

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
                    s.ClientIP,
                    s.UserAgent,
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
                var clientIP = GetClientIP();
                _logger.LogInformation($"GetSearchResult {id} called from IP: {clientIP}");

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
                var clientIP = GetClientIP();
                _logger.LogInformation($"GetStats called from IP: {clientIP}");

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

        [HttpGet("client-info")]
        public IActionResult GetClientInfo()
        {
            var ip = GetClientIP();
            var userAgent = GetUserAgent();
            var referer = Request.Headers["Referer"].ToString();

            return Ok(new
            {
                ip = ip,
                userAgent = userAgent,
                referer = referer,
                timestamp = DateTime.UtcNow
            });
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
    }

    public class MultipleSearchRequest
    {
        public List<string> Queries { get; set; } = new();
        public int? Limit { get; set; } = 100;
        public string? Lang { get; set; } = "en";
    }
}