namespace CloudPulse.HealthChecker.Repositories;

using Amazon.DynamoDBv2.DataModel;
using CloudPulse.Core.Entity;

public class MonitorRepository : IMonitorRepository
{
    private readonly IDynamoDBContext _context;

    public MonitorRepository(IDynamoDBContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<List<MonitorItem>> GetAllMonitorsAsync(CancellationToken cancellationToken = default)
    {
        var scan = _context.ScanAsync<MonitorItem>(new List<ScanCondition>());
        return await scan.GetRemainingAsync(cancellationToken);
    }

    public async Task<MonitorItem?> GetMonitorAsync(string monitorId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(monitorId)) return null;
        return await _context.LoadAsync<MonitorItem>(monitorId, cancellationToken);
    }

    public async Task SaveMonitorAsync(MonitorItem monitor, CancellationToken cancellationToken = default)
    {
        if (monitor == null) throw new ArgumentNullException(nameof(monitor));
        await _context.SaveAsync(monitor, cancellationToken);
    }
}
