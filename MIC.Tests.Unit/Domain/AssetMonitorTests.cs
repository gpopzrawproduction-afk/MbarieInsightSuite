using System;
using System.Linq;
using FluentAssertions;
using MIC.Core.Domain.Entities;
using Xunit;

namespace MIC.Tests.Unit.Domain;

/// <summary>
/// Tests for AssetMonitor entity covering domain logic + events.
/// </summary>
public class AssetMonitorTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        var asset = new AssetMonitor("Server-01", "Server", "Data Center A");

        asset.AssetName.Should().Be("Server-01");
        asset.AssetType.Should().Be("Server");
        asset.Location.Should().Be("Data Center A");
        asset.Status.Should().Be(AssetStatus.Online);
        asset.HealthScore.Should().BeNull();
        asset.Specifications.Should().BeEmpty();
        asset.AssociatedMetrics.Should().BeEmpty();
        asset.LastMonitoredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Constructor_ThrowsOnNullAssetName()
    {
        var act = () => new AssetMonitor(null!, "Server", "DC");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ThrowsOnNullAssetType()
    {
        var act = () => new AssetMonitor("Server", null!, "DC");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ThrowsOnWhitespaceLocation()
    {
        var act = () => new AssetMonitor("Server", "Type", "  ");
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region UpdateStatus Tests

    [Fact]
    public void UpdateStatus_ChangesStatus()
    {
        var asset = new AssetMonitor("Server-01", "Server", "DC-A");

        asset.UpdateStatus(AssetStatus.Maintenance, "admin");

        asset.Status.Should().Be(AssetStatus.Maintenance);
    }

    [Fact]
    public void UpdateStatus_RaisesDomainEvent_WhenStatusChanges()
    {
        var asset = new AssetMonitor("Server-01", "Server", "DC-A");

        asset.UpdateStatus(AssetStatus.Offline, "admin");

        asset.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AssetStatusChangedEvent>()
            .Which.NewStatus.Should().Be(AssetStatus.Offline);
    }

    [Fact]
    public void UpdateStatus_DoesNotRaiseEvent_WhenStatusSame()
    {
        var asset = new AssetMonitor("Server-01", "Server", "DC-A");

        asset.UpdateStatus(AssetStatus.Online, "admin");

        asset.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UpdateStatus_UpdatesLastMonitoredAt()
    {
        var asset = new AssetMonitor("Server-01", "Server", "DC-A");
        var before = asset.LastMonitoredAt;

        asset.UpdateStatus(AssetStatus.Degraded, "admin");

        asset.LastMonitoredAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void UpdateStatus_ThrowsOnNullUpdatedBy()
    {
        var asset = new AssetMonitor("Server-01", "Server", "DC-A");

        var act = () => asset.UpdateStatus(AssetStatus.Offline, null!);
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region UpdateHealthScore Tests

    [Fact]
    public void UpdateHealthScore_SetsScore()
    {
        var asset = new AssetMonitor("Server-01", "Server", "DC-A");

        asset.UpdateHealthScore(85.5, "monitor");

        asset.HealthScore.Should().Be(85.5);
    }

    [Fact]
    public void UpdateHealthScore_RaisesDegradedEvent_WhenBelow50()
    {
        var asset = new AssetMonitor("Server-01", "Server", "DC-A");

        asset.UpdateHealthScore(45.0, "monitor");

        asset.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AssetHealthDegradedEvent>()
            .Which.HealthScore.Should().Be(45.0);
    }

    [Fact]
    public void UpdateHealthScore_DoesNotRaiseEvent_WhenAbove50()
    {
        var asset = new AssetMonitor("Server-01", "Server", "DC-A");

        asset.UpdateHealthScore(75.0, "monitor");

        asset.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UpdateHealthScore_AcceptsBoundaryValues()
    {
        var asset = new AssetMonitor("Server-01", "Server", "DC-A");

        asset.UpdateHealthScore(0.0, "monitor");
        asset.HealthScore.Should().Be(0.0);

        asset.UpdateHealthScore(100.0, "monitor");
        asset.HealthScore.Should().Be(100.0);
    }

    [Fact]
    public void UpdateHealthScore_ThrowsOnValueAbove100()
    {
        var asset = new AssetMonitor("Server-01", "Server", "DC-A");

        var act = () => asset.UpdateHealthScore(101.0, "monitor");
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateHealthScore_ThrowsOnNegativeValue()
    {
        var asset = new AssetMonitor("Server-01", "Server", "DC-A");

        var act = () => asset.UpdateHealthScore(-1.0, "monitor");
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateHealthScore_ThrowsOnNullUpdatedBy()
    {
        var asset = new AssetMonitor("Server-01", "Server", "DC-A");

        var act = () => asset.UpdateHealthScore(50.0, null!);
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region AddSpecification Tests

    [Fact]
    public void AddSpecification_AddsKeyValuePair()
    {
        var asset = new AssetMonitor("Server-01", "Server", "DC-A");

        asset.AddSpecification("CPU", "Intel Xeon");
        asset.AddSpecification("RAM", "64GB");

        asset.Specifications.Should().HaveCount(2);
        asset.Specifications["CPU"].Should().Be("Intel Xeon");
    }

    [Fact]
    public void AddSpecification_OverwritesExistingKey()
    {
        var asset = new AssetMonitor("Server-01", "Server", "DC-A");

        asset.AddSpecification("CPU", "Intel Xeon");
        asset.AddSpecification("CPU", "AMD EPYC");

        asset.Specifications["CPU"].Should().Be("AMD EPYC");
    }

    [Fact]
    public void AddSpecification_ThrowsOnNullKey()
    {
        var asset = new AssetMonitor("Server-01", "Server", "DC-A");

        var act = () => asset.AddSpecification(null!, "value");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddSpecification_ThrowsOnNullValue()
    {
        var asset = new AssetMonitor("Server-01", "Server", "DC-A");

        var act = () => asset.AddSpecification("key", null!);
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region AssociateMetric Tests

    [Fact]
    public void AssociateMetric_AddsMetric()
    {
        var asset = new AssetMonitor("Server-01", "Server", "DC-A");

        asset.AssociateMetric("CPU Usage");
        asset.AssociateMetric("Memory Usage");

        asset.AssociatedMetrics.Should().HaveCount(2);
        asset.AssociatedMetrics.Should().Contain("CPU Usage");
    }

    [Fact]
    public void AssociateMetric_IgnoresDuplicates()
    {
        var asset = new AssetMonitor("Server-01", "Server", "DC-A");

        asset.AssociateMetric("CPU Usage");
        asset.AssociateMetric("CPU Usage");

        asset.AssociatedMetrics.Should().HaveCount(1);
    }

    [Fact]
    public void AssociateMetric_ThrowsOnNullMetricName()
    {
        var asset = new AssetMonitor("Server-01", "Server", "DC-A");

        var act = () => asset.AssociateMetric(null!);
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Enum Tests

    [Theory]
    [InlineData(AssetStatus.Online, 0)]
    [InlineData(AssetStatus.Offline, 1)]
    [InlineData(AssetStatus.Maintenance, 2)]
    [InlineData(AssetStatus.Degraded, 3)]
    [InlineData(AssetStatus.Failed, 4)]
    public void AssetStatus_HasExpectedValues(AssetStatus status, int expected)
    {
        ((int)status).Should().Be(expected);
    }

    #endregion
}
