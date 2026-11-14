namespace CloudPulse.HealthChecker.Repositories;

using CloudPulse.Core.Entity;

public interface IIncidentRepository
{
    Task<IncidentItem?> GetOpenIncidentAsync(string monitorId, CancellationToken cancellationToken = default);
    Task SaveIncidentAsync(IncidentItem incident, CancellationToken cancellationToken = default);
}
