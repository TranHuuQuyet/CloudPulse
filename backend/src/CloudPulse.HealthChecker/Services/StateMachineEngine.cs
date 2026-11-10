namespace CloudPulse.HealthChecker.Services;

using CloudPulse.Core.Entity;
using CloudPulse.HealthChecker.Repositories;

public class StateMachineEngine : IStateMachineEngine
{
    public StateTransitionResult Evaluate(MonitorItem monitor, CheckResultItem checkResult, int failureThreshold = 2)
    {
        if (monitor == null) throw new ArgumentNullException(nameof(monitor));
        if (checkResult == null) throw new ArgumentNullException(nameof(checkResult));

        string oldStatus = string.IsNullOrWhiteSpace(monitor.Status) ? MonitorStatus.Unknown : monitor.Status;
        int failures = monitor.ConsecutiveFailures;

        if (checkResult.IsSuccess)
        {
            failures = 0;
            string newStatus = MonitorStatus.Up;
            bool wasDown = string.Equals(oldStatus, MonitorStatus.Down, StringComparison.OrdinalIgnoreCase);

            return new StateTransitionResult(
                PreviousStatus: oldStatus,
                NewStatus: newStatus,
                ConsecutiveFailures: failures,
                ShouldOpenIncident: false,
                ShouldResolveIncident: wasDown,
                ShouldAlertDown: false,
                ShouldAlertRecovery: wasDown,
                Reason: null,
                DowntimeSeconds: null
            );
        }
        else
        {
            failures++;
            bool crossedThreshold = failures >= failureThreshold;
            bool wasAlreadyDown = string.Equals(oldStatus, MonitorStatus.Down, StringComparison.OrdinalIgnoreCase);

            string newStatus = crossedThreshold ? MonitorStatus.Down : oldStatus;
            bool shouldOpenIncident = crossedThreshold && !wasAlreadyDown;
            bool shouldAlertDown = crossedThreshold && !wasAlreadyDown;

            return new StateTransitionResult(
                PreviousStatus: oldStatus,
                NewStatus: newStatus,
                ConsecutiveFailures: failures,
                ShouldOpenIncident: shouldOpenIncident,
                ShouldResolveIncident: false,
                ShouldAlertDown: shouldAlertDown,
                ShouldAlertRecovery: false,
                Reason: checkResult.ErrorMessage ?? $"HTTP check failed with status {checkResult.HttpStatus}",
                DowntimeSeconds: null
            );
        }
    }

    public async Task<StateTransitionResult> ProcessTransitionAsync(
        MonitorItem monitor,
        CheckResultItem checkResult,
        IIncidentRepository incidentRepository,
        int failureThreshold = 2,
        CancellationToken cancellationToken = default)
    {
        if (monitor == null) throw new ArgumentNullException(nameof(monitor));
        if (checkResult == null) throw new ArgumentNullException(nameof(checkResult));
        if (incidentRepository == null) throw new ArgumentNullException(nameof(incidentRepository));

        var result = Evaluate(monitor, checkResult, failureThreshold);
        long? downtimeSeconds = null;

        if (result.ShouldOpenIncident)
        {
            var incident = new IncidentItem
            {
                MonitorId = monitor.MonitorId,
                IncidentId = $"INC-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                Status = IncidentStatus.Open,
                OpenedAt = DateTime.UtcNow.ToString("o"),
                Reason = result.Reason ?? "Health check failed (threshold reached)",
                DowntimeSeconds = null
            };
            await incidentRepository.SaveIncidentAsync(incident, cancellationToken);
        }
        else if (result.ShouldResolveIncident)
        {
            var openIncident = await incidentRepository.GetOpenIncidentAsync(monitor.MonitorId, cancellationToken);
            if (openIncident != null)
            {
                openIncident.Status = IncidentStatus.Resolved;
                openIncident.ResolvedAt = DateTime.UtcNow.ToString("o");

                if (DateTime.TryParse(openIncident.OpenedAt, out var openedAt) &&
                    DateTime.TryParse(openIncident.ResolvedAt, out var resolvedAt))
                {
                    downtimeSeconds = Math.Max(0, (long)(resolvedAt - openedAt).TotalSeconds);
                }
                else
                {
                    downtimeSeconds = 0;
                }

                openIncident.DowntimeSeconds = downtimeSeconds;
                await incidentRepository.SaveIncidentAsync(openIncident, cancellationToken);
            }
        }

        // Apply state updates to the monitor object
        monitor.Status = result.NewStatus;
        monitor.ConsecutiveFailures = result.ConsecutiveFailures;
        monitor.LastCheckedAt = checkResult.CheckedAt;
        monitor.LastResponseTimeMs = checkResult.ResponseTimeMs;
        monitor.LastStatusCode = checkResult.HttpStatus;
        monitor.UpdatedAt = DateTime.UtcNow.ToString("o");

        return result with { DowntimeSeconds = downtimeSeconds };
    }
}
