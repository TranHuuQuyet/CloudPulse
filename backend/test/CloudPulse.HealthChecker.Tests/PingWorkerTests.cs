namespace CloudPulse.HealthChecker.Tests;

using System.Net;
using CloudPulse.Core.Entity;
using CloudPulse.HealthChecker.Services;
using Xunit;

public class PingWorkerTests
{
    private const string ValidPublicUrl = "http://93.184.216.34/health"; // Public IP bypassing offline DNS resolution

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request, cancellationToken);
        }
    }

    [Fact]
    public async Task PingAsync_ReturnsSuccess_WhenEndpointReturns200()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler((req, token) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"status\":\"ok\"}")
            };
            return Task.FromResult(response);
        });

        var httpClient = new HttpClient(mockHandler);
        var pingService = new HttpPingService(httpClient, TimeSpan.FromSeconds(5));

        var monitor = new MonitorItem
        {
            MonitorId = "mon-1",
            Name = "Production Gateway",
            Url = ValidPublicUrl
        };

        // Act
        var result = await pingService.PingAsync(monitor);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.HttpStatus);
        Assert.Equal(CheckErrorType.None, result.ErrorType);
        Assert.Null(result.ErrorMessage);
        Assert.True(result.ResponseTimeMs >= 0);
    }

    [Fact]
    public async Task PingAsync_ReturnsHttpError_WhenEndpointReturns500()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler((req, token) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            return Task.FromResult(response);
        });

        var httpClient = new HttpClient(mockHandler);
        var pingService = new HttpPingService(httpClient, TimeSpan.FromSeconds(5));

        var monitor = new MonitorItem
        {
            MonitorId = "mon-2",
            Name = "Broken Service",
            Url = ValidPublicUrl
        };

        // Act
        var result = await pingService.PingAsync(monitor);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(500, result.HttpStatus);
        Assert.Equal(CheckErrorType.HttpError, result.ErrorType);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task PingAsync_ReturnsTimeout_WhenResponseExceedsTimeoutThreshold()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler(async (req, token) =>
        {
            // Delay longer than the 50ms test timeout
            await Task.Delay(500, token);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var httpClient = new HttpClient(mockHandler);
        // Use a short timeout of 50ms for fast testing of the timeout logic
        var pingService = new HttpPingService(httpClient, TimeSpan.FromMilliseconds(50));

        var monitor = new MonitorItem
        {
            MonitorId = "mon-3",
            Name = "Slow Endpoint",
            Url = ValidPublicUrl
        };

        // Act
        var result = await pingService.PingAsync(monitor);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(0, result.HttpStatus);
        Assert.Equal(CheckErrorType.Timeout, result.ErrorType);
        Assert.Contains("timed out", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PingAsync_BlocksSsrf_WhenUrlPointsToLoopbackOrPrivateAddress()
    {
        // Arrange
        var pingService = new HttpPingService();

        var monitor = new MonitorItem
        {
            MonitorId = "mon-ssrf",
            Name = "Malicious SSRF Target",
            Url = "http://127.0.0.1:8080/admin"
        };

        // Act
        var result = await pingService.PingAsync(monitor);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(CheckErrorType.SsrfBlocked, result.ErrorType);
        Assert.Contains("Private or loopback IP addresses are not permitted", result.ErrorMessage);
    }

    [Fact]
    public async Task PingAsync_ReturnsDnsError_WhenDnsResolutionFails()
    {
        // Arrange
        var pingService = new HttpPingService();

        var monitor = new MonitorItem
        {
            MonitorId = "mon-dns",
            Name = "Unresolvable Host",
            Url = "https://this-domain-definitely-does-not-exist-123456789.cloudpulse.invalid"
        };

        // Act
        var result = await pingService.PingAsync(monitor);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(CheckErrorType.DnsError, result.ErrorType);
    }

    [Fact]
    public void HttpPingService_DoesNotMutateInjectedHttpClientTimeout()
    {
        // Arrange
        var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };

        // Act
        _ = new HttpPingService(httpClient, TimeSpan.FromSeconds(5));

        // Assert: Injected client timeout must not be overridden
        Assert.Equal(TimeSpan.FromSeconds(60), httpClient.Timeout);
    }

    [Fact]
    public async Task PingAllAsync_ExecutesConcurrently_UsingTaskWhenAll()
    {
        // Arrange
        int concurrentCount = 0;
        int maxConcurrentObserved = 0;
        var lockObj = new object();

        var mockHandler = new MockHttpMessageHandler(async (req, token) =>
        {
            lock (lockObj)
            {
                concurrentCount++;
                if (concurrentCount > maxConcurrentObserved)
                {
                    maxConcurrentObserved = concurrentCount;
                }
            }

            await Task.Delay(50, token);

            lock (lockObj)
            {
                concurrentCount--;
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var httpClient = new HttpClient(mockHandler);
        var pingService = new HttpPingService(httpClient, TimeSpan.FromSeconds(5));

        var monitors = Enumerable.Range(1, 5).Select(i => new MonitorItem
        {
            MonitorId = $"mon-{i}",
            Name = $"Monitor {i}",
            Url = $"http://93.184.216.{i}/api"
        }).ToList();

        // Act
        var results = await pingService.PingAllAsync(monitors);

        // Assert
        Assert.Equal(5, results.Count);
        Assert.All(results, r => Assert.True(r.IsSuccess));
        Assert.True(maxConcurrentObserved > 1, $"Expected concurrent execution but observed max concurrency was {maxConcurrentObserved}");
    }

    [Theory]
    [InlineData("3000", 3000)]
    [InlineData("10000", 10000)]
    public void HttpPingService_WhenTimeoutMsEnvConfigured_UsesConfiguredTimeout(string envValue, int expectedMs)
    {
        var originalEnv = Environment.GetEnvironmentVariable("TIMEOUT_MS");
        try
        {
            Environment.SetEnvironmentVariable("TIMEOUT_MS", envValue);
            var pingService = new HttpPingService();
            Assert.Equal(TimeSpan.FromMilliseconds(expectedMs), pingService.Timeout);
        }
        finally
        {
            Environment.SetEnvironmentVariable("TIMEOUT_MS", originalEnv);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-number")]
    [InlineData("-1000")]
    [InlineData("0")]
    public void HttpPingService_WhenTimeoutMsEnvInvalidOrMissing_FallsBackTo5000Ms(string? envValue)
    {
        var originalEnv = Environment.GetEnvironmentVariable("TIMEOUT_MS");
        try
        {
            Environment.SetEnvironmentVariable("TIMEOUT_MS", envValue);
            var pingService = new HttpPingService();
            Assert.Equal(TimeSpan.FromMilliseconds(5000), pingService.Timeout);
        }
        finally
        {
            Environment.SetEnvironmentVariable("TIMEOUT_MS", originalEnv);
        }
    }

    [Fact]
    public void HttpPingService_ExplicitTimeoutConstructor_TakesPrecedenceOverEnv()
    {
        var originalEnv = Environment.GetEnvironmentVariable("TIMEOUT_MS");
        try
        {
            Environment.SetEnvironmentVariable("TIMEOUT_MS", "8000");
            var explicitTimeout = TimeSpan.FromSeconds(2);
            var pingService = new HttpPingService(timeout: explicitTimeout);
            Assert.Equal(explicitTimeout, pingService.Timeout);
        }
        finally
        {
            Environment.SetEnvironmentVariable("TIMEOUT_MS", originalEnv);
        }
    }
}
