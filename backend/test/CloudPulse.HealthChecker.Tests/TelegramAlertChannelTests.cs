namespace CloudPulse.HealthChecker.Tests;

using System.Net;
using System.Text.Json;
using CloudPulse.Core.Entity;
using CloudPulse.HealthChecker.Services;
using Xunit;

public class TelegramAlertChannelTests
{
    private class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public TestHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request, cancellationToken);
        }
    }

    [Fact]
    public async Task SendAlertAsync_WhenCredentialsMissing_DoesNotMakeHttpRequest()
    {
        // Arrange
        bool requestMade = false;
        var handler = new TestHttpMessageHandler((_, _) =>
        {
            requestMade = true;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        var httpClient = new HttpClient(handler);
        var channel = new TelegramAlertChannel(httpClient, botToken: null, chatId: null);
        var monitor = new MonitorItem { MonitorId = "m1", Name = "Test Monitor", Url = "https://example.com" };

        // Act
        await channel.SendAlertAsync(monitor, "DOWN", "Server Error");

        // Assert
        Assert.False(requestMade);
    }

    [Fact]
    public async Task SendAlertAsync_WhenCredentialsConfigured_SendsExpectedPayload()
    {
        // Arrange
        string botToken = "test-bot-token-123";
        string chatId = "-100123456789";
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;

        var handler = new TestHttpMessageHandler(async (req, token) =>
        {
            capturedRequest = req;
            capturedBody = await req.Content!.ReadAsStringAsync(token);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"ok\":true}")
            };
        });

        var httpClient = new HttpClient(handler);
        var channel = new TelegramAlertChannel(httpClient, botToken: botToken, chatId: chatId);
        var monitor = new MonitorItem
        {
            MonitorId = "m1",
            Name = "Production <API>",
            Url = "https://api.example.com"
        };

        // Act
        await channel.SendAlertAsync(monitor, "DOWN", "HTTP 500 Internal Error");

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Equal($"https://api.telegram.org/bot{botToken}/sendMessage", capturedRequest.RequestUri!.ToString());

        Assert.NotNull(capturedBody);
        using var jsonDoc = JsonDocument.Parse(capturedBody!);
        var root = jsonDoc.RootElement;
        Assert.Equal(chatId, root.GetProperty("chat_id").GetString());
        Assert.Equal("HTML", root.GetProperty("parse_mode").GetString());
        Assert.True(root.GetProperty("disable_web_page_preview").GetBoolean());

        string text = root.GetProperty("text").GetString()!;
        Assert.Contains("[CloudPulse DOWN]", text);
        Assert.Contains("Production &lt;API&gt;", text); // Verified HTML encoding
        Assert.Contains("https://api.example.com", text);
        Assert.Contains("HTTP 500 Internal Error", text);
    }

    [Fact]
    public async Task SendAlertAsync_WhenAlertIsRecovered_IncludesRecoveryEmoji()
    {
        // Arrange
        string? capturedBody = null;
        var handler = new TestHttpMessageHandler(async (req, token) =>
        {
            capturedBody = await req.Content!.ReadAsStringAsync(token);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var httpClient = new HttpClient(handler);
        var channel = new TelegramAlertChannel(httpClient, botToken: "tok", chatId: "cid");
        var monitor = new MonitorItem { MonitorId = "m1", Name = "Auth Service", Url = "https://auth.example.com" };

        // Act
        await channel.SendAlertAsync(monitor, "RECOVERED", "Endpoint is back UP. Downtime: 45s");

        // Assert
        Assert.NotNull(capturedBody);
        using var jsonDoc = JsonDocument.Parse(capturedBody!);
        string text = jsonDoc.RootElement.GetProperty("text").GetString()!;
        Assert.Contains("✅", text);
        Assert.Contains("[CloudPulse RECOVERED]", text);
    }

    [Fact]
    public async Task SendAlertAsync_WhenTelegramApiReturns400_LogsAndDoesNotThrow()
    {
        // Arrange
        var handler = new TestHttpMessageHandler((_, _) =>
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"ok\":false,\"description\":\"Bad Request: chat not found\"}")
            });
        });

        var httpClient = new HttpClient(handler);
        var channel = new TelegramAlertChannel(httpClient, botToken: "tok", chatId: "cid");
        var monitor = new MonitorItem { MonitorId = "m1", Name = "Auth Service", Url = "https://auth.example.com" };

        // Act & Assert (Must not throw)
        var exception = await Record.ExceptionAsync(() => channel.SendAlertAsync(monitor, "DOWN", "error"));
        Assert.Null(exception);
    }

    [Fact]
    public async Task SendAlertAsync_WhenHttpThrowsException_CatchesAndDoesNotThrow()
    {
        // Arrange
        var handler = new TestHttpMessageHandler((_, _) => throw new HttpRequestException("Network failure"));
        var httpClient = new HttpClient(handler);
        var channel = new TelegramAlertChannel(httpClient, botToken: "tok", chatId: "cid");
        var monitor = new MonitorItem { MonitorId = "m1", Name = "Auth Service", Url = "https://auth.example.com" };

        // Act & Assert (Must not throw)
        var exception = await Record.ExceptionAsync(() => channel.SendAlertAsync(monitor, "DOWN", "error"));
        Assert.Null(exception);
    }
}
