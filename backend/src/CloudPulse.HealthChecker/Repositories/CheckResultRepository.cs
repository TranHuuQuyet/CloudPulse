namespace CloudPulse.HealthChecker.Repositories;

using Amazon.DynamoDBv2.DataModel;
using CloudPulse.Core.Entity;

public class CheckResultRepository : ICheckResultRepository
{
    private readonly IDynamoDBContext _context;

    public CheckResultRepository(IDynamoDBContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task SaveCheckResultAsync(CheckResultItem result, CancellationToken cancellationToken = default)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));
        await _context.SaveAsync(result, cancellationToken);
    }

    public async Task SaveCheckResultsBatchAsync(IEnumerable<CheckResultItem> results, CancellationToken cancellationToken = default)
    {
        if (results == null) throw new ArgumentNullException(nameof(results));
        var list = results.ToList();
        if (list.Count == 0) return;

        var batch = _context.CreateBatchWrite<CheckResultItem>();
        batch.AddPutItems(list);
        await batch.ExecuteAsync(cancellationToken);
    }
}
