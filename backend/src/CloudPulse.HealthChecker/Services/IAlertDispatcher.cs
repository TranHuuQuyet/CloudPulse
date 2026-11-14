namespace CloudPulse.HealthChecker.Services;

using CloudPulse.Core.Entity;

public interface IAlertDispatcher
{
    Task DispatchAsync(MonitorItem monitor, StateTransitionResult transition, CancellationToken cancellationToken = default);
}
