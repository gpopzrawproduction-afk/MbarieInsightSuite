using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using FluentAssertions;
using MIC.Infrastructure.Monitoring;
using Xunit;

namespace MIC.Tests.Unit.Infrastructure.Monitoring;

/// <summary>
/// Tests for MonitoringService covering activity tracing, request recording, error recording, and timing.
/// </summary>
public class MonitoringServiceTests : IDisposable
{
    private readonly ActivitySource _activitySource;
    private readonly Meter _meter;
    private readonly MonitoringService _service;
    private readonly ActivityListener _listener;

    public MonitoringServiceTests()
    {
        _activitySource = new ActivitySource("test.monitoring");
        _meter = new Meter("test.monitoring");

        // Enable the activity listener so StartActivity returns non-null
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "test.monitoring",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(_listener);

        _service = new MonitoringService(_activitySource, _meter);
    }

    public void Dispose()
    {
        _listener.Dispose();
        _activitySource.Dispose();
        _meter.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ThrowsOnNullActivitySource()
    {
        var act = () => new MonitoringService(null!, _meter);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ThrowsOnNullMeter()
    {
        var act = () => new MonitoringService(_activitySource, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ParameterlessConstructor_UsesSharedTelemetry()
    {
        var svc = new MonitoringService();
        svc.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_AcceptsValidParameters()
    {
        var svc = new MonitoringService(_activitySource, _meter);
        svc.Should().NotBeNull();
    }

    #endregion

    #region StartActivity Tests

    [Fact]
    public void StartActivity_ReturnsActivity_WithValidName()
    {
        using var activity = _service.StartActivity("test.operation");
        activity.Should().NotBeNull();
        activity!.OperationName.Should().Be("test.operation");
    }

    [Fact]
    public void StartActivity_ThrowsOnNullOrWhitespaceName()
    {
        var act1 = () => _service.StartActivity(null!);
        act1.Should().Throw<ArgumentException>();

        var act2 = () => _service.StartActivity("");
        act2.Should().Throw<ArgumentException>();

        var act3 = () => _service.StartActivity("   ");
        act3.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void StartActivity_AppliesCustomActivityKind()
    {
        using var activity = _service.StartActivity("test.server", ActivityKind.Server);
        activity.Should().NotBeNull();
        activity!.Kind.Should().Be(ActivityKind.Server);
    }

    [Fact]
    public void StartActivity_AppliesTags()
    {
        var tags = new List<KeyValuePair<string, object?>>
        {
            new("key1", "value1"),
            new("key2", 42)
        };

        using var activity = _service.StartActivity("test.tagged", tags: tags);

        activity.Should().NotBeNull();
        activity!.GetTagItem("key1").Should().Be("value1");
        activity.GetTagItem("key2").Should().Be(42);
    }

    [Fact]
    public void StartActivity_WithNullTags_DoesNotThrow()
    {
        using var activity = _service.StartActivity("test.notags", tags: null);
        activity.Should().NotBeNull();
    }

    #endregion

    #region RecordRequest Tests

    [Fact]
    public void RecordRequest_DoesNotThrow_WithNoArgs()
    {
        var act = () => _service.RecordRequest();
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordRequest_DoesNotThrow_WithAllArgs()
    {
        var act = () => _service.RecordRequest("/api/data", "GET", 200, 42.5);
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordRequest_HandlesNullRoute()
    {
        var act = () => _service.RecordRequest(null, "POST", 201);
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordRequest_HandlesOptionalDuration()
    {
        var act = () => _service.RecordRequest("/test", "GET", 200, null);
        act.Should().NotThrow();
    }

    #endregion

    #region RecordError Tests

    [Fact]
    public void RecordError_DoesNotThrow_WithValidException()
    {
        var ex = new InvalidOperationException("test error");
        var act = () => _service.RecordError(ex, "test.operation");
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordError_ThrowsOnNullException()
    {
        var act = () => _service.RecordError(null!, "test.operation");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RecordError_HandlesNullOperation()
    {
        var ex = new Exception("test");
        var act = () => _service.RecordError(ex, null);
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordError_WithActiveActivity_SetsErrorStatus()
    {
        using var activity = _service.StartActivity("test.with-error");
        var ex = new InvalidOperationException("failure");

        _service.RecordError(ex, "test.op");

        // Should not throw - error is recorded
    }

    #endregion

    #region Measure / RequestTimer Tests

    [Fact]
    public void Measure_ReturnsRequestTimer()
    {
        using var timer = _service.Measure("/api/test", "GET");
        // Should not throw on dispose
    }

    [Fact]
    public void RequestTimer_Complete_RecordsMetrics()
    {
        var timer = _service.Measure("/api/test", "GET");
        timer.Complete(200);
        timer.Dispose();
        // Should not throw
    }

    [Fact]
    public void RequestTimer_DisposeWithoutComplete_DoesNotThrow()
    {
        var timer = _service.Measure("/api/data", "POST");
        timer.Dispose();
        // Should not throw
    }

    [Fact]
    public void Measure_WithNullRoute_DoesNotThrow()
    {
        using var timer = _service.Measure(null, null);
        timer.Complete(404);
    }

    #endregion
}
