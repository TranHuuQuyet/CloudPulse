namespace CloudPulse.HealthChecker.Tests;

using Amazon.Lambda.Core;
using CloudPulse.Core.Entity;
using CloudPulse.HealthChecker.Repositories;
using CloudPulse.HealthChecker.Services;
using Moq;
using Xunit;

public class FunctionTests
{
    private readonly Mock<IMonitorRepository> _mockMonitorRepo = new();
    private readonly Mock<ICheckResultRepository> _mockCheckResultRepo = new();
    private readonly Mock<IIncidentRepository> _mockIncidentRepo = new();
    private readonly Mock<IHttpPingService> _mockPingService = new();
    private readonly Mock<IStateMachineEngine> _mockStateMachine = new();
    private readonly Mock<IAlertDispatcher> _mockAlertDispatcher = new();
    private readonly Mock<ILambdaContext> _mockContext = new();
    private readonly Mock<ILambdaLogger> _mockLogger = new();

    public FunctionTests()
    {
        _mockContext.Setup(c => c.Logger).Returns(_mockLogger.Object);
    }

    [Fact]
    public async Task FunctionHandler_WhenNoMonitorsFound_ExitsGracefully()
    {
        // Arrange
        _mockMonitorRepo.Setup(r => r.GetAllMonitorsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MonitorItem>());

        var function = new Function(
            _mockMonitorRepo.Object,
            _mockCheckResultRepo.Object,
            _mockIncidentRepo.Object,
            _mockPingService.Object,
            _mockStateMachine.Object,
            _mockAlertDispatcher.Object
        );

        // Act
        var result = await function.FunctionHandler(null, _mockContext.Object);

        // Assert
        Assert.Equal("0 monitors processed", result);
        _mockPingService.Verify(p => p.PingAllAsync(It.IsAny<IEnumerable<MonitorItem>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FunctionHandler_PingsConcurrentlyAndUpdatesMonitorsAndAlerts()
    {
        // Arrange
        var monitor1 = new MonitorItem { MonitorId = "mon-1", Name = "API 1", Url = "https://api1.example.com", Status = MonitorStatus.Up };
        var monitor2 = new MonitorItem { MonitorId = "mon-2", Name = "API 2", Url = "https://api2.example.com", Status = MonitorStatus.Up };
        var monitors = new List<MonitorItem> { monitor1, monitor2 };

        var check1 = new CheckResultItem { MonitorId = "mon-1", IsSuccess = true, HttpStatus = 200, ResponseTimeMs = 50 };
        var check2 = new CheckResultItem { MonitorId = "mon-2", IsSuccess = false, HttpStatus = 500, ResponseTimeMs = 120 };
        var checkResults = new List<CheckResultItem> { check1, check2 };

        _mockMonitorRepo.Setup(r => r.GetAllMonitorsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(monitors);

        _mockPingService.Setup(p => p.PingAllAsync(monitors, It.IsAny<CancellationToken>()))
            .ReturnsAsync(checkResults);

        _mockStateMachine.Setup(s => s.ProcessTransitionAsync(
                monitor1, check1, _mockIncidentRepo.Object, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StateTransitionResult(MonitorStatus.Up, MonitorStatus.Up, 0, false, false, false, false, null, null));

        _mockStateMachine.Setup(s => s.ProcessTransitionAsync(
                monitor2, check2, _mockIncidentRepo.Object, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StateTransitionResult(MonitorStatus.Up, MonitorStatus.Up, 1, false, false, false, false, "HTTP 500", null));

        var function = new Function(
            _mockMonitorRepo.Object,
            _mockCheckResultRepo.Object,
            _mockIncidentRepo.Object,
            _mockPingService.Object,
            _mockStateMachine.Object,
            _mockAlertDispatcher.Object
        );

        // Act
        var result = await function.FunctionHandler(null, _mockContext.Object);

        // Assert
        Assert.Equal("Completed checking 2 monitors.", result);
        _mockCheckResultRepo.Verify(r => r.SaveCheckResultsBatchAsync(checkResults, It.IsAny<CancellationToken>()), Times.Once);
        _mockMonitorRepo.Verify(r => r.SaveMonitorAsync(monitor1, It.IsAny<CancellationToken>()), Times.Once);
        _mockMonitorRepo.Verify(r => r.SaveMonitorAsync(monitor2, It.IsAny<CancellationToken>()), Times.Once);
        _mockAlertDispatcher.Verify(a => a.DispatchAsync(monitor1, It.IsAny<StateTransitionResult>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockAlertDispatcher.Verify(a => a.DispatchAsync(monitor2, It.IsAny<StateTransitionResult>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FunctionHandler_WhenOneMonitorThrowsException_ContinuesProcessingOtherMonitors()
    {
        // Arrange
        var monitor1 = new MonitorItem { MonitorId = "mon-faulty", Name = "Faulty Monitor", Url = "https://faulty.example.com" };
        var monitor2 = new MonitorItem { MonitorId = "mon-healthy", Name = "Healthy Monitor", Url = "https://healthy.example.com" };
        var monitors = new List<MonitorItem> { monitor1, monitor2 };

        var check1 = new CheckResultItem { MonitorId = "mon-faulty", IsSuccess = false };
        var check2 = new CheckResultItem { MonitorId = "mon-healthy", IsSuccess = true };
        var checkResults = new List<CheckResultItem> { check1, check2 };

        _mockMonitorRepo.Setup(r => r.GetAllMonitorsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(monitors);
        _mockPingService.Setup(p => p.PingAllAsync(monitors, It.IsAny<CancellationToken>()))
            .ReturnsAsync(checkResults);

        // Monitor 1 throws during state machine processing
        _mockStateMachine.Setup(s => s.ProcessTransitionAsync(monitor1, check1, _mockIncidentRepo.Object, 2, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Simulated DynamoDB network timeout"));

        // Monitor 2 succeeds normally
        _mockStateMachine.Setup(s => s.ProcessTransitionAsync(monitor2, check2, _mockIncidentRepo.Object, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StateTransitionResult(MonitorStatus.Up, MonitorStatus.Up, 0, false, false, false, false, null, null));

        var function = new Function(
            _mockMonitorRepo.Object,
            _mockCheckResultRepo.Object,
            _mockIncidentRepo.Object,
            _mockPingService.Object,
            _mockStateMachine.Object,
            _mockAlertDispatcher.Object
        );

        // Act
        var result = await function.FunctionHandler(null, _mockContext.Object);

        // Assert: FunctionHandler completes without throwing, and healthy monitor was processed
        Assert.Equal("Completed checking 2 monitors.", result);
        _mockMonitorRepo.Verify(r => r.SaveMonitorAsync(monitor2, It.IsAny<CancellationToken>()), Times.Once);
        _mockAlertDispatcher.Verify(a => a.DispatchAsync(monitor2, It.IsAny<StateTransitionResult>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("3", 3)]
    [InlineData("5", 5)]
    public void Function_WhenConsecutiveFailuresThresholdEnvConfigured_UsesConfiguredThreshold(string envValue, int expectedThreshold)
    {
        var originalEnv = Environment.GetEnvironmentVariable("CONSECUTIVE_FAILURES_THRESHOLD");
        try
        {
            Environment.SetEnvironmentVariable("CONSECUTIVE_FAILURES_THRESHOLD", envValue);
            var function = new Function(
                _mockMonitorRepo.Object,
                _mockCheckResultRepo.Object,
                _mockIncidentRepo.Object,
                _mockPingService.Object,
                _mockStateMachine.Object,
                _mockAlertDispatcher.Object
            );

            Assert.Equal(expectedThreshold, function.ConsecutiveFailuresThreshold);
        }
        finally
        {
            Environment.SetEnvironmentVariable("CONSECUTIVE_FAILURES_THRESHOLD", originalEnv);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-number")]
    [InlineData("-2")]
    [InlineData("0")]
    public void Function_WhenConsecutiveFailuresThresholdEnvInvalidOrMissing_FallsBackTo2(string? envValue)
    {
        var originalEnv = Environment.GetEnvironmentVariable("CONSECUTIVE_FAILURES_THRESHOLD");
        try
        {
            Environment.SetEnvironmentVariable("CONSECUTIVE_FAILURES_THRESHOLD", envValue);
            var function = new Function(
                _mockMonitorRepo.Object,
                _mockCheckResultRepo.Object,
                _mockIncidentRepo.Object,
                _mockPingService.Object,
                _mockStateMachine.Object,
                _mockAlertDispatcher.Object
            );

            Assert.Equal(2, function.ConsecutiveFailuresThreshold);
        }
        finally
        {
            Environment.SetEnvironmentVariable("CONSECUTIVE_FAILURES_THRESHOLD", originalEnv);
        }
    }

    [Fact]
    public async Task FunctionHandler_PassesConfiguredThresholdToStateMachine()
    {
        // Arrange
        var monitor = new MonitorItem { MonitorId = "mon-custom", Name = "Custom Monitor", Url = "https://custom.example.com" };
        var monitors = new List<MonitorItem> { monitor };
        var checkResult = new CheckResultItem { MonitorId = "mon-custom", IsSuccess = false };
        var checkResults = new List<CheckResultItem> { checkResult };

        _mockMonitorRepo.Setup(r => r.GetAllMonitorsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(monitors);
        _mockPingService.Setup(p => p.PingAllAsync(monitors, It.IsAny<CancellationToken>()))
            .ReturnsAsync(checkResults);

        _mockStateMachine.Setup(s => s.ProcessTransitionAsync(monitor, checkResult, _mockIncidentRepo.Object, 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StateTransitionResult(MonitorStatus.Up, MonitorStatus.Up, 1, false, false, false, false, null, null));

        var function = new Function(
            _mockMonitorRepo.Object,
            _mockCheckResultRepo.Object,
            _mockIncidentRepo.Object,
            _mockPingService.Object,
            _mockStateMachine.Object,
            _mockAlertDispatcher.Object,
            consecutiveFailuresThreshold: 4
        );

        // Act
        var result = await function.FunctionHandler(null, _mockContext.Object);

        // Assert
        Assert.Equal("Completed checking 1 monitors.", result);
        _mockStateMachine.Verify(s => s.ProcessTransitionAsync(monitor, checkResult, _mockIncidentRepo.Object, 4, It.IsAny<CancellationToken>()), Times.Once);
    }
}
