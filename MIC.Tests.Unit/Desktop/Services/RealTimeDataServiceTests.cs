using FluentAssertions;
using MIC.Desktop.Avalonia.Services;

namespace MIC.Tests.Unit.Desktop.Services;

/// <summary>
/// Tests for RealTimeDataService event DTOs (DataUpdateEvent, AlertEvent, MetricEvent)
/// and basic Start/Stop/RefreshInterval behavior.
/// </summary>
public class RealTimeDataServiceTests : IDisposable
{
    private readonly RealTimeDataService _service;

    public RealTimeDataServiceTests()
    {
        // Create a fresh instance (not the singleton) via reflection to avoid singleton state bleed
        _service = new RealTimeDataService();
    }

    public void Dispose()
    {
        _service.Stop();
        _service.Dispose();
    }

    #region DataUpdateEvent

    [Fact]
    public void DataUpdateEvent_Defaults_SourceIsEmpty()
    {
        new DataUpdateEvent().Source.Should().BeEmpty();
    }

    [Fact]
    public void DataUpdateEvent_Defaults_MessageIsEmpty()
    {
        new DataUpdateEvent().Message.Should().BeEmpty();
    }

    [Fact]
    public void DataUpdateEvent_Defaults_IsErrorIsFalse()
    {
        new DataUpdateEvent().IsError.Should().BeFalse();
    }

    [Fact]
    public void DataUpdateEvent_AllProperties_Settable()
    {
        var ts = DateTime.Now;
        var evt = new DataUpdateEvent
        {
            Source = "Database",
            Timestamp = ts,
            Message = "Sync complete",
            IsError = false
        };

        evt.Source.Should().Be("Database");
        evt.Timestamp.Should().Be(ts);
        evt.Message.Should().Be("Sync complete");
        evt.IsError.Should().BeFalse();
    }

    [Fact]
    public void DataUpdateEvent_ErrorEvent_HasIsErrorTrue()
    {
        var evt = new DataUpdateEvent
        {
            Source = "Error",
            Message = "Connection failed",
            IsError = true
        };

        evt.IsError.Should().BeTrue();
    }

    #endregion

    #region AlertEvent

    [Fact]
    public void AlertEvent_Defaults_TitleIsNull()
    {
        new AlertEvent().Title.Should().BeNull();
    }

    [Fact]
    public void AlertEvent_Defaults_MessageIsNull()
    {
        new AlertEvent().Message.Should().BeNull();
    }

    [Fact]
    public void AlertEvent_Defaults_SeverityIsNull()
    {
        new AlertEvent().Severity.Should().BeNull();
    }

    [Fact]
    public void AlertEvent_Defaults_CountsAreZero()
    {
        var evt = new AlertEvent();
        evt.TotalCount.Should().Be(0);
        evt.ActiveCount.Should().Be(0);
        evt.CriticalCount.Should().Be(0);
    }

    [Fact]
    public void AlertEvent_AllProperties_Settable()
    {
        var ts = DateTime.Now;
        var evt = new AlertEvent
        {
            Title = "CPU Alert",
            Message = "CPU > 90%",
            Severity = "Critical",
            TotalCount = 10,
            ActiveCount = 5,
            CriticalCount = 2,
            Timestamp = ts
        };

        evt.Title.Should().Be("CPU Alert");
        evt.Message.Should().Be("CPU > 90%");
        evt.Severity.Should().Be("Critical");
        evt.TotalCount.Should().Be(10);
        evt.ActiveCount.Should().Be(5);
        evt.CriticalCount.Should().Be(2);
        evt.Timestamp.Should().Be(ts);
    }

    #endregion

    #region MetricEvent

    [Fact]
    public void MetricEvent_Defaults_MetricNameIsNull()
    {
        new MetricEvent().MetricName.Should().BeNull();
    }

    [Fact]
    public void MetricEvent_Defaults_ValueIsZero()
    {
        new MetricEvent().Value.Should().Be(0);
    }

    [Fact]
    public void MetricEvent_Defaults_TotalCountIsZero()
    {
        new MetricEvent().TotalCount.Should().Be(0);
    }

    [Fact]
    public void MetricEvent_AllProperties_Settable()
    {
        var ts = DateTime.Now;
        var evt = new MetricEvent
        {
            MetricName = "CPU",
            Value = 85.5,
            TotalCount = 50,
            Timestamp = ts
        };

        evt.MetricName.Should().Be("CPU");
        evt.Value.Should().Be(85.5);
        evt.TotalCount.Should().Be(50);
        evt.Timestamp.Should().Be(ts);
    }

    #endregion

    #region RealTimeDataService Start/Stop/IsRunning

    [Fact]
    public void IsRunning_InitiallyFalse()
    {
        _service.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void Start_SetsIsRunningTrue()
    {
        _service.Start();
        _service.IsRunning.Should().BeTrue();
    }

    [Fact]
    public void Stop_SetsIsRunningFalse()
    {
        _service.Start();
        _service.Stop();
        _service.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void Start_AlreadyRunning_DoesNotThrow()
    {
        _service.Start();
        var act = () => _service.Start();
        act.Should().NotThrow();
        _service.IsRunning.Should().BeTrue();
    }

    [Fact]
    public void Stop_NotRunning_DoesNotThrow()
    {
        var act = () => _service.Stop();
        act.Should().NotThrow();
    }

    #endregion

    #region RefreshIntervalSeconds

    [Fact]
    public void RefreshIntervalSeconds_Default_Is30()
    {
        _service.RefreshIntervalSeconds.Should().Be(30);
    }

    [Fact]
    public void RefreshIntervalSeconds_CanBeSet()
    {
        _service.RefreshIntervalSeconds = 60;
        _service.RefreshIntervalSeconds.Should().Be(60);
    }

    [Fact]
    public void RefreshIntervalSeconds_ClampedToMin5()
    {
        _service.RefreshIntervalSeconds = 1;
        _service.RefreshIntervalSeconds.Should().Be(5);
    }

    [Fact]
    public void RefreshIntervalSeconds_ClampedToMax300()
    {
        _service.RefreshIntervalSeconds = 600;
        _service.RefreshIntervalSeconds.Should().Be(300);
    }

    [Fact]
    public void RefreshIntervalSeconds_SetWhileRunning_RestartsService()
    {
        _service.Start();
        _service.IsRunning.Should().BeTrue();

        _service.RefreshIntervalSeconds = 10;
        _service.RefreshIntervalSeconds.Should().Be(10);
        // Service restarts and should still be running
        _service.IsRunning.Should().BeTrue();
    }

    #endregion

    #region Observable Streams

    [Fact]
    public void DataUpdates_IsObservable()
    {
        _service.DataUpdates.Should().NotBeNull();
    }

    [Fact]
    public void AlertEvents_IsObservable()
    {
        _service.AlertEvents.Should().NotBeNull();
    }

    [Fact]
    public void MetricEvents_IsObservable()
    {
        _service.MetricEvents.Should().NotBeNull();
    }

    #endregion

    #region Status Events

    [Fact]
    public void OnStatusChanged_FiresOnStart()
    {
        string? status = null;
        _service.OnStatusChanged += s => status = s;

        _service.Start();

        status.Should().Be("Connected");
    }

    [Fact]
    public void OnStatusChanged_FiresOnStop()
    {
        string? status = null;
        _service.Start();
        _service.OnStatusChanged += s => status = s;

        _service.Stop();

        status.Should().Be("Disconnected");
    }

    #endregion

    #region PublishAlert

    [Fact]
    public void PublishAlert_EmitsAlertEvent()
    {
        AlertEvent? received = null;
        _service.AlertEvents.Subscribe(e => received = e);

        _service.PublishAlert("Test Alert", "Something happened", "Warning");

        received.Should().NotBeNull();
        received!.Title.Should().Be("Test Alert");
        received.Message.Should().Be("Something happened");
        received.Severity.Should().Be("Warning");
    }

    [Fact]
    public void PublishAlert_CriticalSeverity_EmitsEvent()
    {
        AlertEvent? received = null;
        _service.AlertEvents.Subscribe(e => received = e);

        _service.PublishAlert("Critical!", "System down", "Critical");

        received.Should().NotBeNull();
        received!.Severity.Should().Be("Critical");
    }

    #endregion

    #region Dispose

    [Fact]
    public void Dispose_StopsService()
    {
        _service.Start();
        _service.Dispose();
        _service.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        _service.Dispose();
        var act = () => _service.Dispose();
        act.Should().NotThrow();
    }

    #endregion
}
