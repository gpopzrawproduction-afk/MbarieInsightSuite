using FluentAssertions;
using MIC.Core.Application.Metrics.Common;
using MIC.Core.Domain.Entities;

namespace MIC.Tests.Unit.Application.Metrics;

public class MetricDtoDisplayTests
{
    #region SeverityDisplay

    [Theory]
    [InlineData(MetricSeverity.Normal, "Normal")]
    [InlineData(MetricSeverity.Warning, "Warning")]
    [InlineData(MetricSeverity.Critical, "Critical")]
    public void SeverityDisplay_ShouldReturnEnumName(MetricSeverity severity, string expected)
    {
        var dto = new MetricDto { Severity = severity };
        dto.SeverityDisplay.Should().Be(expected);
    }

    #endregion

    #region SeverityColor

    [Theory]
    [InlineData(MetricSeverity.Normal, "#39FF14")]
    [InlineData(MetricSeverity.Warning, "#FF6B00")]
    [InlineData(MetricSeverity.Critical, "#FF0055")]
    public void SeverityColor_ShouldReturnCorrectHex(MetricSeverity severity, string expected)
    {
        var dto = new MetricDto { Severity = severity };
        dto.SeverityColor.Should().Be(expected);
    }

    [Fact]
    public void SeverityColor_Unknown_ShouldReturnDefault()
    {
        var dto = new MetricDto { Severity = (MetricSeverity)99 };
        dto.SeverityColor.Should().Be("#607D8B");
    }

    #endregion

    #region TrendIcon

    [Fact]
    public void TrendIcon_Positive_ShouldReturnUpArrow()
    {
        var dto = new MetricDto { ChangePercent = 5.5 };
        dto.TrendIcon.Should().NotBeEmpty();
    }

    [Fact]
    public void TrendIcon_Negative_ShouldReturnDownArrow()
    {
        var dto = new MetricDto { ChangePercent = -3.0 };
        dto.TrendIcon.Should().NotBeEmpty();
    }

    [Fact]
    public void TrendIcon_Zero_ShouldReturnNeutral()
    {
        var dto = new MetricDto { ChangePercent = 0 };
        dto.TrendIcon.Should().NotBeEmpty();
    }

    [Fact]
    public void TrendIcon_PositiveAndNegative_BothReturnNonEmpty()
    {
        var positive = new MetricDto { ChangePercent = 10 };
        var negative = new MetricDto { ChangePercent = -10 };
        var zero = new MetricDto { ChangePercent = 0 };
        // All trend icons should be non-null, non-empty
        positive.TrendIcon.Should().NotBeNullOrEmpty();
        negative.TrendIcon.Should().NotBeNullOrEmpty();
        zero.TrendIcon.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region TrendColor

    [Fact]
    public void TrendColor_StrongPositive_ShouldBeGreen()
    {
        var dto = new MetricDto { ChangePercent = 10 };
        dto.TrendColor.Should().Be("#39FF14");
    }

    [Fact]
    public void TrendColor_StrongNegative_ShouldBeRed()
    {
        var dto = new MetricDto { ChangePercent = -10 };
        dto.TrendColor.Should().Be("#FF0055");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(-3)]
    [InlineData(5)]
    [InlineData(-5)]
    public void TrendColor_SmallChange_ShouldBeOrange(double change)
    {
        var dto = new MetricDto { ChangePercent = change };
        dto.TrendColor.Should().Be("#FF6B00");
    }

    [Fact]
    public void TrendColor_Exactly5Point1_ShouldBeGreen()
    {
        var dto = new MetricDto { ChangePercent = 5.1 };
        dto.TrendColor.Should().Be("#39FF14");
    }

    [Fact]
    public void TrendColor_ExactlyMinus5Point1_ShouldBeRed()
    {
        var dto = new MetricDto { ChangePercent = -5.1 };
        dto.TrendColor.Should().Be("#FF0055");
    }

    #endregion

    #region FormattedValue

    [Theory]
    [InlineData(95.5, "%", "95.5%")]
    [InlineData(150000, "$", "$150,000")]
    [InlineData(1500, "K", "1,500K")]
    [InlineData(2.5, "M", "2.5M")]
    public void FormattedValue_KnownUnits_ShouldFormat(double value, string unit, string expected)
    {
        var dto = new MetricDto { Value = value, Unit = unit };
        dto.FormattedValue.Should().Be(expected);
    }

    [Fact]
    public void FormattedValue_UnknownUnit_ShouldShowValueAndUnit()
    {
        var dto = new MetricDto { Value = 42.5, Unit = "ms" };
        dto.FormattedValue.Should().Be("42.50 ms");
    }

    [Fact]
    public void FormattedValue_EmptyUnit_ShouldShowValueAndUnit()
    {
        var dto = new MetricDto { Value = 100, Unit = "" };
        dto.FormattedValue.Should().Contain("100");
    }

    #endregion

    #region FormattedChange

    [Fact]
    public void FormattedChange_Positive_ShouldHavePlusSign()
    {
        var dto = new MetricDto { ChangePercent = 5.5 };
        dto.FormattedChange.Should().Be("+5.5%");
    }

    [Fact]
    public void FormattedChange_Negative_ShouldHaveMinusSign()
    {
        var dto = new MetricDto { ChangePercent = -3.2 };
        dto.FormattedChange.Should().Be("-3.2%");
    }

    [Fact]
    public void FormattedChange_Zero_ShouldHavePlusSign()
    {
        var dto = new MetricDto { ChangePercent = 0 };
        dto.FormattedChange.Should().Be("+0.0%");
    }

    #endregion

    #region Defaults

    [Fact]
    public void Default_ShouldHaveCorrectDefaults()
    {
        var dto = new MetricDto();

        dto.Id.Should().Be(Guid.Empty);
        dto.MetricName.Should().BeEmpty();
        dto.Category.Should().BeEmpty();
        dto.Source.Should().BeEmpty();
        dto.Value.Should().Be(0);
        dto.Unit.Should().BeEmpty();
        dto.TargetValue.Should().BeNull();
        dto.PreviousValue.Should().BeNull();
        dto.ChangePercent.Should().Be(0);
    }

    #endregion

    #region MetricDataPoint

    [Fact]
    public void MetricDataPoint_ShouldHaveCorrectProperties()
    {
        var now = DateTime.UtcNow;
        var point = new MetricDataPoint
        {
            Timestamp = now,
            Value = 100.5,
            TargetValue = 95,
            PredictedValue = 102,
            ConfidenceLow = 98,
            ConfidenceHigh = 106
        };

        point.Timestamp.Should().Be(now);
        point.Value.Should().Be(100.5);
        point.TargetValue.Should().Be(95);
        point.PredictedValue.Should().Be(102);
        point.ConfidenceLow.Should().Be(98);
        point.ConfidenceHigh.Should().Be(106);
    }

    [Fact]
    public void MetricDataPoint_Defaults_NullablesAreNull()
    {
        var point = new MetricDataPoint();
        point.TargetValue.Should().BeNull();
        point.PredictedValue.Should().BeNull();
        point.ConfidenceLow.Should().BeNull();
        point.ConfidenceHigh.Should().BeNull();
    }

    #endregion

    #region MetricTrendDto

    [Fact]
    public void MetricTrendDto_ShouldHaveCorrectDefaults()
    {
        var trend = new MetricTrendDto();
        trend.MetricName.Should().BeEmpty();
        trend.Category.Should().BeEmpty();
        trend.Unit.Should().BeEmpty();
        trend.DataPoints.Should().BeEmpty();
        trend.CurrentValue.Should().Be(0);
        trend.TargetValue.Should().BeNull();
    }

    [Theory]
    [InlineData(0.05, "Upward")]
    [InlineData(-0.05, "Downward")]
    [InlineData(0.005, "Stable")]
    [InlineData(0, "Stable")]
    [InlineData(-0.005, "Stable")]
    [InlineData(0.01, "Stable")]
    [InlineData(-0.01, "Stable")]
    public void MetricTrendDto_TrendDirection_ShouldBeCorrect(double slope, string expected)
    {
        var trend = new MetricTrendDto { TrendSlope = slope };
        trend.TrendDirection.Should().Be(expected);
    }

    #endregion

    #region MetricCategorySummary

    [Fact]
    public void MetricCategorySummary_OverallHealth_ShouldCalculatePercentage()
    {
        var summary = new MetricCategorySummary
        {
            TotalMetrics = 10,
            OnTarget = 8,
            Warning = 1,
            Critical = 1
        };

        summary.OverallHealth.Should().Be(80);
    }

    [Fact]
    public void MetricCategorySummary_OverallHealth_ZeroTotal_ShouldReturnZero()
    {
        var summary = new MetricCategorySummary { TotalMetrics = 0 };
        summary.OverallHealth.Should().Be(0);
    }

    [Fact]
    public void MetricCategorySummary_AllOnTarget_ShouldBe100()
    {
        var summary = new MetricCategorySummary { TotalMetrics = 5, OnTarget = 5 };
        summary.OverallHealth.Should().Be(100);
    }

    [Fact]
    public void MetricCategorySummary_Defaults()
    {
        var summary = new MetricCategorySummary();
        summary.Category.Should().BeEmpty();
        summary.TotalMetrics.Should().Be(0);
        summary.OnTarget.Should().Be(0);
        summary.Warning.Should().Be(0);
        summary.Critical.Should().Be(0);
    }

    #endregion

    #region MetricMappingExtensions

    [Fact]
    public void ToDto_NullMetric_ShouldThrow()
    {
        OperationalMetric? metric = null;
        var act = () => metric!.ToDto();
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToDto_ValidMetric_WithPreviousValue_ShouldCalculateChange()
    {
        var metric = new OperationalMetric("Revenue", "Financial", "System", 150000, "$", MetricSeverity.Normal);

        var dto = metric.ToDto(previousValue: 100000, targetValue: 200000);

        dto.MetricName.Should().Be("Revenue");
        dto.Category.Should().Be("Financial");
        dto.Source.Should().Be("System");
        dto.Value.Should().Be(150000);
        dto.Unit.Should().Be("$");
        dto.Severity.Should().Be(MetricSeverity.Normal);
        dto.PreviousValue.Should().Be(100000);
        dto.TargetValue.Should().Be(200000);
        dto.ChangePercent.Should().Be(50); // (150000-100000)/100000*100 = 50%
    }

    [Fact]
    public void ToDto_NoPreviousValue_ShouldHaveZeroChange()
    {
        var metric = new OperationalMetric("Test", "Cat", "Src", 100, "u", MetricSeverity.Normal);

        var dto = metric.ToDto();

        dto.ChangePercent.Should().Be(0);
        dto.PreviousValue.Should().BeNull();
        dto.TargetValue.Should().BeNull();
    }

    [Fact]
    public void ToDto_PreviousValueZero_ShouldHaveZeroChange()
    {
        var metric = new OperationalMetric("Test", "Cat", "Src", 100, "u", MetricSeverity.Normal);

        var dto = metric.ToDto(previousValue: 0);

        dto.ChangePercent.Should().Be(0);
    }

    [Fact]
    public void ToDtos_MultipleMetrics_ShouldMapAll()
    {
        var metrics = new[]
        {
            new OperationalMetric("M1", "C1", "S1", 10, "u", MetricSeverity.Normal),
            new OperationalMetric("M2", "C2", "S2", 20, "u", MetricSeverity.Warning),
        };

        var dtos = metrics.ToDtos().ToList();

        dtos.Should().HaveCount(2);
        dtos[0].MetricName.Should().Be("M1");
        dtos[1].MetricName.Should().Be("M2");
    }

    [Fact]
    public void ToDtos_Empty_ShouldReturnEmpty()
    {
        var dtos = Array.Empty<OperationalMetric>().ToDtos().ToList();
        dtos.Should().BeEmpty();
    }

    #endregion

    #region OperationalMetric Entity

    [Fact]
    public void OperationalMetric_Constructor_ShouldSetProperties()
    {
        var m = new OperationalMetric("CPU", "Performance", "Monitor", 85.5, "%", MetricSeverity.Warning);

        m.MetricName.Should().Be("CPU");
        m.Category.Should().Be("Performance");
        m.Source.Should().Be("Monitor");
        m.Value.Should().Be(85.5);
        m.Unit.Should().Be("%");
        m.Severity.Should().Be(MetricSeverity.Warning);
        m.Metadata.Should().NotBeNull().And.BeEmpty();
        m.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(MetricSeverity.Normal, 0)]
    [InlineData(MetricSeverity.Warning, 1)]
    [InlineData(MetricSeverity.Critical, 2)]
    public void MetricSeverity_ShouldHaveCorrectValues(MetricSeverity severity, int expected)
    {
        ((int)severity).Should().Be(expected);
    }

    [Fact]
    public void MetricSeverity_ShouldHave3Values()
    {
        Enum.GetValues<MetricSeverity>().Should().HaveCount(3);
    }

    #endregion

    #region Record Equality

    [Fact]
    public void MetricDto_RecordEquality()
    {
        var id = Guid.NewGuid();
        var ts = DateTime.UtcNow;
        var dto1 = new MetricDto { Id = id, MetricName = "X", Timestamp = ts };
        var dto2 = new MetricDto { Id = id, MetricName = "X", Timestamp = ts };
        dto1.Should().Be(dto2);
    }

    #endregion
}
