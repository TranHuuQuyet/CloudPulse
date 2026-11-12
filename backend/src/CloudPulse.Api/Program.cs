using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using CloudPulse.Core.Entity;
using CloudPulse.Core.Validation;
using CloudPulse.Api.Requests;
using Amazon.Lambda.AspNetCoreServer.Hosting;
using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using Amazon.Runtime;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAWSLambdaHosting(
    LambdaEventSource.HttpApi);

builder.Services.AddHttpClient("MonitorCheck", client =>
{
    // Dành thời gian cho việc lưu kết quả vào DynamoDB.
    client.Timeout = TimeSpan.FromMilliseconds(1500);

    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "CloudPulse-Monitor/1.0");
})
.ConfigurePrimaryHttpMessageHandler(() =>
    new SocketsHttpHandler
    {
        // Không tự động đi theo redirect để tránh SSRF qua redirect.
        AllowAutoRedirect = false,

        ConnectTimeout = TimeSpan.FromMilliseconds(1000)
    });

builder.Services.AddSingleton<IAmazonDynamoDB>(_ =>
{
    if (builder.Environment.IsDevelopment())
    {
        var config = new AmazonDynamoDBConfig
        {
            ServiceURL = builder.Configuration["AWS:ServiceURL"]
                         ?? "http://localhost:8000",

            AuthenticationRegion = builder.Configuration["AWS:Region"]
                                   ?? "ap-southeast-1"
        };

        var credentials = new BasicAWSCredentials(
            builder.Configuration["AWS:AccessKey"] ?? "local",
            builder.Configuration["AWS:SecretKey"] ?? "local"
        );

        return new AmazonDynamoDBClient(credentials, config);
    }

    return new AmazonDynamoDBClient();
});

builder.Services.AddScoped<IDynamoDBContext>(sp =>
    new DynamoDBContext(
        sp.GetRequiredService<IAmazonDynamoDB>()));



builder.Services.AddHttpClient();

var app = builder.Build();

app.MapGet("/api/status", () =>
{
    return Results.Ok(new
    {
        service = "CloudPulse.Api",
        status = "running",
        message = "CloudPulse API is working!"
    });
});

app.MapGet("/api/monitors",
    async (IDynamoDBContext db, CancellationToken ct) =>
{
    var search = db.ScanAsync<MonitorItem>(
        new List<ScanCondition>());

    var monitors = await search.GetRemainingAsync(ct);

    return Results.Ok(monitors);
});

app.MapPost("/api/monitors",
    async (
        CreateMonitorRequest request,
        IDynamoDBContext db,
        CancellationToken ct) =>
{
    // 1. Kiểm tra tên Monitor
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new
        {
            message = "Monitor name is required."
        });
    }

    // 2. Kiểm tra URL
    if (string.IsNullOrWhiteSpace(request.Url))
    {
        return Results.BadRequest(new
        {
            message = "Monitor URL is required."
        });
    }

    // 3. Kiểm tra tính hợp lệ và an toàn của URL
    var validation = await UrlValidator.ValidateAsync(
        request.Url,
        ct);

    if (!validation.IsValid)
    {
        return Results.BadRequest(new
        {
            message = validation.ErrorMessage
        });
    }

    // 4. Tạo Monitor mới
    var now = DateTime.UtcNow.ToString("O");

    var monitor = new MonitorItem
    {
        Name = request.Name.Trim(),
        Url = validation.ValidatedUri!.ToString(),
        Status = MonitorStatus.Unknown,
        IntervalSeconds = 60,
        ConsecutiveFailures = 0,
        CreatedAt = now,
        UpdatedAt = now
    };

    // 5. Lưu vào DynamoDB
    await db.SaveAsync(monitor, ct);

    // 6. Trả về HTTP 201
    return Results.Created(
        $"/api/monitors/{monitor.MonitorId}",
        monitor);
});

app.MapGet("/api/monitors/{id}",
    async (
        string id,
        IDynamoDBContext db,
        CancellationToken ct) =>
{
    // Tìm Monitor theo Partition Key
    var monitor = await db.LoadAsync<MonitorItem>(
        id,
        ct);

    // Không tìm thấy Monitor
    if (monitor is null)
    {
        return Results.NotFound(new
        {
            message = "Monitor not found."
        });
    }

    // Trả về thông tin Monitor
    return Results.Ok(monitor);
});

app.MapDelete("/api/monitors/{id}",
    async (
        string id,
        IDynamoDBContext db,
        CancellationToken ct) =>
{
    // 1. Kiểm tra Monitor có tồn tại không
    var monitor = await db.LoadAsync<MonitorItem>(
        id,
        ct);

    if (monitor is null)
    {
        return Results.NotFound(new
        {
            message = "Monitor not found."
        });
    }

    // 2. Xóa Monitor khỏi DynamoDB
    await db.DeleteAsync(monitor, ct);

    // 3. Trả về HTTP 204
    return Results.NoContent();
});

app.MapGet("/api/monitors/{id}/checks",
    async (
        string id,
        IDynamoDBContext db,
        CancellationToken ct) =>
{
    // 1. Kiểm tra Monitor tồn tại
    var monitor = await db.LoadAsync<MonitorItem>(
        id,
        ct);

    if (monitor is null)
    {
        return Results.NotFound(new
        {
            message = "Monitor not found."
        });
    }

    // 2. Truy vấn lịch sử kiểm tra
    var search = db.QueryAsync<CheckResultItem>(
        id,
        new DynamoDBOperationConfig
        {
            BackwardQuery = true
        });

    // 3. Lấy kết quả
    var checks = new List<CheckResultItem>();

    while (!search.IsDone && checks.Count < 30)
    {
        var page = await search.GetNextSetAsync(ct);

        checks.AddRange(
            page.Take(30 - checks.Count));
    }
    // 4. Trả về kết quả
    var chartData = checks
        .Take(30)
        .Reverse()
        .Select(FormatCheckChartPoint)
        .ToList();

    return Results.Ok(chartData);
});

app.MapGet("/api/monitors/{id}/incidents",
    async (
        string id,
        IDynamoDBContext db,
        CancellationToken ct) =>
{
    // 1. Kiểm tra Monitor có tồn tại không
    var monitor = await db.LoadAsync<MonitorItem>(
        id,
        ct);

    if (monitor is null)
    {
        return Results.NotFound(new
        {
            message = "Monitor not found."
        });
    }

    // 2. Truy vấn lịch sử sự cố
    var search = db.QueryAsync<IncidentItem>(
        id,
        new DynamoDBOperationConfig
        {
            BackwardQuery = true
        });

    // 3. Lấy kết quả
    var incidents = await search.GetRemainingAsync(ct);

    // 4. Trả về kết quả
    return Results.Ok(incidents);
});


app.MapPost("/api/monitors/{id}/check",
    async (
        string id,
        IDynamoDBContext db,
        IHttpClientFactory httpClientFactory,
        CancellationToken ct) =>
{
    // 1. Tìm Monitor trong DynamoDB
    var monitor = await db.LoadAsync<MonitorItem>(
        id,
        ct);

    if (monitor is null)
    {
        return Results.NotFound(new
        {
            message = "Monitor not found."
        });
    }

    // 2. Khởi tạo thông tin lần kiểm tra
    var checkedAt = DateTime.UtcNow.ToString("O");

    int httpStatus = 0;
    bool isSuccess = false;

    string errorType = CheckErrorType.None;
    string? errorMessage = null;

    var stopwatch = new Stopwatch();

    // Giới hạn thời gian kiểm tra để đáp ứng AC-08
    using var timeoutCts =
        CancellationTokenSource.CreateLinkedTokenSource(ct);

    timeoutCts.CancelAfter(
        TimeSpan.FromMilliseconds(1500));

    var checkToken = timeoutCts.Token;

    // 3. Kiểm tra URL và chống SSRF
    var validation = await UrlValidator.ValidateAsync(
        monitor.Url,
        checkToken);

    ct.ThrowIfCancellationRequested();

    if (!validation.IsValid)
    {
        if (checkToken.IsCancellationRequested)
        {
            errorType = CheckErrorType.Timeout;
            errorMessage = "URL validation timed out.";
        }
        else if (string.Equals(
            validation.ErrorMessage,
            "Private or loopback IP addresses are not permitted.",
            StringComparison.Ordinal))
        {
            errorType = CheckErrorType.SsrfBlocked;
            errorMessage = validation.ErrorMessage;
        }
        else if (
            validation.ErrorMessage?.Contains(
                "DNS", StringComparison.OrdinalIgnoreCase) == true ||
            validation.ErrorMessage?.Contains(
                "resolve host", StringComparison.OrdinalIgnoreCase) == true)
        {
            errorType = CheckErrorType.DnsError;
            errorMessage = validation.ErrorMessage;
        }
        else
        {
            errorType = CheckErrorType.Unknown;
            errorMessage = validation.ErrorMessage;
        }
    }
    else
    {
        // 4. Gửi HTTP request đến website
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                validation.ValidatedUri);

            var httpClient = httpClientFactory.CreateClient("MonitorCheck");

            // Chỉ đo thời gian gửi HTTP request đến khi nhận response headers.
            stopwatch.Start();

            try
            {
                using var response = await httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    checkToken);

                httpStatus = (int)response.StatusCode;

                // Theo FR-03.2: HTTP 200-399 được xem là SUCCESS.
                isSuccess = httpStatus >= 200 && httpStatus <= 399;

                if (!isSuccess)
                {
                    errorType = CheckErrorType.HttpError;

                    errorMessage =
                        $"HTTP {httpStatus} ({response.ReasonPhrase})";
                }
            }
            finally
            {
                // Đảm bảo dừng Stopwatch cả khi request lỗi hoặc timeout.
                if (stopwatch.IsRunning)
                {
                    stopwatch.Stop();
                }
            }
        }
        catch (OperationCanceledException)
            when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            errorType = CheckErrorType.Timeout;

            errorMessage = "Request timed out.";
        }
        catch (HttpRequestException ex)
        {
            errorType = IsDnsResolutionError(ex)
                ? CheckErrorType.DnsError
                : CheckErrorType.ConnectionError;

            errorMessage = ex.Message;
        }
    }

    // 5. Tính thời gian phản hồi của HTTP request.
    if (stopwatch.IsRunning)
    {
        stopwatch.Stop();
    }

    long responseTimeMs = stopwatch.ElapsedMilliseconds;

    // 6. Tạo bản ghi CheckResult
    var checkResult = new CheckResultItem
    {
        MonitorId = id,
        CheckedAt = checkedAt,

        HttpStatus = httpStatus,
        ResponseTimeMs = responseTimeMs,

        IsSuccess = isSuccess,

        ErrorType = errorType,
        ErrorMessage = errorMessage,

        // TTL: tự động hết hạn sau 7 ngày
        ExpireAt = DateTimeOffset.UtcNow
            .AddDays(7)
            .ToUnixTimeSeconds()
    };

    // 7. Lưu lịch sử kiểm tra
    await db.SaveAsync(checkResult, ct);

    // 8. Cập nhật thông tin lần kiểm tra gần nhất
    // Không thay đổi Status và ConsecutiveFailures
    // để không ảnh hưởng State Machine của TV2.

    monitor.LastCheckedAt = checkedAt;
    monitor.LastResponseTimeMs = responseTimeMs;
    monitor.LastStatusCode = httpStatus == 0
        ? null
        : httpStatus;

    monitor.UpdatedAt = checkedAt;

    await db.SaveAsync(monitor, ct);

    // 9. Trả kết quả cho Dashboard
    return Results.Ok(new
    {
        monitorId = id,
        checkedAt,

        isSuccess,
        statusCode = httpStatus == 0
            ? (int?)null
            : httpStatus,

        responseTimeMs,

        errorType,
        errorMessage
    });
});


// Phân loại lỗi DNS từ HttpRequestException
static bool IsDnsResolutionError(Exception? exception)
{
    while (exception is not null)
    {
        if (exception is SocketException socketException &&
            socketException.SocketErrorCode is
                SocketError.HostNotFound or
                SocketError.NoData or
                SocketError.TryAgain or
                SocketError.NoRecovery)
        {
            return true;
        }

        exception = exception.InnerException;
    }

    return false;
}


// Chuẩn hóa dữ liệu điểm biểu đồ kèm timestamp ISO 8601 UTC để Frontend tự đổi múi giờ
static object FormatCheckChartPoint(CheckResultItem check)
{
    bool parsed = DateTimeOffset.TryParse(
        check.CheckedAt,
        CultureInfo.InvariantCulture,
        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
        out var utcTimestamp);

    return new
    {
        checkedAt = parsed
            ? utcTimestamp.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)
            : check.CheckedAt,
        time = parsed
            ? utcTimestamp.UtcDateTime.ToString("dd/MM HH:mm:ss", CultureInfo.InvariantCulture)
            : check.CheckedAt,
        responseTimeMs = check.ResponseTimeMs,
        statusCode = check.HttpStatus == 0
            ? (int?)null
            : check.HttpStatus
    };
}


app.Run();