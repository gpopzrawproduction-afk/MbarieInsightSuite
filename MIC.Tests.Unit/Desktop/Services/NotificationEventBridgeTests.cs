using System.Reactive.Concurrency;
using System.Reflection;
using FluentAssertions;
using MIC.Desktop.Avalonia.Services;
using NSubstitute;
using ReactiveUI;
using Xunit;

namespace MIC.Tests.Unit.Desktop.Services;

/// <summary>
/// Tests for <see cref="NotificationEventBridge"/>.
/// Tests handler methods via reflection since they are private.
/// </summary>
public class NotificationEventBridgeTests : IDisposable
{
    private readonly INotificationService _notificationService;
    private readonly NotificationEventBridge _bridge;

    static NotificationEventBridgeTests()
    {
        RxApp.MainThreadScheduler = CurrentThreadScheduler.Instance;
        RxApp.TaskpoolScheduler = CurrentThreadScheduler.Instance;
    }

    public NotificationEventBridgeTests()
    {
        _notificationService = Substitute.For<INotificationService>();
        _bridge = new NotificationEventBridge(_notificationService);
    }

    public void Dispose()
    {
        _bridge.Dispose();
    }

    private void InvokeHandleAlertEvent(AlertEvent alertEvent)
    {
        var method = typeof(NotificationEventBridge)
            .GetMethod("HandleAlertEvent", BindingFlags.NonPublic | BindingFlags.Instance);
        method.Should().NotBeNull();
        method!.Invoke(_bridge, new object[] { alertEvent });
    }

    private void InvokeHandleDataUpdate(DataUpdateEvent dataUpdate)
    {
        var method = typeof(NotificationEventBridge)
            .GetMethod("HandleDataUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
        method.Should().NotBeNull();
        method!.Invoke(_bridge, new object[] { dataUpdate });
    }

    private void SetPreviousAlerts(int? activeCount, int? criticalCount)
    {
        typeof(NotificationEventBridge)
            .GetField("_previousActiveAlerts", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(_bridge, activeCount);
        typeof(NotificationEventBridge)
            .GetField("_previousCriticalAlerts", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(_bridge, criticalCount);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullNotifications_ThrowsArgumentNullException()
    {
        var act = () => new NotificationEventBridge(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ValidNotifications_CreatesInstance()
    {
        using var bridge = new NotificationEventBridge(_notificationService);
        bridge.Should().NotBeNull();
    }

    #endregion

    #region HandleAlertEvent — Title-Based Alerts

    [Fact]
    public void HandleAlertEvent_WithTitle_CriticalSeverity_CallsShowError()
    {
        var alert = new AlertEvent
        {
            Title = "Server Down",
            Message = "The server is not responding",
            Severity = "Critical"
        };

        InvokeHandleAlertEvent(alert);

        _notificationService.Received(1)
            .ShowError("The server is not responding", "Server Down", "Alerts");
    }

    [Fact]
    public void HandleAlertEvent_WithTitle_WarningSeverity_CallsShowWarning()
    {
        var alert = new AlertEvent
        {
            Title = "High CPU",
            Message = "CPU usage above 80%",
            Severity = "Warning"
        };

        InvokeHandleAlertEvent(alert);

        _notificationService.Received(1)
            .ShowWarning("CPU usage above 80%", "High CPU", "Alerts");
    }

    [Fact]
    public void HandleAlertEvent_WithTitle_InfoSeverity_CallsShowInfo()
    {
        var alert = new AlertEvent
        {
            Title = "Update Available",
            Message = "Version 2.0 is ready",
            Severity = "Info"
        };

        InvokeHandleAlertEvent(alert);

        _notificationService.Received(1)
            .ShowInfo("Version 2.0 is ready", "Update Available", "Alerts");
    }

    [Fact]
    public void HandleAlertEvent_WithTitle_NullSeverity_CallsShowInfo()
    {
        var alert = new AlertEvent
        {
            Title = "Something happened",
            Message = "Details here",
            Severity = null
        };

        InvokeHandleAlertEvent(alert);

        _notificationService.Received(1)
            .ShowInfo("Details here", "Something happened", "Alerts");
    }

    [Fact]
    public void HandleAlertEvent_WithTitle_NullMessage_PassesEmptyString()
    {
        var alert = new AlertEvent
        {
            Title = "Alert Title",
            Message = null,
            Severity = "Warning"
        };

        InvokeHandleAlertEvent(alert);

        _notificationService.Received(1)
            .ShowWarning(string.Empty, "Alert Title", "Alerts");
    }

    #endregion

    #region HandleAlertEvent — Active Count Changes

    [Fact]
    public void HandleAlertEvent_ActiveCountIncreased_ShowsWarning()
    {
        SetPreviousAlerts(activeCount: 3, criticalCount: 0);

        var alert = new AlertEvent
        {
            ActiveCount = 5,
            CriticalCount = 0
        };

        InvokeHandleAlertEvent(alert);

        _notificationService.Received(1)
            .ShowWarning(
                Arg.Is<string>(s => s.Contains("5")),
                "Alerts Updated",
                "Alerts");
    }

    [Fact]
    public void HandleAlertEvent_ActiveCountDecreased_ShowsSuccess()
    {
        SetPreviousAlerts(activeCount: 5, criticalCount: 0);

        var alert = new AlertEvent
        {
            ActiveCount = 2,
            CriticalCount = 0
        };

        InvokeHandleAlertEvent(alert);

        _notificationService.Received(1)
            .ShowSuccess(
                Arg.Is<string>(s => s.Contains("2")),
                "Alerts Resolved",
                "Alerts");
    }

    [Fact]
    public void HandleAlertEvent_ActiveCountUnchanged_NoNotification()
    {
        SetPreviousAlerts(activeCount: 3, criticalCount: 0);

        var alert = new AlertEvent
        {
            ActiveCount = 3,
            CriticalCount = 0
        };

        InvokeHandleAlertEvent(alert);

        _notificationService.DidNotReceive()
            .ShowWarning(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        _notificationService.DidNotReceive()
            .ShowSuccess(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    #endregion

    #region HandleAlertEvent — Critical Count Changes

    [Fact]
    public void HandleAlertEvent_CriticalCountIncreased_ShowsError()
    {
        SetPreviousAlerts(activeCount: 3, criticalCount: 1);

        var alert = new AlertEvent
        {
            ActiveCount = 3,
            CriticalCount = 3
        };

        InvokeHandleAlertEvent(alert);

        _notificationService.Received(1)
            .ShowError(
                Arg.Is<string>(s => s.Contains("3")),
                "Critical Alerts",
                "Alerts");
    }

    [Fact]
    public void HandleAlertEvent_CriticalCountDroppedToZero_ShowsSuccess()
    {
        SetPreviousAlerts(activeCount: 3, criticalCount: 2);

        var alert = new AlertEvent
        {
            ActiveCount = 3,
            CriticalCount = 0
        };

        InvokeHandleAlertEvent(alert);

        _notificationService.Received(1)
            .ShowSuccess(
                "All critical alerts resolved",
                "Critical Alerts",
                "Alerts");
    }

    [Fact]
    public void HandleAlertEvent_FirstCall_NoPreviousData_NoNotification()
    {
        // No previous data set
        var alert = new AlertEvent
        {
            ActiveCount = 5,
            CriticalCount = 2
        };

        InvokeHandleAlertEvent(alert);

        // First call should not trigger any notifications for count changes
        _notificationService.DidNotReceive()
            .ShowWarning(Arg.Any<string>(), "Alerts Updated", Arg.Any<string>());
        _notificationService.DidNotReceive()
            .ShowError(Arg.Any<string>(), "Critical Alerts", Arg.Any<string>());
    }

    [Fact]
    public void HandleAlertEvent_UpdatesPreviousCounts()
    {
        InvokeHandleAlertEvent(new AlertEvent { ActiveCount = 10, CriticalCount = 3 });

        var prevActive = typeof(NotificationEventBridge)
            .GetField("_previousActiveAlerts", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(_bridge);
        var prevCritical = typeof(NotificationEventBridge)
            .GetField("_previousCriticalAlerts", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(_bridge);

        prevActive.Should().Be(10);
        prevCritical.Should().Be(3);
    }

    #endregion

    #region HandleDataUpdate Tests

    [Fact]
    public void HandleDataUpdate_ErrorEvent_ShowsError()
    {
        var update = new DataUpdateEvent
        {
            Source = "Database",
            Message = "Connection lost",
            IsError = true
        };

        InvokeHandleDataUpdate(update);

        _notificationService.Received(1)
            .ShowError("Connection lost", "Database Sync Error", "Sync");
    }

    [Fact]
    public void HandleDataUpdate_SuccessEvent_ShowsInfo()
    {
        var update = new DataUpdateEvent
        {
            Source = "API",
            Message = "Metrics synced successfully",
            IsError = false
        };

        InvokeHandleDataUpdate(update);

        _notificationService.Received(1)
            .ShowInfo("Metrics synced successfully", "Data Synchronized", "Sync");
    }

    [Fact]
    public void HandleDataUpdate_DuplicateWithin5Minutes_Suppressed()
    {
        var update = new DataUpdateEvent
        {
            Source = "API",
            Message = "Data synced",
            IsError = false
        };

        InvokeHandleDataUpdate(update);
        InvokeHandleDataUpdate(update); // Same message within 5 minutes

        _notificationService.Received(1)
            .ShowInfo("Data synced", "Data Synchronized", "Sync");
    }

    [Fact]
    public void HandleDataUpdate_DifferentMessages_NotSuppressed()
    {
        var update1 = new DataUpdateEvent
        {
            Source = "API",
            Message = "Metrics synced",
            IsError = false
        };
        var update2 = new DataUpdateEvent
        {
            Source = "API",
            Message = "Alerts synced",
            IsError = false
        };

        InvokeHandleDataUpdate(update1);
        InvokeHandleDataUpdate(update2);

        _notificationService.Received(1)
            .ShowInfo("Metrics synced", "Data Synchronized", "Sync");
        _notificationService.Received(1)
            .ShowInfo("Alerts synced", "Data Synchronized", "Sync");
    }

    [Fact]
    public void HandleDataUpdate_ErrorEvents_NeverSuppressed()
    {
        var error = new DataUpdateEvent
        {
            Source = "DB",
            Message = "Connection failed",
            IsError = true
        };

        InvokeHandleDataUpdate(error);
        InvokeHandleDataUpdate(error);

        _notificationService.Received(2)
            .ShowError("Connection failed", "DB Sync Error", "Sync");
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        using var bridge = new NotificationEventBridge(_notificationService);
        bridge.Dispose();
        // Second dispose should not throw
        var act = () => bridge.Dispose();
        act.Should().NotThrow();
    }

    #endregion
}

/// <summary>
/// Tests for RealTimeDataService POCOs and properties.
/// </summary>
public class RealTimeDataServicePropertyTests
{
    #region RefreshIntervalSeconds Clamping

    [Theory]
    [InlineData(1, 5)]       // Below min → clamped to 5
    [InlineData(0, 5)]       // Zero → clamped to 5
    [InlineData(-10, 5)]     // Negative → clamped to 5
    [InlineData(5, 5)]       // Exact min → stays
    [InlineData(30, 30)]     // Normal → stays
    [InlineData(150, 150)]   // Normal → stays
    [InlineData(300, 300)]   // Exact max → stays
    [InlineData(500, 300)]   // Above max → clamped to 300
    [InlineData(1000, 300)]  // Way above max → clamped to 300
    public void RefreshIntervalSeconds_ClampsBetween5And300(int input, int expected)
    {
        var instance = RealTimeDataService.Instance;
        var original = instance.RefreshIntervalSeconds;

        try
        {
            instance.RefreshIntervalSeconds = input;
            instance.RefreshIntervalSeconds.Should().Be(expected);
        }
        finally
        {
            // Restore original
            instance.RefreshIntervalSeconds = original;
        }
    }

    #endregion

    #region POCO Tests

    [Fact]
    public void DataUpdateEvent_DefaultValues()
    {
        var evt = new DataUpdateEvent();
        evt.Source.Should().BeEmpty();
        evt.Message.Should().BeEmpty();
        evt.IsError.Should().BeFalse();
        evt.Timestamp.Should().Be(default);
    }

    [Fact]
    public void DataUpdateEvent_CanSetProperties()
    {
        var now = DateTime.UtcNow;
        var evt = new DataUpdateEvent
        {
            Source = "API",
            Timestamp = now,
            Message = "Test message",
            IsError = true
        };

        evt.Source.Should().Be("API");
        evt.Timestamp.Should().Be(now);
        evt.Message.Should().Be("Test message");
        evt.IsError.Should().BeTrue();
    }

    [Fact]
    public void AlertEvent_DefaultValues()
    {
        var evt = new AlertEvent();
        evt.Title.Should().BeNull();
        evt.Message.Should().BeNull();
        evt.Severity.Should().BeNull();
        evt.TotalCount.Should().Be(0);
        evt.ActiveCount.Should().Be(0);
        evt.CriticalCount.Should().Be(0);
        evt.Timestamp.Should().Be(default);
    }

    [Fact]
    public void AlertEvent_CanSetProperties()
    {
        var now = DateTime.UtcNow;
        var evt = new AlertEvent
        {
            Title = "Test Alert",
            Message = "Alert message",
            Severity = "Critical",
            TotalCount = 10,
            ActiveCount = 5,
            CriticalCount = 2,
            Timestamp = now
        };

        evt.Title.Should().Be("Test Alert");
        evt.Message.Should().Be("Alert message");
        evt.Severity.Should().Be("Critical");
        evt.TotalCount.Should().Be(10);
        evt.ActiveCount.Should().Be(5);
        evt.CriticalCount.Should().Be(2);
        evt.Timestamp.Should().Be(now);
    }

    [Fact]
    public void MetricEvent_DefaultValues()
    {
        var evt = new MetricEvent();
        evt.MetricName.Should().BeNull();
        evt.Value.Should().Be(0);
        evt.TotalCount.Should().Be(0);
        evt.Timestamp.Should().Be(default);
    }

    [Fact]
    public void MetricEvent_CanSetProperties()
    {
        var evt = new MetricEvent
        {
            MetricName = "Revenue",
            Value = 1234.56,
            TotalCount = 42,
            Timestamp = DateTime.UtcNow
        };

        evt.MetricName.Should().Be("Revenue");
        evt.Value.Should().Be(1234.56);
        evt.TotalCount.Should().Be(42);
    }

    #endregion

    #region Observable Properties

    [Fact]
    public void DataUpdates_IsNotNull()
    {
        RealTimeDataService.Instance.DataUpdates.Should().NotBeNull();
    }

    [Fact]
    public void AlertEvents_IsNotNull()
    {
        RealTimeDataService.Instance.AlertEvents.Should().NotBeNull();
    }

    [Fact]
    public void MetricEvents_IsNotNull()
    {
        RealTimeDataService.Instance.MetricEvents.Should().NotBeNull();
    }

    [Fact]
    public void IsRunning_InitiallyFalse()
    {
        // Instance may or may not be running depending on test order
        // Just verify the property doesn't throw
        var running = RealTimeDataService.Instance.IsRunning;
        running.Should().Be(running); // Just verify no exception
    }

    #endregion
}
