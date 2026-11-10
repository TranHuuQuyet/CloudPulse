namespace CloudPulse.HealthChecker.Tests;

using CloudPulse.Core.Entity;
using CloudPulse.HealthChecker.Services;
using Moq;
using Xunit;

public class AlertDispatcherTests
{
    private readonly Mock<ISnsAlertChannel> _mockSns = new();
    private readonly Mock<ITelegramAlertChannel> _mockTelegram = new();
    private readonly AlertDispatcher _dispatcher;

    public AlertDispatcherTests()
    {
        _dispatcher = new AlertDispatcher(_mockSns.Object, _mockTelegram.Object);
    }

    [Fact]
    public async Task DispatchAsync_WhenShouldAlertDown_CallsBothSnsAndTelegram()
    {
        // Arrange
        var monitor = new MonitorItem
        {
            MonitorId = "mon-1",
            Name = "Payment Gateway",
            Url = "https://pay.example.com"
        };

        var transition = new StateTransitionResult(
            PreviousStatus: MonitorStatus.Up,
            NewStatus: MonitorStatus.Down,
            ConsecutiveFailures: 2,
            ShouldOpenIncident: true,
            ShouldResolveIncident: false,
            ShouldAlertDown: true,
            ShouldAlertRecovery: false,
            Reason: "HTTP 500",
            DowntimeSeconds: null
        );

        // Act
        await _dispatcher.DispatchAsync(monitor, transition);

        // Assert
        _mockSns.Verify(s => s.SendAlertAsync(
            monitor,
            "DOWN",
            It.Is<string>(m => m.Contains("DOWN") && m.Contains("Payment Gateway") || m.Contains(monitor.Url)),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _mockTelegram.Verify(t => t.SendAlertAsync(
            monitor,
            "DOWN",
            It.Is<string>(m => m.Contains("DOWN") && m.Contains(monitor.Url)),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_WhenShouldAlertRecovery_CallsBothSnsAndTelegram()
    {
        // Arrange
        var monitor = new MonitorItem
        {
            MonitorId = "mon-1",
            Name = "Payment Gateway",
            Url = "https://pay.example.com"
        };

        var transition = new StateTransitionResult(
            PreviousStatus: MonitorStatus.Down,
            NewStatus: MonitorStatus.Up,
            ConsecutiveFailures: 0,
            ShouldOpenIncident: false,
            ShouldResolveIncident: true,
            ShouldAlertDown: false,
            ShouldAlertRecovery: true,
            Reason: null,
            DowntimeSeconds: 150
        );

        // Act
        await _dispatcher.DispatchAsync(monitor, transition);

        // Assert
        _mockSns.Verify(s => s.SendAlertAsync(
            monitor,
            "RECOVERED",
            It.Is<string>(m => m.Contains("150")),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _mockTelegram.Verify(t => t.SendAlertAsync(
            monitor,
            "RECOVERED",
            It.Is<string>(m => m.Contains("150")),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_WhenNeitherAlertFlagged_SuppressesAllAlerts_AntiSpam()
    {
        // Arrange
        var monitor = new MonitorItem
        {
            MonitorId = "mon-1",
            Name = "Payment Gateway",
            Url = "https://pay.example.com"
        };

        // Steady state or suppressed 1st error
        var transition = new StateTransitionResult(
            PreviousStatus: MonitorStatus.Up,
            NewStatus: MonitorStatus.Up,
            ConsecutiveFailures: 1,
            ShouldOpenIncident: false,
            ShouldResolveIncident: false,
            ShouldAlertDown: false,
            ShouldAlertRecovery: false,
            Reason: "1st fail",
            DowntimeSeconds: null
        );

        // Act
        await _dispatcher.DispatchAsync(monitor, transition);

        // Assert: 0 calls to either channel
        _mockSns.Verify(s => s.SendAlertAsync(It.IsAny<MonitorItem>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockTelegram.Verify(t => t.SendAlertAsync(It.IsAny<MonitorItem>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
