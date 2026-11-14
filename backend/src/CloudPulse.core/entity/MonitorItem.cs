using System.Text.Json.Serialization;
using Amazon.DynamoDBv2.DataModel;

namespace CloudPulse.Core.Entity;

[DynamoDBTable("CloudPulse_Monitors")]
public class MonitorItem
{
    [DynamoDBHashKey("monitorId")]
    [JsonPropertyName("monitorId")]
    public string MonitorId { get; set; } = Guid.NewGuid().ToString();

    [DynamoDBProperty("name")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [DynamoDBProperty("url")]
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [DynamoDBProperty("status")]
    [JsonPropertyName("status")]
    public string Status { get; set; } = MonitorStatus.Unknown;

    [DynamoDBProperty("intervalSeconds")]
    [JsonPropertyName("intervalSeconds")]
    public int IntervalSeconds { get; set; } = 60;

    [DynamoDBProperty("consecutiveFailures")]
    [JsonPropertyName("consecutiveFailures")]
    public int ConsecutiveFailures { get; set; } = 0;

    [DynamoDBProperty("lastCheckedAt")]
    [JsonPropertyName("lastCheckedAt")]
    public string? LastCheckedAt { get; set; }

    [DynamoDBProperty("lastResponseTimeMs")]
    [JsonPropertyName("lastResponseTimeMs")]
    public long? LastResponseTimeMs { get; set; }

    [DynamoDBProperty("lastStatusCode")]
    [JsonPropertyName("lastStatusCode")]
    public int? LastStatusCode { get; set; }

    [DynamoDBProperty("createdAt")]
    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");

    [DynamoDBProperty("updatedAt")]
    [JsonPropertyName("updatedAt")]
    public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("o");
}

