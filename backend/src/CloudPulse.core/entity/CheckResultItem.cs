using System.Text.Json.Serialization;
using Amazon.DynamoDBv2.DataModel;

namespace CloudPulse.Core.Entity;

[DynamoDBTable("CloudPulse_CheckResults")]
public class CheckResultItem
{
    [DynamoDBHashKey("monitorId")]
    [JsonPropertyName("monitorId")]
    public string MonitorId { get; set; } = string.Empty;

    [DynamoDBRangeKey("checkedAt")]
    [JsonPropertyName("checkedAt")]
    public string CheckedAt { get; set; } = DateTime.UtcNow.ToString("o");

    [DynamoDBProperty("httpStatus")]
    [JsonPropertyName("httpStatus")]
    public int HttpStatus { get; set; }

    [DynamoDBProperty("responseTimeMs")]
    [JsonPropertyName("responseTimeMs")]
    public long ResponseTimeMs { get; set; }

    [DynamoDBProperty("isSuccess")]
    [JsonPropertyName("isSuccess")]
    public bool IsSuccess { get; set; }

    [DynamoDBProperty("errorType")]
    [JsonPropertyName("errorType")]
    public string ErrorType { get; set; } = CheckErrorType.None;

    [DynamoDBProperty("errorMessage")]
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// TTL timestamp (epoch seconds) for DynamoDB auto-expiration (e.g. 7 days).
    /// </summary>
    [DynamoDBProperty("expireAt")]
    [JsonPropertyName("expireAt")]
    public long ExpireAt { get; set; } = DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeSeconds();
}

