namespace CloudPulse.HealthChecker.Services;

using CloudPulse.Core.Entity;

public interface IHttpPingService
{
    Task<CheckResultItem> PingAsync(MonitorItem monitor, CancellationToken cancellationToken = default);
    Task<List<CheckResultItem>> PingAllAsync(IEnumerable<MonitorItem> monitors, CancellationToken cancellationToken = default);
}
