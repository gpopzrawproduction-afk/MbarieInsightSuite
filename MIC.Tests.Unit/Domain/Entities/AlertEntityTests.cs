using FluentAssertions;
using MIC.Core.Domain.Entities;
using Xunit;

namespace MIC.Tests.Unit.Domain.Entities;

/// <summary>
/// Tests for IntelligenceAlert domain entity focusing on validation and state management.
/// Target: 7 tests for entity behavior
/// </summary>
public class AlertEntityTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesAlert()
    {
        // Arrange
        var alertName = "Critical System Alert";
        var description = "Database connection pool exhausted";
        var severity = AlertSeverity.Critical;
        var source = "DatabaseMonitor";

        // Act
        var alert = new IntelligenceAlert(alertName, description, severity, source);

        // Assert
        alert.AlertName.Should().Be(alertName);
        alert.Description.Should().Be(description);
        alert.Severity.Should().Be(severity);
        alert.Source.Should().Be(source);
        alert.Status.Should().Be(AlertStatus.Active);
        alert.TriggeredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        alert.Context.Should().NotBeNull();
        alert.Context.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithNullAlertName_ThrowsException()
    {
        // Act
        var act = () => new IntelligenceAlert(null!, "Description", AlertSeverity.Warning, "Source");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("alertName");
    }

    [Fact]
    public void Constructor_WithEmptyDescription_ThrowsException()
    {
        // Act
        var act = () => new IntelligenceAlert("Alert", "", AlertSeverity.Info, "Source");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("description");
    }

    [Fact]
    public void Acknowledge_ChangesStatusAndSetsTimestamp()
    {
        // Arrange
        var alert = new IntelligenceAlert("Test Alert", "Test Description", AlertSeverity.Warning, "TestSource");
        var acknowledgedBy = "TestUser";

        // Act
        alert.Acknowledge(acknowledgedBy);

        // Assert
        alert.Status.Should().Be(AlertStatus.Acknowledged);
        alert.AcknowledgedBy.Should().Be(acknowledgedBy);
        alert.AcknowledgedAt.Should().NotBeNull();
        alert.AcknowledgedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Resolve_WithValidResolution_ChangesStatusAndAddsContext()
    {
        // Arrange
        var alert = new IntelligenceAlert("Test Alert", "Test Description", AlertSeverity.Critical, "TestSource");
        var resolvedBy = "AdminUser";
        var resolution = "Fixed database connection pool configuration";

        // Act
        alert.Resolve(resolvedBy, resolution);

        // Assert
        alert.Status.Should().Be(AlertStatus.Resolved);
        alert.ResolvedBy.Should().Be(resolvedBy);
        alert.ResolvedAt.Should().NotBeNull();
        alert.ResolvedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        alert.Context.Should().ContainKey("Resolution");
        alert.Context["Resolution"].Should().Be(resolution);
    }

    [Fact]
    public void Acknowledge_OnResolvedAlert_ThrowsException()
    {
        // Arrange
        var alert = new IntelligenceAlert("Test Alert", "Test Description", AlertSeverity.Info, "TestSource");
        alert.Resolve("FirstUser", "Already resolved");

        // Act
        var act = () => alert.Acknowledge("SecondUser");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot acknowledge a resolved alert");
    }

    [Fact]
    public void UpdateMetadata_WithValidParameters_UpdatesFields()
    {
        // Arrange
        var alert = new IntelligenceAlert("Original Alert", "Original Description", AlertSeverity.Info, "OriginalSource");
        var newAlertName = "Updated Alert Name";
        var newDescription = "Updated description with more details";
        var newSeverity = AlertSeverity.Critical;
        var newSource = "UpdatedSource";
        var updatedBy = "AdminUser";

        // Act
        alert.UpdateMetadata(newAlertName, newDescription, newSeverity, newSource, updatedBy);

        // Assert
        alert.AlertName.Should().Be(newAlertName);
        alert.Description.Should().Be(newDescription);
        alert.Severity.Should().Be(newSeverity);
        alert.Source.Should().Be(newSource);
        alert.Context.Should().ContainKey("LastMetadataUpdate");
        alert.Context.Should().ContainKey("LastMetadataUpdateBy");
        alert.Context["LastMetadataUpdateBy"].Should().Be(updatedBy);
    }
}
