namespace CloudPulse.HealthChecker.Repositories;

using CloudPulse.Core.Entity;

public interface IMonitorRepository
{
    Task<List<MonitorItem>> GetAllMonitorsAsync(CancellationToken cancellationToken = default);
    Task<MonitorItem?> GetMonitorAsync(string monitorId, CancellationToken cancellationToken = default);
    Task SaveMonitorAsync(MonitorItem monitor, CancellationToken cancellationToken = default);
}
