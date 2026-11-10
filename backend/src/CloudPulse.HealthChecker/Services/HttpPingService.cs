namespace CloudPulse.HealthChecker.Services;

using System.Diagnostics;
using System.Net.Sockets;
using CloudPulse.Core.Entity;
using CloudPulse.Core.Validation;

public class HttpPingService : IHttpPingService
{
    private readonly HttpClient _httpClient;
    private readonly TimeSpan _timeout;
    private readonly Func<string, CancellationToken, Task<UrlValidationResult>> _urlValidator;

    public HttpPingService(
        HttpClient? httpClient = null,
        TimeSpan? timeout = null,
        Func<string, CancellationToken, Task<UrlValidationResult>>? urlValidator = null)
    {
        _httpClient = httpClient ?? CreateDefaultHttpClient();
        _timeout = timeout ?? TimeSpan.FromSeconds(5);
        _urlValidator = urlValidator ?? UrlValidator.ValidateAsync;
    }

    private static HttpClient CreateDefaultHttpClient()
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false
        };
        return new HttpClient(handler);
    }

    public async Task<CheckResultItem> PingAsync(MonitorItem monitor, CancellationToken cancellationToken = default)
    {
        if (monitor == null) throw new ArgumentNullException(nameof(monitor));

        var checkedAt = DateTime.UtcNow.ToString("o");
        var expireAt = DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeSeconds();

        // 1. Validate URL to prevent SSRF and resolve DNS safely
        var validationResult = await _urlValidator(monitor.Url, cancellationToken);

        if (!validationResult.IsValid)
        {
            string errorType = validationResult.IsDnsError ||
                (validationResult.ErrorMessage != null &&
                 (validationResult.ErrorMessage.Contains("DNS", StringComparison.OrdinalIgnoreCase) ||
                  validationResult.ErrorMessage.Contains("resolve host", StringComparison.OrdinalIgnoreCase)))
                ? CheckErrorType.DnsError
                : CheckErrorType.SsrfBlocked;

            return new CheckResultItem
            {
                MonitorId = monitor.MonitorId,
                CheckedAt = checkedAt,
                HttpStatus = 0,
                ResponseTimeMs = 0,
                IsSuccess = false,
                ErrorType = errorType,
                ErrorMessage = validationResult.ErrorMessage ?? (errorType == CheckErrorType.DnsError ? "DNS resolution failed." : "URL failed SSRF validation."),
                ExpireAt = expireAt
            };
        }

        var stopwatch = Stopwatch.StartNew();
        using var timeoutCts = new CancellationTokenSource(_timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, validationResult.ValidatedUri ?? new Uri(monitor.Url));
            request.Headers.Add("User-Agent", "CloudPulse-HealthChecker/1.0");

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token);
            stopwatch.Stop();

            int statusCode = (int)response.StatusCode;
            bool isSuccess = statusCode >= 200 && statusCode < 400;

            return new CheckResultItem
            {
                MonitorId = monitor.MonitorId,
                CheckedAt = checkedAt,
                HttpStatus = statusCode,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                IsSuccess = isSuccess,
                ErrorType = isSuccess ? CheckErrorType.None : CheckErrorType.HttpError,
                ErrorMessage = isSuccess ? null : $"HTTP {statusCode} {response.ReasonPhrase}",
                ExpireAt = expireAt
            };
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            return new CheckResultItem
            {
                MonitorId = monitor.MonitorId,
                CheckedAt = checkedAt,
                HttpStatus = 0,
                ResponseTimeMs = Math.Max(stopwatch.ElapsedMilliseconds, (long)_timeout.TotalMilliseconds),
                IsSuccess = false,
                ErrorType = CheckErrorType.Timeout,
                ErrorMessage = $"Request timed out after {_timeout.TotalSeconds} seconds.",
                ExpireAt = expireAt
            };
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            bool isDns = ex.InnerException is SocketException sock &&
                (sock.SocketErrorCode == SocketError.HostNotFound ||
                 sock.SocketErrorCode == SocketError.NoData ||
                 sock.SocketErrorCode == SocketError.TryAgain) ||
                ex.Message.Contains("Name or service not known", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("No such host is known", StringComparison.OrdinalIgnoreCase);

            return new CheckResultItem
            {
                MonitorId = monitor.MonitorId,
                CheckedAt = checkedAt,
                HttpStatus = 0,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                IsSuccess = false,
                ErrorType = isDns ? CheckErrorType.DnsError : CheckErrorType.ConnectionError,
                ErrorMessage = ex.Message,
                ExpireAt = expireAt
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new CheckResultItem
            {
                MonitorId = monitor.MonitorId,
                CheckedAt = checkedAt,
                HttpStatus = 0,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                IsSuccess = false,
                ErrorType = CheckErrorType.Unknown,
                ErrorMessage = ex.Message,
                ExpireAt = expireAt
            };
        }
    }

    public async Task<List<CheckResultItem>> PingAllAsync(IEnumerable<MonitorItem> monitors, CancellationToken cancellationToken = default)
    {
        if (monitors == null) throw new ArgumentNullException(nameof(monitors));

        var pingTasks = monitors.Select(monitor => PingAsync(monitor, cancellationToken));
        var results = await Task.WhenAll(pingTasks);
        return results.ToList();
    }
}
