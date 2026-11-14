namespace CloudPulse.HealthChecker.Tests;

using CloudPulse.Core.Entity;
using CloudPulse.HealthChecker.Repositories;
using CloudPulse.HealthChecker.Services;
using Moq;
using Xunit;

public class StateMachineEngineTests
{
    private readonly StateMachineEngine _engine = new();

    [Fact]
    public void Evaluate_FirstFailure_DoesNotTransitionToDown_SuppressesAlert()
    {
        // Arrange
        var monitor = new MonitorItem
        {
            MonitorId = "mon-1",
            Status = MonitorStatus.Up,
            ConsecutiveFailures = 0
        };

        var checkResult = new CheckResultItem
        {
            MonitorId = "mon-1",
            IsSuccess = false,
            HttpStatus = 500,
            ErrorType = CheckErrorType.HttpError,
            ErrorMessage = "HTTP 500 Internal Server Error"
        };

        // Act
        var transition = _engine.Evaluate(monitor, checkResult, failureThreshold: 2);

        // Assert: 1 error does not trigger DOWN
        Assert.Equal(MonitorStatus.Up, transition.NewStatus);
        Assert.Equal(1, transition.ConsecutiveFailures);
        Assert.False(transition.ShouldOpenIncident);
        Assert.False(transition.ShouldAlertDown);
    }

    [Fact]
    public void Evaluate_SecondConsecutiveFailure_TransitionsToDown_AndFlagsAlertAndIncident()
    {
        // Arrange
        var monitor = new MonitorItem
        {
            MonitorId = "mon-1",
            Status = MonitorStatus.Up,
            ConsecutiveFailures = 1
        };

        var checkResult = new CheckResultItem
        {
            MonitorId = "mon-1",
            IsSuccess = false,
            HttpStatus = 503,
            ErrorType = CheckErrorType.HttpError,
            ErrorMessage = "HTTP 503 Service Unavailable"
        };

        // Act
        var transition = _engine.Evaluate(monitor, checkResult, failureThreshold: 2);

        // Assert: 2nd consecutive failure transitions to DOWN
        Assert.Equal(MonitorStatus.Down, transition.NewStatus);
        Assert.Equal(2, transition.ConsecutiveFailures);
        Assert.True(transition.ShouldOpenIncident);
        Assert.True(transition.ShouldAlertDown);
        Assert.False(transition.ShouldAlertRecovery);
    }

    [Fact]
    public void Evaluate_SubsequentFailuresWhileDown_SuppressesRepeatedAlerts_AntiSpam()
    {
        // Arrange
        var monitor = new MonitorItem
        {
            MonitorId = "mon-1",
            Status = MonitorStatus.Down,
            ConsecutiveFailures = 2
        };

        var checkResult = new CheckResultItem
        {
            MonitorId = "mon-1",
            IsSuccess = false,
            HttpStatus = 500,
            ErrorType = CheckErrorType.HttpError
        };

        // Act
        var transition = _engine.Evaluate(monitor, checkResult, failureThreshold: 2);

        // Assert: Remains DOWN, counter increments, NO duplicate incident or alert
        Assert.Equal(MonitorStatus.Down, transition.NewStatus);
        Assert.Equal(3, transition.ConsecutiveFailures);
        Assert.False(transition.ShouldOpenIncident);
        Assert.False(transition.ShouldAlertDown);
        Assert.False(transition.ShouldAlertRecovery);
    }

    [Fact]
    public void Evaluate_SuccessWhileDown_TransitionsToUp_AndFlagsRecoveryAlertAndIncidentResolution()
    {
        // Arrange
        var monitor = new MonitorItem
        {
            MonitorId = "mon-1",
            Status = MonitorStatus.Down,
            ConsecutiveFailures = 3
        };

        var checkResult = new CheckResultItem
        {
            MonitorId = "mon-1",
            IsSuccess = true,
            HttpStatus = 200,
            ErrorType = CheckErrorType.None
        };

        // Act
        var transition = _engine.Evaluate(monitor, checkResult, failureThreshold: 2);

        // Assert: Transitions to UP, resets failures, flags recovery and incident resolution
        Assert.Equal(MonitorStatus.Up, transition.NewStatus);
        Assert.Equal(0, transition.ConsecutiveFailures);
        Assert.True(transition.ShouldResolveIncident);
        Assert.True(transition.ShouldAlertRecovery);
        Assert.False(transition.ShouldAlertDown);
    }

    [Fact]
    public async Task ProcessTransitionAsync_CreatesOpenIncident_WhenTransitioningToDown()
    {
        // Arrange
        var mockIncidentRepo = new Mock<IIncidentRepository>();
        IncidentItem? capturedIncident = null;

        mockIncidentRepo.Setup(r => r.SaveIncidentAsync(It.IsAny<IncidentItem>(), It.IsAny<CancellationToken>()))
            .Callback<IncidentItem, CancellationToken>((inc, ct) => capturedIncident = inc)
            .Returns(Task.CompletedTask);

        var monitor = new MonitorItem
        {
            MonitorId = "mon-test",
            Status = MonitorStatus.Up,
            ConsecutiveFailures = 1
        };

        var checkResult = new CheckResultItem
        {
            MonitorId = "mon-test",
            IsSuccess = false,
            HttpStatus = 500,
            ErrorMessage = "Crash"
        };

        // Act
        var transition = await _engine.ProcessTransitionAsync(monitor, checkResult, mockIncidentRepo.Object, failureThreshold: 2);

        // Assert
        Assert.Equal(MonitorStatus.Down, monitor.Status);
        Assert.Equal(2, monitor.ConsecutiveFailures);
        Assert.NotNull(capturedIncident);
        Assert.Equal("mon-test", capturedIncident.MonitorId);
        Assert.Equal(IncidentStatus.Open, capturedIncident.Status);
        Assert.Equal("Crash", capturedIncident.Reason);
    }

    [Fact]
    public async Task ProcessTransitionAsync_ResolvesIncidentAndCalculatesDowntime_WhenRecovering()
    {
        // Arrange
        var mockIncidentRepo = new Mock<IIncidentRepository>();
        var openedAt = DateTime.UtcNow.AddMinutes(-5).ToString("o");

        var existingIncident = new IncidentItem
        {
            MonitorId = "mon-rec",
            IncidentId = "INC-12345",
            Status = IncidentStatus.Open,
            OpenedAt = openedAt,
            Reason = "Server down"
        };

        mockIncidentRepo.Setup(r => r.GetOpenIncidentAsync("mon-rec", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingIncident);

        mockIncidentRepo.Setup(r => r.SaveIncidentAsync(It.IsAny<IncidentItem>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var monitor = new MonitorItem
        {
            MonitorId = "mon-rec",
            Status = MonitorStatus.Down,
            ConsecutiveFailures = 5
        };

        var checkResult = new CheckResultItem
        {
            MonitorId = "mon-rec",
            IsSuccess = true,
            HttpStatus = 200
        };

        // Act
        var transition = await _engine.ProcessTransitionAsync(monitor, checkResult, mockIncidentRepo.Object, failureThreshold: 2);

        // Assert
        Assert.Equal(MonitorStatus.Up, monitor.Status);
        Assert.Equal(0, monitor.ConsecutiveFailures);
        Assert.Equal(IncidentStatus.Resolved, existingIncident.Status);
        Assert.NotNull(existingIncident.ResolvedAt);
        Assert.NotNull(existingIncident.DowntimeSeconds);
        Assert.InRange(existingIncident.DowntimeSeconds.Value, 290, 310);
        Assert.Equal(existingIncident.DowntimeSeconds, transition.DowntimeSeconds);
    }
}
