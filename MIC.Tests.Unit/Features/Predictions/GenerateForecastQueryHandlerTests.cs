using FluentAssertions;
using Moq;
using Xunit;
using MIC.Core.Application.Predictions.Queries.GenerateForecast;
using MIC.Core.Application.Predictions.Queries.GetMetricHistory;
using Microsoft.Extensions.Logging;
using static MIC.Core.Application.Predictions.Queries.GenerateForecast.GenerateForecastQuery;

namespace MIC.Tests.Unit.Features.Predictions;

public sealed class GenerateForecastQueryHandlerTests
{
    private readonly GenerateForecastQueryHandler _handler;
    private readonly Mock<ILogger<GenerateForecastQueryHandler>> _loggerMock;

    public GenerateForecastQueryHandlerTests()
    {
        _loggerMock = new Mock<ILogger<GenerateForecastQueryHandler>>();
        _handler = new GenerateForecastQueryHandler(_loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidData_ReturnsValidForecast()
    {
        // Arrange
        var historicalData = GenerateLinearData(30, 100, 1.5);
        var query = new GenerateForecastQuery
        {
            MetricName = "cpu_usage",
            HistoricalData = historicalData,
            PeriodsAhead = 7
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.ForecastPoints.Should().HaveCount(7);
        result.Value.ConfidenceScore.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(1);
        result.Value.StandardDeviation.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Handle_WithUpwardTrend_DetectsTrendCorrectly()
    {
        // Arrange
        var historicalData = GenerateLinearData(30, 50, 3.0); // Strong upward trend
        var query = new GenerateForecastQuery
        {
            MetricName = "memory_usage",
            HistoricalData = historicalData,
            PeriodsAhead = 7
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Trend.Should().Be(TrendDirection.Upward);
        result.Value.TrendPercentage.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Handle_WithDownwardTrend_DetectsTrendCorrectly()
    {
        // Arrange
        var historicalData = GenerateLinearData(30, 100, -2.5); // Strong downward trend
        var query = new GenerateForecastQuery
        {
            MetricName = "response_time",
            HistoricalData = historicalData,
            PeriodsAhead = 7
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Trend.Should().Be(TrendDirection.Downward);
        result.Value.TrendPercentage.Should().BeLessThan(0);
    }

    [Fact]
    public async Task Handle_WithStableData_DetectsStaleness()
    {
        // Arrange
        var historicalData = GenerateConstantData(30, 50); // Stable/flat data
        var query = new GenerateForecastQuery
        {
            MetricName = "stable_metric",
            HistoricalData = historicalData,
            PeriodsAhead = 7
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Trend.Should().Be(TrendDirection.Stable);
        result.Value.ConfidenceScore.Should().BeLessThan(0.5); // Low confidence for flat data
    }

    [Fact]
    public async Task Handle_WithTooFewDataPoints_ReturnsValidationError()
    {
        // Arrange
        var historicalData = GenerateLinearData(5, 100, 1.0); // Only 5 points, need 7+
        var query = new GenerateForecastQuery
        {
            MetricName = "cpu_usage",
            HistoricalData = historicalData,
            PeriodsAhead = 7
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Contain("insufficient_data");
    }

    [Fact]
    public async Task Handle_WithEmptyData_ReturnsValidationError()
    {
        // Arrange
        var query = new GenerateForecastQuery
        {
            MetricName = "cpu_usage",
            HistoricalData = new List<MetricDataPointDto>(),
            PeriodsAhead = 7
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Contain("insufficient_data");
    }

    [Fact]
    public async Task Handle_WithPerfectLinearData_HasHighConfidence()
    {
        // Arrange - perfectly linear data should have R² ? 1.0
        var historicalData = GeneratePerfectLinearData(20, 10, 2.0);
        var query = new GenerateForecastQuery
        {
            MetricName = "linear_metric",
            HistoricalData = historicalData,
            PeriodsAhead = 5
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.ConfidenceScore.Should().BeGreaterThan(0.9);
    }

    [Fact]
    public async Task Handle_GeneratesForecastPointsWithBounds()
    {
        // Arrange
        var historicalData = GenerateLinearData(20, 100, 1.5);
        var query = new GenerateForecastQuery
        {
            MetricName = "bounded_metric",
            HistoricalData = historicalData,
            PeriodsAhead = 5
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        foreach (var point in result.Value.ForecastPoints)
        {
            point.IsPredicted.Should().BeTrue();
            point.LowerBound.Should().BeLessThanOrEqualTo(point.Value);
            point.UpperBound.Should().BeGreaterThanOrEqualTo(point.Value);
            point.Value.Should().BeGreaterThanOrEqualTo(0); // No negative metric values
        }
    }

    [Fact]
    public async Task Handle_AnomalyRiskIncreases_WithExtremeValues()
    {
        // Arrange - data with one extreme outlier
        var historicalData = GenerateLinearData(20, 100, 1.0);
        historicalData.Add(new MetricDataPointDto
        {
            Timestamp = historicalData.Last().Timestamp.AddHours(1),
            Value = 500, // Extreme outlier
            IsPredicted = false
        });

        var query = new GenerateForecastQuery
        {
            MetricName = "outlier_metric",
            HistoricalData = historicalData.OrderBy(d => d.Timestamp).ToList(),
            PeriodsAhead = 5
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.AnomalyRiskScore.Should().BeGreaterThan(0);
    }

    private static List<MetricDataPointDto> GenerateLinearData(
        int pointCount,
        double startValue,
        double slope)
    {
        var data = new List<MetricDataPointDto>();
        var baseTime = DateTime.UtcNow.AddHours(-pointCount);

        for (int i = 0; i < pointCount; i++)
        {
            data.Add(new MetricDataPointDto
            {
                Timestamp = baseTime.AddHours(i),
                Value = Math.Max(0, startValue + (i * slope)),
                IsPredicted = false
            });
        }

        return data;
    }

    private static List<MetricDataPointDto> GeneratePerfectLinearData(
        int pointCount,
        double startValue,
        double slope)
    {
        // Generate perfectly linear data (no variance)
        var data = new List<MetricDataPointDto>();
        var baseTime = DateTime.UtcNow.AddHours(-pointCount);

        for (int i = 0; i < pointCount; i++)
        {
            data.Add(new MetricDataPointDto
            {
                Timestamp = baseTime.AddHours(i),
                Value = startValue + (i * slope),
                IsPredicted = false
            });
        }

        return data;
    }

    private static List<MetricDataPointDto> GenerateConstantData(
        int pointCount,
        double value)
    {
        var data = new List<MetricDataPointDto>();
        var baseTime = DateTime.UtcNow.AddHours(-pointCount);

        for (int i = 0; i < pointCount; i++)
        {
            data.Add(new MetricDataPointDto
            {
                Timestamp = baseTime.AddHours(i),
                Value = value,
                IsPredicted = false
            });
        }

        return data;
    }
}
