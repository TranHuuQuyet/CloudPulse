using System.Text.Json.Serialization;
using Amazon.DynamoDBv2.DataModel;

namespace CloudPulse.Core.Entity;

[DynamoDBTable("CloudPulse_Incidents")]
public class IncidentItem
{
    [DynamoDBHashKey("monitorId")]
    [JsonPropertyName("monitorId")]
    public string MonitorId { get; set; } = string.Empty;

    [DynamoDBRangeKey("incidentId")]
    [JsonPropertyName("incidentId")]
    public string IncidentId { get; set; } = $"INC-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

    [DynamoDBProperty("status")]
    [JsonPropertyName("status")]
    public string Status { get; set; } = IncidentStatus.Open;

    [DynamoDBProperty("openedAt")]
    [JsonPropertyName("openedAt")]
    public string OpenedAt { get; set; } = DateTime.UtcNow.ToString("o");

    [DynamoDBProperty("resolvedAt")]
    [JsonPropertyName("resolvedAt")]
    public string? ResolvedAt { get; set; }

    [DynamoDBProperty("reason")]
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [DynamoDBProperty("downtimeSeconds")]
    [JsonPropertyName("downtimeSeconds")]
    public long? DowntimeSeconds { get; set; }
}

