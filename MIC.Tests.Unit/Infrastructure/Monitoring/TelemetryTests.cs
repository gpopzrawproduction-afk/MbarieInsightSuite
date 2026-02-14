using System;
using System.Collections.Generic;
using System.Diagnostics;
using FluentAssertions;
using MIC.Infrastructure.Monitoring;
using Xunit;

namespace MIC.Tests.Unit.Infrastructure.Monitoring;

/// <summary>
/// Tests for Telemetry static class covering tracing and metrics.
/// Target: 12 tests for telemetry functionality
/// </summary>
public class TelemetryTests
{
    #region Constants Tests

    [Fact]
    public void ServiceName_IsCorrect()
    {
        // Act & Assert
        Telemetry.ServiceName.Should().Be("MIC");
    }

    #endregion

    #region ActivitySource Tests

    [Fact]
    public void ActivitySource_IsNotNull()
    {
        // Act & Assert
        Telemetry.ActivitySource.Should().NotBeNull();
    }

    [Fact]
    public void ActivitySource_HasCorrectName()
    {
        // Act & Assert
        Telemetry.ActivitySource.Name.Should().Be("MIC");
    }

    [Fact]
    public void StartActivity_WithListener_ReturnsActivity()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "MIC",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        // Act
        using var activity = Telemetry.StartActivity("test-operation");

        // Assert
        activity.Should().NotBeNull();
        activity!.DisplayName.Should().Be("test-operation");
    }

    [Fact]
    public void StartActivity_WithActivityKind_SetsKind()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "MIC",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        // Act
        using var activity = Telemetry.StartActivity("test-operation", ActivityKind.Client);

        // Assert
        activity.Should().NotBeNull();
        activity!.Kind.Should().Be(ActivityKind.Client);
    }

    [Fact]
    public void StartActivity_WithoutListener_ReturnsNull()
    {
        // Act - No listener configured
        using var activity = Telemetry.StartActivity("test-operation");

        // Assert - ActivitySource returns null when no listeners
        activity.Should().BeNull();
    }

    #endregion

    #region Meter Tests

    [Fact]
    public void Meter_IsNotNull()
    {
        // Act & Assert
        Telemetry.Meter.Should().NotBeNull();
    }

    [Fact]
    public void Meter_HasCorrectName()
    {
        // Act & Assert
        Telemetry.Meter.Name.Should().Be("MIC");
    }

    #endregion

    #region ApplicationInsights Tests

    [Fact]
    public void InitializeApplicationInsights_WithNullKey_DoesNotThrow()
    {
        // Act
        var act = () => Telemetry.InitializeApplicationInsights(null);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void InitializeApplicationInsights_WithEmptyKey_DoesNotThrow()
    {
        // Act
        var act = () => Telemetry.InitializeApplicationInsights("");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void TrackEvent_WithNullProperties_DoesNotThrow()
    {
        // Act
        var act = () => Telemetry.TrackEvent("test-event", null, null);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void TrackException_WithException_DoesNotThrow()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        var act = () => Telemetry.TrackException(exception, null);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void TrackMetric_WithValue_DoesNotThrow()
    {
        // Act
        var act = () => Telemetry.TrackMetric("test-metric", 42.0, null);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void TrackPageView_WithPageName_DoesNotThrow()
    {
        // Act
        var act = () => Telemetry.TrackPageView("Dashboard");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void TrackDependency_WithParameters_DoesNotThrow()
    {
        // Act
        var act = () => Telemetry.TrackDependency(
            "HTTP",
            "api.example.com",
            "GET /api/data",
            DateTimeOffset.UtcNow,
            TimeSpan.FromMilliseconds(250),
            true);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordMetric_WithValue_DoesNotThrow()
    {
        // Act
        var act = () => Telemetry.RecordMetric("counter", 1.0);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordHistogram_WithValue_DoesNotThrow()
    {
        // Act
        var act = () => Telemetry.RecordHistogram("response-time", 150.0);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Flush_DoesNotThrow()
    {
        // Act
        var act = () => Telemetry.Flush();

        // Assert
        act.Should().NotThrow();
    }

    #endregion
}
