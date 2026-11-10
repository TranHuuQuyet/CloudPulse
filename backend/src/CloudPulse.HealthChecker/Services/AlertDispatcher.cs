namespace CloudPulse.HealthChecker.Services;

using CloudPulse.Core.Entity;

public class AlertDispatcher : IAlertDispatcher
{
    private readonly ISnsAlertChannel _snsAlertChannel;
    private readonly ITelegramAlertChannel _telegramAlertChannel;

    public AlertDispatcher(ISnsAlertChannel snsAlertChannel, ITelegramAlertChannel telegramAlertChannel)
    {
        _snsAlertChannel = snsAlertChannel ?? throw new ArgumentNullException(nameof(snsAlertChannel));
        _telegramAlertChannel = telegramAlertChannel ?? throw new ArgumentNullException(nameof(telegramAlertChannel));
    }

    public async Task DispatchAsync(MonitorItem monitor, StateTransitionResult transition, CancellationToken cancellationToken = default)
    {
        if (monitor == null) throw new ArgumentNullException(nameof(monitor));
        if (transition == null) throw new ArgumentNullException(nameof(transition));

        if (transition.ShouldAlertDown)
        {
            string alertType = "DOWN";
            string message = $"Website is DOWN after {transition.ConsecutiveFailures} consecutive failures.\n" +
                             $"URL: {monitor.Url}\n" +
                             $"Reason: {transition.Reason ?? "Health check failed."}";

            var snsTask = _snsAlertChannel.SendAlertAsync(monitor, alertType, message, cancellationToken);
            var telegramTask = _telegramAlertChannel.SendAlertAsync(monitor, alertType, message, cancellationToken);
            await Task.WhenAll(snsTask, telegramTask);
        }
        else if (transition.ShouldAlertRecovery)
        {
            string alertType = "RECOVERED";
            long downtime = transition.DowntimeSeconds ?? 0;
            string message = $"Website has recovered and is now UP.\n" +
                             $"URL: {monitor.Url}\n" +
                             $"Total Outage Downtime: {downtime} seconds.";

            var snsTask = _snsAlertChannel.SendAlertAsync(monitor, alertType, message, cancellationToken);
            var telegramTask = _telegramAlertChannel.SendAlertAsync(monitor, alertType, message, cancellationToken);
            await Task.WhenAll(snsTask, telegramTask);
        }
    }
}
