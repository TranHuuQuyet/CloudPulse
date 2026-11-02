using System.Text.Json;
using CloudPulse.Core.Entity;
using Xunit;

namespace CloudPulse.Core.Tests;

public class EntityTests
{
    [Fact]
    public void MonitorItem_DefaultValues_AreCorrect()
    {
        var item = new MonitorItem
        {
            Name = "Production API",
            Url = "https://example.com/health"
        };

        Assert.False(string.IsNullOrWhiteSpace(item.MonitorId));
        Assert.Equal("Production API", item.Name);
        Assert.Equal("https://example.com/health", item.Url);
        Assert.Equal(MonitorStatus.Unknown, item.Status);
        Assert.Equal(60, item.IntervalSeconds);
        Assert.Equal(0, item.ConsecutiveFailures);
        Assert.Null(item.LastCheckedAt);
        Assert.Null(item.LastResponseTimeMs);
        Assert.Null(item.LastStatusCode);
    }

    [Fact]
    public void MonitorItem_JsonSerialization_MatchesProperties()
    {
        var item = new MonitorItem
        {
            MonitorId = "mon-123",
            Name = "Test Monitor",
            Url = "https://test.com",
            Status = MonitorStatus.Up,
            ConsecutiveFailures = 0,
            LastStatusCode = 200,
            LastResponseTimeMs = 150
        };

        var json = JsonSerializer.Serialize(item);
        Assert.Contains("\"monitorId\":\"mon-123\"", json);
        Assert.Contains("\"name\":\"Test Monitor\"", json);
        Assert.Contains("\"status\":\"UP\"", json);
        Assert.Contains("\"lastStatusCode\":200", json);
        Assert.Contains("\"lastResponseTimeMs\":150", json);

        var deserialized = JsonSerializer.Deserialize<MonitorItem>(json);
        Assert.NotNull(deserialized);
        Assert.Equal("mon-123", deserialized.MonitorId);
        Assert.Equal(MonitorStatus.Up, deserialized.Status);
    }

    [Fact]
    public void CheckResultItem_PropertiesAndTTL_AreValid()
    {
        var result = new CheckResultItem
        {
            MonitorId = "mon-123",
            HttpStatus = 200,
            ResponseTimeMs = 120,
            IsSuccess = true,
            ErrorType = CheckErrorType.None
        };

        Assert.Equal("mon-123", result.MonitorId);
        Assert.Equal(200, result.HttpStatus);
        Assert.Equal(120, result.ResponseTimeMs);
        Assert.True(result.IsSuccess);
        Assert.Equal(CheckErrorType.None, result.ErrorType);
        Assert.True(result.ExpireAt > DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    [Fact]
    public void IncidentItem_DefaultValues_AreValid()
    {
        var incident = new IncidentItem
        {
            MonitorId = "mon-123",
            Reason = "HTTP 500 error after 2 retries"
        };

        Assert.Equal("mon-123", incident.MonitorId);
        Assert.StartsWith("INC-", incident.IncidentId);
        Assert.Equal(IncidentStatus.Open, incident.Status);
        Assert.Null(incident.ResolvedAt);
        Assert.Null(incident.DowntimeSeconds);
    }
}

