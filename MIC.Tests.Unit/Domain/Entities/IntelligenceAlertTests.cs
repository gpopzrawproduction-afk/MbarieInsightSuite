using FluentAssertions;
using MIC.Core.Domain.Entities;

namespace MIC.Tests.Unit.Domain.Entities;

public class IntelligenceAlertTests
{
    #region Constructor

    [Fact]
    public void Constructor_ValidArgs_ShouldSetProperties()
    {
        var alert = new IntelligenceAlert("CPU Spike", "CPU usage exceeded 95%", AlertSeverity.Critical, "Monitoring");

        alert.AlertName.Should().Be("CPU Spike");
        alert.Description.Should().Be("CPU usage exceeded 95%");
        alert.Severity.Should().Be(AlertSeverity.Critical);
        alert.Source.Should().Be("Monitoring");
        alert.Status.Should().Be(AlertStatus.Active);
        alert.TriggeredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        alert.Context.Should().NotBeNull().And.BeEmpty();
        alert.AcknowledgedAt.Should().BeNull();
        alert.AcknowledgedBy.Should().BeNull();
        alert.ResolvedAt.Should().BeNull();
        alert.ResolvedBy.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespaceAlertName_ShouldThrow(string? name)
    {
        var act = () => new IntelligenceAlert(name!, "desc", AlertSeverity.Info, "src");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespaceDescription_ShouldThrow(string? desc)
    {
        var act = () => new IntelligenceAlert("name", desc!, AlertSeverity.Info, "src");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespaceSource_ShouldThrow(string? src)
    {
        var act = () => new IntelligenceAlert("name", "desc", AlertSeverity.Info, src!);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(AlertSeverity.Info)]
    [InlineData(AlertSeverity.Warning)]
    [InlineData(AlertSeverity.Critical)]
    [InlineData(AlertSeverity.Emergency)]
    public void Constructor_AllSeverities_ShouldBeAccepted(AlertSeverity severity)
    {
        var alert = new IntelligenceAlert("Alert", "Desc", severity, "Src");
        alert.Severity.Should().Be(severity);
    }

    #endregion

    #region Acknowledge

    [Fact]
    public void Acknowledge_ValidUser_ShouldUpdateStatusAndTimestamp()
    {
        var alert = new IntelligenceAlert("Alert", "Desc", AlertSeverity.Info, "Src");

        alert.Acknowledge("admin");

        alert.Status.Should().Be(AlertStatus.Acknowledged);
        alert.AcknowledgedBy.Should().Be("admin");
        alert.AcknowledgedAt.Should().NotBeNull();
        alert.AcknowledgedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Acknowledge_ShouldAddDomainEvent()
    {
        var alert = new IntelligenceAlert("Alert", "Desc", AlertSeverity.Info, "Src");

        alert.Acknowledge("admin");

        alert.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AlertAcknowledgedEvent>()
            .Which.AcknowledgedBy.Should().Be("admin");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Acknowledge_NullOrWhitespace_ShouldThrow(string? user)
    {
        var alert = new IntelligenceAlert("Alert", "Desc", AlertSeverity.Info, "Src");
        var act = () => alert.Acknowledge(user!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Acknowledge_ResolvedAlert_ShouldThrow()
    {
        var alert = new IntelligenceAlert("Alert", "Desc", AlertSeverity.Info, "Src");
        alert.Resolve("user", "Fixed");

        var act = () => alert.Acknowledge("admin");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cannot acknowledge*resolved*");
    }

    [Fact]
    public void Acknowledge_ShouldCallMarkAsModified()
    {
        var alert = new IntelligenceAlert("Alert", "Desc", AlertSeverity.Info, "Src");
        alert.Acknowledge("admin");
        alert.ModifiedAt.Should().NotBeNull();
    }

    #endregion

    #region Resolve

    [Fact]
    public void Resolve_ValidArgs_ShouldUpdateStatusAndContext()
    {
        var alert = new IntelligenceAlert("Alert", "Desc", AlertSeverity.Critical, "Src");

        alert.Resolve("admin", "Root cause identified and fixed");

        alert.Status.Should().Be(AlertStatus.Resolved);
        alert.ResolvedBy.Should().Be("admin");
        alert.ResolvedAt.Should().NotBeNull();
        alert.Context.Should().ContainKey("Resolution");
        alert.Context["Resolution"].Should().Be("Root cause identified and fixed");
    }

    [Fact]
    public void Resolve_ShouldAddDomainEvent()
    {
        var alert = new IntelligenceAlert("Alert", "Desc", AlertSeverity.Info, "Src");

        alert.Resolve("admin", "Fixed");

        alert.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AlertResolvedEvent>()
            .Which.ResolvedBy.Should().Be("admin");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_NullOrWhitespaceUser_ShouldThrow(string? user)
    {
        var alert = new IntelligenceAlert("Alert", "Desc", AlertSeverity.Info, "Src");
        var act = () => alert.Resolve(user!, "resolution");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_NullOrWhitespaceResolution_ShouldThrow(string? resolution)
    {
        var alert = new IntelligenceAlert("Alert", "Desc", AlertSeverity.Info, "Src");
        var act = () => alert.Resolve("admin", resolution!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Resolve_ShouldCallMarkAsModified()
    {
        var alert = new IntelligenceAlert("Alert", "Desc", AlertSeverity.Info, "Src");
        alert.Resolve("admin", "Done");
        alert.ModifiedAt.Should().NotBeNull();
    }

    #endregion

    #region UpdateMetadata

    [Fact]
    public void UpdateMetadata_ValidArgs_ShouldChangeAllFields()
    {
        var alert = new IntelligenceAlert("Old Name", "Old Desc", AlertSeverity.Info, "OldSrc");

        alert.UpdateMetadata("New Name", "New Desc", AlertSeverity.Critical, "NewSrc", "admin");

        alert.AlertName.Should().Be("New Name");
        alert.Description.Should().Be("New Desc");
        alert.Severity.Should().Be(AlertSeverity.Critical);
        alert.Source.Should().Be("NewSrc");
    }

    [Fact]
    public void UpdateMetadata_ShouldAddContextData()
    {
        var alert = new IntelligenceAlert("Name", "Desc", AlertSeverity.Info, "Src");

        alert.UpdateMetadata("Name", "Desc", AlertSeverity.Info, "Src", "admin");

        alert.Context.Should().ContainKey("LastMetadataUpdate");
        alert.Context.Should().ContainKey("LastMetadataUpdateBy");
        alert.Context["LastMetadataUpdateBy"].Should().Be("admin");
    }

    [Fact]
    public void UpdateMetadata_ShouldAddDomainEvent()
    {
        var alert = new IntelligenceAlert("Name", "Desc", AlertSeverity.Info, "Src");

        alert.UpdateMetadata("Name", "Desc", AlertSeverity.Info, "Src", "admin");

        alert.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AlertMetadataUpdatedEvent>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateMetadata_NullAlertName_ShouldThrow(string? name)
    {
        var alert = new IntelligenceAlert("Name", "Desc", AlertSeverity.Info, "Src");
        var act = () => alert.UpdateMetadata(name!, "desc", AlertSeverity.Info, "src", "admin");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateMetadata_NullDescription_ShouldThrow(string? desc)
    {
        var alert = new IntelligenceAlert("Name", "Desc", AlertSeverity.Info, "Src");
        var act = () => alert.UpdateMetadata("name", desc!, AlertSeverity.Info, "src", "admin");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateMetadata_NullSource_ShouldThrow(string? src)
    {
        var alert = new IntelligenceAlert("Name", "Desc", AlertSeverity.Info, "Src");
        var act = () => alert.UpdateMetadata("name", "desc", AlertSeverity.Info, src!, "admin");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateMetadata_NullUpdatedBy_ShouldThrow(string? user)
    {
        var alert = new IntelligenceAlert("Name", "Desc", AlertSeverity.Info, "Src");
        var act = () => alert.UpdateMetadata("name", "desc", AlertSeverity.Info, "src", user!);
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region AddContextData

    [Fact]
    public void AddContextData_ValidArgs_ShouldAddToContext()
    {
        var alert = new IntelligenceAlert("Alert", "Desc", AlertSeverity.Info, "Src");

        alert.AddContextData("Key1", "Value1");
        alert.AddContextData("Key2", 42);

        alert.Context.Should().HaveCount(2);
        alert.Context["Key1"].Should().Be("Value1");
        alert.Context["Key2"].Should().Be(42);
    }

    [Fact]
    public void AddContextData_SameKey_ShouldOverwrite()
    {
        var alert = new IntelligenceAlert("Alert", "Desc", AlertSeverity.Info, "Src");

        alert.AddContextData("Key1", "Old");
        alert.AddContextData("Key1", "New");

        alert.Context["Key1"].Should().Be("New");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddContextData_NullOrWhitespaceKey_ShouldThrow(string? key)
    {
        var alert = new IntelligenceAlert("Alert", "Desc", AlertSeverity.Info, "Src");
        var act = () => alert.AddContextData(key!, "value");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddContextData_NullValue_ShouldThrow()
    {
        var alert = new IntelligenceAlert("Alert", "Desc", AlertSeverity.Info, "Src");
        var act = () => alert.AddContextData("key", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Domain Events

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAll()
    {
        var alert = new IntelligenceAlert("Alert", "Desc", AlertSeverity.Info, "Src");
        alert.Acknowledge("admin");
        alert.DomainEvents.Should().HaveCount(1);

        alert.ClearDomainEvents();
        alert.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void AlertAcknowledgedEvent_ShouldHaveCorrectProperties()
    {
        var evt = new AlertAcknowledgedEvent(Guid.NewGuid(), "Test", "admin");
        evt.AlertName.Should().Be("Test");
        evt.AcknowledgedBy.Should().Be("admin");
        evt.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AlertResolvedEvent_ShouldHaveCorrectProperties()
    {
        var evt = new AlertResolvedEvent(Guid.NewGuid(), "Test", "admin");
        evt.AlertName.Should().Be("Test");
        evt.ResolvedBy.Should().Be("admin");
        evt.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AlertMetadataUpdatedEvent_ShouldHaveCorrectProperties()
    {
        var evt = new AlertMetadataUpdatedEvent(Guid.NewGuid(), "Test", "admin");
        evt.AlertName.Should().Be("Test");
        evt.UpdatedBy.Should().Be("admin");
        evt.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region AlertSeverity Enum

    [Fact]
    public void AlertSeverity_ShouldHave4Values()
    {
        Enum.GetValues<AlertSeverity>().Should().HaveCount(4);
    }

    [Theory]
    [InlineData(AlertSeverity.Info, 0)]
    [InlineData(AlertSeverity.Warning, 1)]
    [InlineData(AlertSeverity.Critical, 2)]
    [InlineData(AlertSeverity.Emergency, 3)]
    public void AlertSeverity_ShouldHaveCorrectValues(AlertSeverity severity, int expected)
    {
        ((int)severity).Should().Be(expected);
    }

    #endregion

    #region AlertStatus Enum

    [Fact]
    public void AlertStatus_ShouldHave4Values()
    {
        Enum.GetValues<AlertStatus>().Should().HaveCount(4);
    }

    [Theory]
    [InlineData(AlertStatus.Active, 0)]
    [InlineData(AlertStatus.Acknowledged, 1)]
    [InlineData(AlertStatus.Resolved, 2)]
    [InlineData(AlertStatus.Escalated, 3)]
    public void AlertStatus_ShouldHaveCorrectValues(AlertStatus status, int expected)
    {
        ((int)status).Should().Be(expected);
    }

    #endregion
}
