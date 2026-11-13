using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.SimpleNotificationService;
using CloudPulse.HealthChecker.Repositories;
using CloudPulse.HealthChecker.Services;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace CloudPulse.HealthChecker;

public class Function
{
    private readonly IMonitorRepository _monitorRepository;
    private readonly ICheckResultRepository _checkResultRepository;
    private readonly IIncidentRepository _incidentRepository;
    private readonly IHttpPingService _pingService;
    private readonly IStateMachineEngine _stateMachine;
    private readonly IAlertDispatcher _alertDispatcher;
    private readonly int _consecutiveFailuresThreshold;

    public int ConsecutiveFailuresThreshold => _consecutiveFailuresThreshold;

    /// <summary>
    /// Parameterless constructor for AWS Lambda runtime execution.
    /// </summary>
    public Function()
    {
        var dynamoDbClient = new AmazonDynamoDBClient();
        var dynamoDbContext = new DynamoDBContext(dynamoDbClient);
        var snsClient = new AmazonSimpleNotificationServiceClient();

        _monitorRepository = new MonitorRepository(dynamoDbContext);
        _checkResultRepository = new CheckResultRepository(dynamoDbContext);
        _incidentRepository = new IncidentRepository(dynamoDbContext);

        _pingService = new HttpPingService();
        _stateMachine = new StateMachineEngine();

        var snsChannel = new SnsAlertChannel(snsClient);
        var telegramChannel = new TelegramAlertChannel();
        _alertDispatcher = new AlertDispatcher(snsChannel, telegramChannel);
        _consecutiveFailuresThreshold = ResolveConsecutiveFailuresThreshold();
    }

    /// <summary>
    /// Dependency injection constructor for unit and integration testing.
    /// </summary>
    public Function(
        IMonitorRepository monitorRepository,
        ICheckResultRepository checkResultRepository,
        IIncidentRepository incidentRepository,
        IHttpPingService pingService,
        IStateMachineEngine stateMachine,
        IAlertDispatcher alertDispatcher,
        int? consecutiveFailuresThreshold = null)
    {
        _monitorRepository = monitorRepository ?? throw new ArgumentNullException(nameof(monitorRepository));
        _checkResultRepository = checkResultRepository ?? throw new ArgumentNullException(nameof(checkResultRepository));
        _incidentRepository = incidentRepository ?? throw new ArgumentNullException(nameof(incidentRepository));
        _pingService = pingService ?? throw new ArgumentNullException(nameof(pingService));
        _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        _alertDispatcher = alertDispatcher ?? throw new ArgumentNullException(nameof(alertDispatcher));
        _consecutiveFailuresThreshold = consecutiveFailuresThreshold ?? ResolveConsecutiveFailuresThreshold();
    }

    private static int ResolveConsecutiveFailuresThreshold()
    {
        var thresholdEnv = Environment.GetEnvironmentVariable("CONSECUTIVE_FAILURES_THRESHOLD");
        if (int.TryParse(thresholdEnv, out var threshold) && threshold > 0)
        {
            return threshold;
        }
        return 2;
    }

    public async Task<string> FunctionHandler(object? input, ILambdaContext? context)
    {
        context?.Logger?.LogLine("Starting CloudPulse HealthChecker execution cycle...");

        // 1. Fetch all monitors from DynamoDB
        var monitors = await _monitorRepository.GetAllMonitorsAsync();
        if (monitors.Count == 0)
        {
            context?.Logger?.LogLine("No monitors found in CloudPulse_Monitors.");
            return "0 monitors processed";
        }

        context?.Logger?.LogLine($"Executing concurrent health checks for {monitors.Count} endpoints...");

        // 2. Ping all endpoints concurrently with Task.WhenAll
        var checkResults = await _pingService.PingAllAsync(monitors);

        // 3. Save all check result items to DynamoDB in batch
        await _checkResultRepository.SaveCheckResultsBatchAsync(checkResults);

        // 4. Map results by MonitorId and evaluate State Machine
        var resultsMap = checkResults.ToDictionary(r => r.MonitorId);

        foreach (var monitor in monitors)
        {
            if (resultsMap.TryGetValue(monitor.MonitorId, out var checkResult))
            {
                try
                {
                    var transition = await _stateMachine.ProcessTransitionAsync(
                        monitor,
                        checkResult,
                        _incidentRepository,
                        failureThreshold: _consecutiveFailuresThreshold);

                    // Update monitor table
                    await _monitorRepository.SaveMonitorAsync(monitor);

                    // Dispatch alerts (SNS + Telegram) if transition requires it
                    await _alertDispatcher.DispatchAsync(monitor, transition);
                }
                catch (Exception ex)
                {
                    var errMsg = $"[ERROR] Failed processing monitor {monitor.MonitorId} ({monitor.Name}): {ex.Message}";
                    context?.Logger?.LogLine(errMsg);
                    Console.WriteLine(errMsg);
                }
            }
        }

        string summary = $"Completed checking {monitors.Count} monitors.";
        context?.Logger?.LogLine(summary);
        return summary;
    }
}
