using FluentAssertions;
using MIC.Core.Application.Alerts.Common;
using MIC.Core.Domain.Entities;

namespace MIC.Tests.Unit.Application.Alerts;

public class AlertDtoDisplayTests
{
    #region SeverityDisplay

    [Theory]
    [InlineData(AlertSeverity.Info, "Info")]
    [InlineData(AlertSeverity.Warning, "Warning")]
    [InlineData(AlertSeverity.Critical, "Critical")]
    [InlineData(AlertSeverity.Emergency, "Emergency")]
    public void SeverityDisplay_ShouldReturnEnumName(AlertSeverity severity, string expected)
    {
        var dto = new AlertDto { Severity = severity };
        dto.SeverityDisplay.Should().Be(expected);
    }

    #endregion

    #region StatusDisplay

    [Theory]
    [InlineData(AlertStatus.Active, "Active")]
    [InlineData(AlertStatus.Acknowledged, "Acknowledged")]
    [InlineData(AlertStatus.Resolved, "Resolved")]
    [InlineData(AlertStatus.Escalated, "Escalated")]
    public void StatusDisplay_ShouldReturnEnumName(AlertStatus status, string expected)
    {
        var dto = new AlertDto { Status = status };
        dto.StatusDisplay.Should().Be(expected);
    }

    #endregion

    #region SeverityColor

    [Theory]
    [InlineData(AlertSeverity.Info, "#00E5FF")]
    [InlineData(AlertSeverity.Warning, "#FF6B00")]
    [InlineData(AlertSeverity.Critical, "#FF0055")]
    [InlineData(AlertSeverity.Emergency, "#FF0055")]
    public void SeverityColor_ShouldReturnCorrectHex(AlertSeverity severity, string expected)
    {
        var dto = new AlertDto { Severity = severity };
        dto.SeverityColor.Should().Be(expected);
    }

    [Fact]
    public void SeverityColor_UnknownValue_ShouldReturnDefault()
    {
        var dto = new AlertDto { Severity = (AlertSeverity)99 };
        dto.SeverityColor.Should().Be("#607D8B");
    }

    #endregion

    #region StatusColor

    [Theory]
    [InlineData(AlertStatus.Active, "#FF0055")]
    [InlineData(AlertStatus.Acknowledged, "#FF6B00")]
    [InlineData(AlertStatus.Resolved, "#39FF14")]
    [InlineData(AlertStatus.Escalated, "#BF40FF")]
    public void StatusColor_ShouldReturnCorrectHex(AlertStatus status, string expected)
    {
        var dto = new AlertDto { Status = status };
        dto.StatusColor.Should().Be(expected);
    }

    [Fact]
    public void StatusColor_UnknownValue_ShouldReturnDefault()
    {
        var dto = new AlertDto { Status = (AlertStatus)99 };
        dto.StatusColor.Should().Be("#607D8B");
    }

    #endregion

    #region Default values

    [Fact]
    public void Defaults_ShouldBeCorrect()
    {
        var dto = new AlertDto();

        dto.Id.Should().Be(Guid.Empty);
        dto.AlertName.Should().BeEmpty();
        dto.Description.Should().BeEmpty();
        dto.Source.Should().BeEmpty();
        dto.AcknowledgedAt.Should().BeNull();
        dto.AcknowledgedBy.Should().BeNull();
        dto.ResolvedAt.Should().BeNull();
        dto.ResolvedBy.Should().BeNull();
        dto.Resolution.Should().BeNull();
    }

    #endregion

    #region ToDto mapping

    [Fact]
    public void ToDto_NullAlert_ShouldThrow()
    {
        IntelligenceAlert? alert = null;
        var act = () => alert!.ToDto();
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToDto_ValidAlert_ShouldMapAllProperties()
    {
        var alert = new IntelligenceAlert("Test Alert", "Description here", AlertSeverity.Warning, "TestSource");

        var dto = alert.ToDto();

        dto.Id.Should().Be(alert.Id);
        dto.AlertName.Should().Be("Test Alert");
        dto.Description.Should().Be("Description here");
        dto.Severity.Should().Be(AlertSeverity.Warning);
        dto.Status.Should().Be(AlertStatus.Active);
        dto.Source.Should().Be("TestSource");
        dto.TriggeredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        dto.AcknowledgedAt.Should().BeNull();
        dto.AcknowledgedBy.Should().BeNull();
        dto.ResolvedAt.Should().BeNull();
        dto.ResolvedBy.Should().BeNull();
        dto.Resolution.Should().BeNull();
    }

    [Fact]
    public void ToDto_ResolvedAlert_ShouldMapResolution()
    {
        var alert = new IntelligenceAlert("Alert", "Desc", AlertSeverity.Critical, "Src");
        alert.Resolve("admin", "Fixed the issue");

        var dto = alert.ToDto();

        dto.Status.Should().Be(AlertStatus.Resolved);
        dto.ResolvedBy.Should().Be("admin");
        dto.ResolvedAt.Should().NotBeNull();
        dto.Resolution.Should().Be("Fixed the issue");
    }

    [Fact]
    public void ToDto_AcknowledgedAlert_ShouldMapAcknowledgement()
    {
        var alert = new IntelligenceAlert("Alert", "Desc", AlertSeverity.Info, "Src");
        alert.Acknowledge("user1");

        var dto = alert.ToDto();

        dto.Status.Should().Be(AlertStatus.Acknowledged);
        dto.AcknowledgedBy.Should().Be("user1");
        dto.AcknowledgedAt.Should().NotBeNull();
    }

    [Fact]
    public void ToDtos_ShouldMapMultiple()
    {
        var alerts = new[]
        {
            new IntelligenceAlert("A1", "D1", AlertSeverity.Info, "S1"),
            new IntelligenceAlert("A2", "D2", AlertSeverity.Warning, "S2"),
            new IntelligenceAlert("A3", "D3", AlertSeverity.Critical, "S3")
        };

        var dtos = alerts.ToDtos().ToList();

        dtos.Should().HaveCount(3);
        dtos[0].AlertName.Should().Be("A1");
        dtos[1].Severity.Should().Be(AlertSeverity.Warning);
        dtos[2].Source.Should().Be("S3");
    }

    [Fact]
    public void ToDtos_Empty_ShouldReturnEmpty()
    {
        var dtos = Array.Empty<IntelligenceAlert>().ToDtos().ToList();
        dtos.Should().BeEmpty();
    }

    #endregion

    #region Record Equality

    [Fact]
    public void AlertDto_RecordEquality_SameValues_ShouldBeEqual()
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var dto1 = new AlertDto { Id = id, AlertName = "Test", CreatedAt = now };
        var dto2 = new AlertDto { Id = id, AlertName = "Test", CreatedAt = now };

        dto1.Should().Be(dto2);
    }

    [Fact]
    public void AlertDto_RecordEquality_DifferentValues_ShouldNotBeEqual()
    {
        var dto1 = new AlertDto { AlertName = "A" };
        var dto2 = new AlertDto { AlertName = "B" };

        dto1.Should().NotBe(dto2);
    }

    #endregion
}
