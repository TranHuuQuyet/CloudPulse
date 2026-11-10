namespace CloudPulse.HealthChecker.Repositories;

using CloudPulse.Core.Entity;

public interface ICheckResultRepository
{
    Task SaveCheckResultAsync(CheckResultItem result, CancellationToken cancellationToken = default);
    Task SaveCheckResultsBatchAsync(IEnumerable<CheckResultItem> results, CancellationToken cancellationToken = default);
}
