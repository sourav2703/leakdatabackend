using System.Text;
using System.Text.Json;

namespace LeakOsintApi.Services;

public class LeakOsintService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public LeakOsintService(HttpClient httpClient,
                            IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<string> SearchAsync(string query,
                                          int limit,
                                          string lang)
    {
        var body = new
        {
            token = _configuration["LeakOsint:Token"],
            request = query,
            limit,
            lang
        };

        var json = JsonSerializer.Serialize(body);

        var content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(
            _configuration["LeakOsint:ApiUrl"],
            content);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }
}