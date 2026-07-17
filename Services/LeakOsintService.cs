using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using YourApp.Models;

namespace YourApp.Services
{
    public class LeakOsintService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<LeakOsintService> _logger;
        private readonly string _token;
        private readonly string _apiUrl;

        public LeakOsintService(
            HttpClient httpClient,
            AppDbContext dbContext,
            IConfiguration configuration,
            ILogger<LeakOsintService> logger)
        {
            _httpClient = httpClient;
            _dbContext = dbContext;
            _logger = logger;
            _token = configuration["LeakOsint:Token"] ??
                throw new InvalidOperationException("LeakOSINT token not configured");
            _apiUrl = configuration["LeakOsint:ApiUrl"] ??
                throw new InvalidOperationException("LeakOSINT API URL not configured");
        }

        public async Task<object> SearchAndSaveAsync(string query, string lang = "en", int limit = 100, string clientIP = "Unknown", string userAgent = "Unknown")
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync<object>(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync();

                try
                {
                    // Prepare request data
                    var requestData = new
                    {
                        token = _token,
                        request = query,
                        lang = lang,
                    };

                    var jsonRequest = JsonSerializer.Serialize(requestData);
                    var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                    var response = await _httpClient.PostAsync(_apiUrl, content);
                    response.EnsureSuccessStatusCode();

                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var leakData = JsonSerializer.Deserialize<LeakOSINTResponse>(jsonResponse);

                    if (leakData?.ErrorCode != null)
                    {
                        _logger.LogError("LeakOSINT API Error: {ErrorCode}", leakData.ErrorCode);
                        return new { success = false, error = leakData.ErrorCode };
                    }

                    if (leakData?.List == null || !leakData.List.Any())
                    {
                        _logger.LogWarning("No data found for query: {Query}", query);
                        return new { success = false, message = "No results found" };
                    }

                    // Create and save Search with IP and UserAgent
                    var search = new Search
                    {
                        Query = query,
                        Status = "completed",
                        Limit = limit,
                        Language = lang,
                        ClientIP = clientIP,
                        UserAgent = userAgent,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _dbContext.Searches.AddAsync(search);
                    await _dbContext.SaveChangesAsync();

                    // Process each database result
                    foreach (var databaseEntry in leakData.List)
                    {
                        var databaseName = databaseEntry.Key;
                        var databaseData = databaseEntry.Value;

                        var database = new Database
                        {
                            SearchId = search.Id,
                            DatabaseName = databaseName,
                            InfoLeak = databaseData.InfoLeak,
                            RecordCount = databaseData.Data?.Count ?? 0
                        };

                        await _dbContext.Databases.AddAsync(database);
                        await _dbContext.SaveChangesAsync();

                        if (databaseData.Data != null && databaseData.Data.Any())
                        {
                            var records = databaseData.Data.Select(record => new Record
                            {
                                DatabaseId = database.Id,
                                RawData = JsonDocument.Parse(JsonSerializer.Serialize(record))
                            });

                            await _dbContext.Records.AddRangeAsync(records);
                        }
                    }

                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation($"Successfully saved search results for: {query} from IP: {clientIP}");

                    return new
                    {
                        success = true,
                        searchId = search.Id,
                        data = leakData
                    } as object;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, $"Error processing LeakOSINT search for: {query}");

                    try
                    {
                        var errorSearch = new Search
                        {
                            Query = query,
                            Status = "failed",
                            ErrorMessage = ex.Message,
                            Limit = limit,
                            Language = lang,
                            ClientIP = clientIP,
                            UserAgent = userAgent,
                            CreatedAt = DateTime.UtcNow
                        };

                        await _dbContext.Searches.AddAsync(errorSearch);
                        await _dbContext.SaveChangesAsync();
                    }
                    catch (Exception dbEx)
                    {
                        _logger.LogError(dbEx, "Failed to log error to database");
                    }

                    throw;
                }
            });
        }

        //public async Task<object> SearchMultipleAsync(List<string> queries, int limit = 100, string lang = "en", string clientIP = "Unknown", string userAgent = "Unknown")
        //{
        //    var strategy = _dbContext.Database.CreateExecutionStrategy();

        //    return await strategy.ExecuteAsync<object>(async () =>
        //    {
        //        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        //        try
        //        {
        //            var combinedQuery = string.Join("\n", queries);

        //            var requestData = new
        //            {
        //                token = _token,
        //                request = combinedQuery,
        //                limit = limit,
        //                lang = lang,
        //                type = "json"
        //            };

        //            var jsonRequest = JsonSerializer.Serialize(requestData);
        //            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        //            var response = await _httpClient.PostAsync(_apiUrl, content);
        //            response.EnsureSuccessStatusCode();

        //            var jsonResponse = await response.Content.ReadAsStringAsync();
        //            var leakData = JsonSerializer.Deserialize<LeakOSINTResponse>(jsonResponse);

        //            if (leakData?.ErrorCode != null)
        //            {
        //                return new { success = false, error = leakData.ErrorCode } as object;
        //            }

        //            var results = new List<object>();
        //            foreach (var query in queries)
        //            {
        //                var search = new Search
        //                {
        //                    Query = query,
        //                    QueryType = "multiple",
        //                    Status = "completed",
        //                    Limit = limit,
        //                    Language = lang,
        //                    ClientIP = clientIP,
        //                    UserAgent = userAgent,
        //                    CreatedAt = DateTime.UtcNow
        //                };

        //                await _dbContext.Searches.AddAsync(search);
        //                await _dbContext.SaveChangesAsync();
        //                results.Add(new { query, searchId = search.Id });
        //            }

        //            await _dbContext.SaveChangesAsync();
        //            await transaction.CommitAsync();

        //            return new
        //            {
        //                success = true,
        //                results = results,
        //                data = leakData
        //            } as object;
        //        }
        //        catch (Exception ex)
        //        {
        //            await transaction.RollbackAsync();
        //            _logger.LogError(ex, "Error processing multiple LeakOSINT searches");
        //            throw;
        //        }
        //    });
        //}
    }
}