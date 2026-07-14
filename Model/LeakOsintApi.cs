using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace YourApp.Models
{
    // ==================== ENTITY MODELS ====================

    public class Search
    {
        public long Id { get; set; }
        public string Query { get; set; } = string.Empty;
        public string QueryType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public int Limit { get; set; } = 100;
        public string? Language { get; set; } = "en";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public ICollection<Database> Databases { get; set; } = new List<Database>();
    }

    public class Database
    {
        public long Id { get; set; }
        public long SearchId { get; set; }
        public string DatabaseName { get; set; } = string.Empty;
        public string? InfoLeak { get; set; }
        public int RecordCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Search Search { get; set; } = null!;
        public ICollection<Record> Records { get; set; } = new List<Record>();
    }

    public class Record
    {
        public long Id { get; set; }
        public long DatabaseId { get; set; }
        public JsonDocument RawData { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public Database Database { get; set; } = null!;
    }

    // ==================== API REQUEST MODELS ====================

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

    // ==================== API RESPONSE MODELS ====================

    public class LeakOSINTResponse
    {
        [JsonPropertyName("Error code")]
        public string? ErrorCode { get; set; }

        [JsonPropertyName("List")]
        public Dictionary<string, DatabaseData>? List { get; set; }
    }

    public class DatabaseData
    {
        [JsonPropertyName("InfoLeak")]
        public string InfoLeak { get; set; } = string.Empty;

        [JsonPropertyName("Data")]
        public List<Dictionary<string, object>> Data { get; set; } = new();
    }

    // ==================== SEARCH RESPONSE DTO ====================

    public class SearchResponse
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? Message { get; set; }
        public long? SearchId { get; set; }
        public LeakOSINTResponse? Data { get; set; }
    }

    // ==================== DATABASE CONTEXT ====================

  
}