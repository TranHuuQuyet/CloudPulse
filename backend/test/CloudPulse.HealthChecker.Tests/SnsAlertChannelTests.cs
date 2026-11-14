namespace CloudPulse.HealthChecker.Tests;

using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using CloudPulse.Core.Entity;
using CloudPulse.HealthChecker.Services;
using Moq;
using Xunit;

public class SnsAlertChannelTests
{
    private readonly Mock<IAmazonSimpleNotificationService> _mockSns = new();

    [Fact]
    public async Task SendAlertAsync_WhenTopicArnNotConfigured_DoesNotPublish()
    {
        // Arrange
        var channel = new SnsAlertChannel(_mockSns.Object, topicArn: null);
        var monitor = new MonitorItem { MonitorId = "m1", Name = "Test Monitor", Url = "https://example.com" };

        // Act
        await channel.SendAlertAsync(monitor, "DOWN", "Connection refused");

        // Assert
        _mockSns.Verify(s => s.PublishAsync(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendAlertAsync_WhenTopicArnConfigured_PublishesWithCorrectPayload()
    {
        // Arrange
        string topicArn = "arn:aws:sns:us-east-1:123456789012:CloudPulse_Alerts";
        var channel = new SnsAlertChannel(_mockSns.Object, topicArn: topicArn);
        var monitor = new MonitorItem { MonitorId = "m1", Name = "Test Monitor", Url = "https://example.com" };

        PublishRequest? capturedRequest = null;
        _mockSns.Setup(s => s.PublishAsync(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PublishRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new PublishResponse());

        // Act
        await channel.SendAlertAsync(monitor, "DOWN", "Endpoint returned HTTP 500");

        // Assert
        _mockSns.Verify(s => s.PublishAsync(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(capturedRequest);
        Assert.Equal(topicArn, capturedRequest!.TopicArn);
        Assert.Equal("[CloudPulse DOWN] Test Monitor", capturedRequest.Subject);
        Assert.Equal("Endpoint returned HTTP 500", capturedRequest.Message);
    }

    [Fact]
    public async Task SendAlertAsync_WhenSubjectExceeds100Chars_TruncatesSubjectTo100Chars()
    {
        // Arrange
        string topicArn = "arn:aws:sns:us-east-1:123456789012:CloudPulse_Alerts";
        var channel = new SnsAlertChannel(_mockSns.Object, topicArn: topicArn);
        var veryLongName = new string('A', 120);
        var monitor = new MonitorItem { MonitorId = "m1", Name = veryLongName, Url = "https://example.com" };

        PublishRequest? capturedRequest = null;
        _mockSns.Setup(s => s.PublishAsync(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PublishRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new PublishResponse());

        // Act
        await channel.SendAlertAsync(monitor, "DOWN", "Endpoint down");

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.True(capturedRequest!.Subject.Length <= 100);
        Assert.EndsWith("...", capturedRequest.Subject);
    }

    [Fact]
    public async Task SendAlertAsync_WhenPublishThrowsException_CatchesAndDoesNotThrow()
    {
        // Arrange
        string topicArn = "arn:aws:sns:us-east-1:123456789012:CloudPulse_Alerts";
        var channel = new SnsAlertChannel(_mockSns.Object, topicArn: topicArn);
        var monitor = new MonitorItem { MonitorId = "m1", Name = "Test Monitor", Url = "https://example.com" };

        _mockSns.Setup(s => s.PublishAsync(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonSimpleNotificationServiceException("SNS Service Unavailable"));

        // Act & Assert (Must not throw)
        var exception = await Record.ExceptionAsync(() => channel.SendAlertAsync(monitor, "DOWN", "error"));
        Assert.Null(exception);
    }

    [Fact]
    public async Task SendAlertAsync_WhenMonitorNameContainsNewlines_SanitizesSubjectWithoutNewlines()
    {
        // Arrange
        string topicArn = "arn:aws:sns:us-east-1:123456789012:CloudPulse_Alerts";
        var channel = new SnsAlertChannel(_mockSns.Object, topicArn: topicArn);
        var monitor = new MonitorItem
        {
            MonitorId = "m1",
            Name = "Monitor\r\nWith\nLine\rBreaks",
            Url = "https://example.com"
        };

        PublishRequest? capturedRequest = null;
        _mockSns.Setup(s => s.PublishAsync(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PublishRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new PublishResponse());

        // Act
        await channel.SendAlertAsync(monitor, "DOWN", "Error message");

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.DoesNotContain("\r", capturedRequest!.Subject);
        Assert.DoesNotContain("\n", capturedRequest.Subject);
        Assert.Equal("[CloudPulse DOWN] Monitor With Line Breaks", capturedRequest.Subject);
    }
}
