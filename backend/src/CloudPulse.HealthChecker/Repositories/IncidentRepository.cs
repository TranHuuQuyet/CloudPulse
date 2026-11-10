namespace CloudPulse.HealthChecker.Repositories;

using Amazon.DynamoDBv2.DataModel;
using CloudPulse.Core.Entity;

public class IncidentRepository : IIncidentRepository
{
    private readonly IDynamoDBContext _context;

    public IncidentRepository(IDynamoDBContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IncidentItem?> GetOpenIncidentAsync(string monitorId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(monitorId)) return null;

        var query = _context.QueryAsync<IncidentItem>(monitorId);
        var incidents = await query.GetRemainingAsync(cancellationToken);
        return incidents.FirstOrDefault(i => string.Equals(i.Status, IncidentStatus.Open, StringComparison.OrdinalIgnoreCase));
    }

    public async Task SaveIncidentAsync(IncidentItem incident, CancellationToken cancellationToken = default)
    {
        if (incident == null) throw new ArgumentNullException(nameof(incident));
        await _context.SaveAsync(incident, cancellationToken);
    }
}
