namespace CloudPulse.HealthChecker.Services;

using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using CloudPulse.Core.Entity;

public class SnsAlertChannel : ISnsAlertChannel
{
    private readonly IAmazonSimpleNotificationService _snsClient;
    private readonly string? _topicArn;

    public SnsAlertChannel(IAmazonSimpleNotificationService snsClient, string? topicArn = null)
    {
        _snsClient = snsClient ?? throw new ArgumentNullException(nameof(snsClient));
        _topicArn = topicArn ?? Environment.GetEnvironmentVariable("SNS_TOPIC_ARN");
    }

    public async Task SendAlertAsync(MonitorItem monitor, string alertType, string message, CancellationToken cancellationToken = default)
    {
        if (monitor == null) throw new ArgumentNullException(nameof(monitor));
        if (string.IsNullOrWhiteSpace(_topicArn))
        {
            Console.WriteLine("[WARN] SnsAlertChannel: SNS_TOPIC_ARN is not configured. Skipping SNS alert.");
            return;
        }

        string subject = $"[CloudPulse {alertType}] {monitor.Name}";
        if (subject.Length > 100)
        {
            subject = subject.Substring(0, 97) + "...";
        }

        try
        {
            var request = new PublishRequest
            {
                TopicArn = _topicArn,
                Subject = subject,
                Message = message
            };

            await _snsClient.PublishAsync(request, cancellationToken);
            Console.WriteLine($"[INFO] SnsAlertChannel: Successfully published {alertType} alert for {monitor.Name}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] SnsAlertChannel: Failed to publish SNS alert for {monitor.Name}: {ex.Message}");
        }
    }
}
