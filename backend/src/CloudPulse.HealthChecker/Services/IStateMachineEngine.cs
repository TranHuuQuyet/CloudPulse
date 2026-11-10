namespace CloudPulse.HealthChecker.Services;

using CloudPulse.Core.Entity;
using CloudPulse.HealthChecker.Repositories;

public record StateTransitionResult(
    string PreviousStatus,
    string NewStatus,
    int ConsecutiveFailures,
    bool ShouldOpenIncident,
    bool ShouldResolveIncident,
    bool ShouldAlertDown,
    bool ShouldAlertRecovery,
    string? Reason,
    long? DowntimeSeconds
);

public interface IStateMachineEngine
{
    StateTransitionResult Evaluate(MonitorItem monitor, CheckResultItem checkResult, int failureThreshold = 2);

    Task<StateTransitionResult> ProcessTransitionAsync(
        MonitorItem monitor,
        CheckResultItem checkResult,
        IIncidentRepository incidentRepository,
        int failureThreshold = 2,
        CancellationToken cancellationToken = default);
}
