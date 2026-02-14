using FluentAssertions;
using MIC.Core.Application.Metrics.Common;
using MIC.Core.Domain.Entities;
using Xunit;

namespace MIC.Tests.Unit.Metrics;

/// <summary>
/// Tests for MetricDto, MetricTrendDto, and MetricCategorySummary computed display properties.
/// </summary>
public class MetricDtoTests
{
    #region MetricDto.FormattedValue

    [Fact]
    public void FormattedValue_Percentage()
    {
        var dto = new MetricDto { Value = 92.5, Unit = "%" };
        dto.FormattedValue.Should().Be("92.5%");
    }

    [Fact]
    public void FormattedValue_Dollar()
    {
        var dto = new MetricDto { Value = 100000, Unit = "$" };
        dto.FormattedValue.Should().Be("$100,000");
    }

    [Fact]
    public void FormattedValue_K()
    {
        var dto = new MetricDto { Value = 1500, Unit = "K" };
        dto.FormattedValue.Should().Be("1,500K");
    }

    [Fact]
    public void FormattedValue_M()
    {
        var dto = new MetricDto { Value = 2.5, Unit = "M" };
        dto.FormattedValue.Should().Be("2.5M");
    }

    [Fact]
    public void FormattedValue_OtherUnit()
    {
        var dto = new MetricDto { Value = 42.123, Unit = "items" };
        dto.FormattedValue.Should().Be("42.12 items");
    }

    #endregion

    #region MetricDto.TrendIcon

    [Fact]
    public void TrendIcon_PositiveChange()
    {
        var dto = new MetricDto { ChangePercent = 5.0 };
        dto.TrendIcon.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void TrendIcon_NegativeChange()
    {
        var dto = new MetricDto { ChangePercent = -3.0 };
        dto.TrendIcon.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void TrendIcon_ZeroChange()
    {
        var dto = new MetricDto { ChangePercent = 0 };
        dto.TrendIcon.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region MetricDto.FormattedChange

    [Fact]
    public void FormattedChange_PositiveHasPlus()
    {
        var dto = new MetricDto { ChangePercent = 5.2 };
        dto.FormattedChange.Should().StartWith("+");
        dto.FormattedChange.Should().Contain("5.2%");
    }

    [Fact]
    public void FormattedChange_NegativeHasMinus()
    {
        var dto = new MetricDto { ChangePercent = -3.1 };
        dto.FormattedChange.Should().StartWith("-");
        dto.FormattedChange.Should().Contain("3.1%");
    }

    [Fact]
    public void FormattedChange_Zero()
    {
        var dto = new MetricDto { ChangePercent = 0 };
        dto.FormattedChange.Should().Contain("0.0%");
    }

    #endregion

    #region MetricDto.SeverityDisplay and SeverityColor

    [Theory]
    [InlineData(MetricSeverity.Normal, "#39FF14")]
    [InlineData(MetricSeverity.Warning, "#FF6B00")]
    [InlineData(MetricSeverity.Critical, "#FF0055")]
    public void SeverityColor_ReturnsMappedColor(MetricSeverity severity, string expectedColor)
    {
        var dto = new MetricDto { Severity = severity };
        dto.SeverityColor.Should().Be(expectedColor);
    }

    [Fact]
    public void SeverityDisplay_ReturnsEnumName()
    {
        var dto = new MetricDto { Severity = MetricSeverity.Warning };
        dto.SeverityDisplay.Should().Be("Warning");
    }

    #endregion

    #region MetricDto.TrendColor

    [Fact]
    public void TrendColor_HighPositive()
    {
        var dto = new MetricDto { ChangePercent = 10.0 };
        dto.TrendColor.Should().Be("#39FF14");
    }

    [Fact]
    public void TrendColor_HighNegative()
    {
        var dto = new MetricDto { ChangePercent = -10.0 };
        dto.TrendColor.Should().Be("#FF0055");
    }

    [Fact]
    public void TrendColor_Stable()
    {
        var dto = new MetricDto { ChangePercent = 2.0 };
        dto.TrendColor.Should().Be("#FF6B00");
    }

    #endregion

    #region MetricDto Defaults

    [Fact]
    public void MetricDto_DefaultValues()
    {
        var dto = new MetricDto();
        dto.MetricName.Should().BeEmpty();
        dto.Category.Should().BeEmpty();
        dto.Source.Should().BeEmpty();
        dto.Unit.Should().BeEmpty();
        dto.Value.Should().Be(0);
    }

    #endregion
}

/// <summary>
/// Tests for MetricTrendDto computed properties.
/// </summary>
public class MetricTrendDtoTests
{
    [Fact]
    public void TrendDirection_Upward()
    {
        var dto = new MetricTrendDto { TrendSlope = 0.5 };
        dto.TrendDirection.Should().Be("Upward");
    }

    [Fact]
    public void TrendDirection_Downward()
    {
        var dto = new MetricTrendDto { TrendSlope = -0.5 };
        dto.TrendDirection.Should().Be("Downward");
    }

    [Fact]
    public void TrendDirection_Stable()
    {
        var dto = new MetricTrendDto { TrendSlope = 0.005 };
        dto.TrendDirection.Should().Be("Stable");
    }

    [Fact]
    public void TrendDirection_StableNegative()
    {
        var dto = new MetricTrendDto { TrendSlope = -0.005 };
        dto.TrendDirection.Should().Be("Stable");
    }

    [Fact]
    public void DefaultValues()
    {
        var dto = new MetricTrendDto();
        dto.MetricName.Should().BeEmpty();
        dto.DataPoints.Should().NotBeNull().And.BeEmpty();
    }
}

/// <summary>
/// Tests for MetricCategorySummary.
/// </summary>
public class MetricCategorySummaryTests
{
    [Fact]
    public void OverallHealth_CalculatesCorrectPercentage()
    {
        var summary = new MetricCategorySummary { TotalMetrics = 10, OnTarget = 8 };
        summary.OverallHealth.Should().Be(80);
    }

    [Fact]
    public void OverallHealth_ReturnsZero_WhenNoMetrics()
    {
        var summary = new MetricCategorySummary { TotalMetrics = 0 };
        summary.OverallHealth.Should().Be(0);
    }

    [Fact]
    public void OverallHealth_100Percent()
    {
        var summary = new MetricCategorySummary { TotalMetrics = 5, OnTarget = 5 };
        summary.OverallHealth.Should().Be(100);
    }
}
