namespace LeakOsintApi.Models;

public class SearchRequest
{
    public string Query { get; set; } = string.Empty;

    public int Limit { get; set; } = 100;

    public string Lang { get; set; } = "en";
}