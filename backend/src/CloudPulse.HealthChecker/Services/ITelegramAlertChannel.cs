namespace CloudPulse.HealthChecker.Services;

using CloudPulse.Core.Entity;

public interface ITelegramAlertChannel
{
    Task SendAlertAsync(MonitorItem monitor, string alertType, string message, CancellationToken cancellationToken = default);
}
