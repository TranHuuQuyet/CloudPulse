namespace CloudPulse.HealthChecker.Services;

using System.Text;
using System.Text.Json;
using CloudPulse.Core.Entity;

public class TelegramAlertChannel : ITelegramAlertChannel
{
    private readonly HttpClient _httpClient;
    private readonly TimeSpan _timeout;
    private readonly string? _botToken;
    private readonly string? _chatId;

    public TelegramAlertChannel(
        HttpClient? httpClient = null,
        string? botToken = null,
        string? chatId = null,
        TimeSpan? timeout = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _timeout = timeout ?? TimeSpan.FromSeconds(5);
        _botToken = botToken ?? Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
        _chatId = chatId ?? Environment.GetEnvironmentVariable("TELEGRAM_CHAT_ID");
    }

    public async Task SendAlertAsync(MonitorItem monitor, string alertType, string message, CancellationToken cancellationToken = default)
    {
        if (monitor == null) throw new ArgumentNullException(nameof(monitor));

        if (string.IsNullOrWhiteSpace(_botToken) || string.IsNullOrWhiteSpace(_chatId))
        {
            Console.WriteLine("[INFO] TelegramAlertChannel: TELEGRAM_BOT_TOKEN or TELEGRAM_CHAT_ID is not configured. Skipping Telegram alert.");
            return;
        }

        string emoji = alertType.Equals("DOWN", StringComparison.OrdinalIgnoreCase) ? "🚨" : "✅";
        string formattedText = $"{emoji} <b>[CloudPulse {alertType}]</b>\n\n" +
                               $"<b>Monitor:</b> {System.Net.WebUtility.HtmlEncode(monitor.Name)}\n" +
                               $"<b>URL:</b> {System.Net.WebUtility.HtmlEncode(monitor.Url)}\n" +
                               $"<b>Status:</b> <code>{alertType}</code>\n" +
                               $"<b>Details:</b> {System.Net.WebUtility.HtmlEncode(message)}\n" +
                               $"<b>Time:</b> <code>{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</code>";

        var payload = new
        {
            chat_id = _chatId,
            text = formattedText,
            parse_mode = "HTML",
            disable_web_page_preview = true
        };

        try
        {
            var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";
            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var timeoutCts = new CancellationTokenSource(_timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            using var response = await _httpClient.PostAsync(url, content, linkedCts.Token);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(linkedCts.Token);
                Console.WriteLine($"[WARN] TelegramAlertChannel: Telegram API returned {(int)response.StatusCode} {response.ReasonPhrase}: {errorBody}");
            }
            else
            {
                Console.WriteLine($"[INFO] TelegramAlertChannel: Successfully sent Telegram {alertType} alert for {monitor.Name}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] TelegramAlertChannel: Exception while sending Telegram alert for {monitor.Name}: {ex.Message}");
        }
    }
}
