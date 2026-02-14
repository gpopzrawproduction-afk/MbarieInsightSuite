using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.AI.Services;
using NSubstitute;
using Xunit;

namespace MIC.Tests.Unit.Infrastructure.AI;

/// <summary>
/// Tests for PredictionService covering forecast generation, edge cases, and linear trend calculations.
/// Target: 18 tests for comprehensive prediction coverage
/// </summary>
public class PredictionServiceTests
{
    private readonly IMetricsRepository _mockMetricsRepository;
    private readonly PredictionService _sut;

    public PredictionServiceTests()
    {
        _mockMetricsRepository = Substitute.For<IMetricsRepository>();
        _sut = new PredictionService(_mockMetricsRepository);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new PredictionService(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("metricsRepository");
    }

    #endregion

    #region GenerateForecastAsync - Input Validation

    [Fact]
    public async Task GenerateForecastAsync_WithNullMetricName_ThrowsArgumentException()
    {
        // Act
        Func<Task> act = async () => await _sut.GenerateForecastAsync(null!, 7);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("metricName");
    }

    [Fact]
    public async Task GenerateForecastAsync_WithEmptyMetricName_ThrowsArgumentException()
    {
        // Act
        Func<Task> act = async () => await _sut.GenerateForecastAsync("", 7);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("metricName");
    }

    [Fact]
    public async Task GenerateForecastAsync_WithWhitespaceMetricName_ThrowsArgumentException()
    {
        // Act
        Func<Task> act = async () => await _sut.GenerateForecastAsync("   ", 7);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("metricName");
    }

    [Fact]
    public async Task GenerateForecastAsync_WithZeroDays_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.GenerateForecastAsync("test-metric", 0);

        // Assert
        result.Should().BeEmpty();
        await _mockMetricsRepository.DidNotReceive()
            .GetFilteredMetricsAsync(
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<DateTime?>(),
                Arg.Any<DateTime?>(),
                Arg.Any<int?>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateForecastAsync_WithNegativeDays_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.GenerateForecastAsync("test-metric", -5);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GenerateForecastAsync - No Historical Data

    [Fact]
    public async Task GenerateForecastAsync_WithNoHistoricalData_ReturnsEmptyList()
    {
        // Arrange
        _mockMetricsRepository.GetFilteredMetricsAsync(
            Arg.Any<string?>(),  // category
            Arg.Any<string?>(),  // metricName
            Arg.Any<DateTime?>(),  // startDate
            Arg.Any<DateTime?>(),  // endDate
            Arg.Any<int?>(),  // take
            Arg.Any<bool>(),  // latestOnly
            Arg.Any<CancellationToken>())
            .Returns(new List<OperationalMetric>());

        // Act
        var result = await _sut.GenerateForecastAsync("nonexistent-metric", 7);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateForecastAsync_WithNullHistoricalData_ReturnsEmptyList()
    {
        // Arrange
        _mockMetricsRepository.GetFilteredMetricsAsync(
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<int?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<OperationalMetric>?)null);

        // Act
        var result = await _sut.GenerateForecastAsync("null-metric", 7);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GenerateForecastAsync - Valid Forecasts

    [Fact]
    public async Task GenerateForecastAsync_WithValidData_ReturnsCorrectNumberOfPoints()
    {
        // Arrange
        var historicalMetrics = CreateLinearTrendMetrics(baseValue: 100, days: 30, dailyIncrease: 2);
        _mockMetricsRepository.GetFilteredMetricsAsync(
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<int?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(historicalMetrics);

        // Act
        var result = await _sut.GenerateForecastAsync("revenue", 7);

        // Assert
        result.Should().HaveCount(7);
    }

    [Fact]
    public async Task GenerateForecastAsync_WithLinearUpwardTrend_ProducesIncreasingForecast()
    {
        // Arrange - Create metrics with clear upward trend: 100, 102, 104, ..., 158
        var historicalMetrics = CreateLinearTrendMetrics(baseValue: 100, days: 30, dailyIncrease: 2);
        _mockMetricsRepository.GetFilteredMetricsAsync(
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<int?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(historicalMetrics);

        // Act
        var result = await _sut.GenerateForecastAsync("revenue", 7);

        // Assert
        result.Should().HaveCount(7);
        
        // Forecast should show increasing trend
        for (int i = 1; i < result.Count; i++)
        {
            result[i].Value.Should().BeGreaterThan(result[i - 1].Value,
                "because the historical data shows an upward trend");
        }

        // First forecast day should be approximately 160 (last historical was ~158, trend is +2/day)
        result[0].Value.Should().BeInRange(158, 162);
    }

    [Fact]
    public async Task GenerateForecastAsync_WithLinearDownwardTrend_ProducesDecreasingForecast()
    {
        // Arrange - Create metrics with clear downward trend
        var historicalMetrics = CreateLinearTrendMetrics(baseValue: 200, days: 30, dailyIncrease: -3);
        _mockMetricsRepository.GetFilteredMetricsAsync(
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<int?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(historicalMetrics);

        // Act
        var result = await _sut.GenerateForecastAsync("cost", 5);

        // Assert
        result.Should().HaveCount(5);
        
        // Forecast should show decreasing trend
        for (int i = 1; i < result.Count; i++)
        {
            result[i].Value.Should().BeLessThan(result[i - 1].Value,
                "because the historical data shows a downward trend");
        }
    }

    [Fact]
    public async Task GenerateForecastAsync_WithFlatTrend_ProducesFlatForecast()
    {
        // Arrange - Create metrics with flat trend (all same value)
        var historicalMetrics = CreateLinearTrendMetrics(baseValue: 150, days: 30, dailyIncrease: 0);
        _mockMetricsRepository.GetFilteredMetricsAsync(
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<int?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(historicalMetrics);

        // Act
        var result = await _sut.GenerateForecastAsync("stable-metric", 5);

        // Assert
        result.Should().HaveCount(5);
        
        // All forecast values should be approximately the same
        var expectedValue = 150.0;
        result.Should().AllSatisfy(fp => 
            fp.Value.Should().BeApproximately(expectedValue, 1.0));
    }

    [Fact]
    public async Task GenerateForecastAsync_IncludesConfidenceIntervals()
    {
        // Arrange
        var historicalMetrics = CreateLinearTrendMetrics(baseValue: 100, days: 30, dailyIncrease: 2);
        _mockMetricsRepository.GetFilteredMetricsAsync(
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<int?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(historicalMetrics);

        // Act
        var result = await _sut.GenerateForecastAsync("revenue", 7);

        // Assert
        result.Should().HaveCount(7);
        
        foreach (var point in result)
        {
            point.LowerBound.Should().BeLessThan(point.Value,
                "lower bound should be below the forecast value");
            point.UpperBound.Should().BeGreaterThan(point.Value,
                "upper bound should be above the forecast value");
        }
    }

    [Fact]
    public async Task GenerateForecastAsync_ProducesSequentialDates()
    {
        // Arrange
        var historicalMetrics = CreateLinearTrendMetrics(baseValue: 100, days: 30, dailyIncrease: 2);
        _mockMetricsRepository.GetFilteredMetricsAsync(
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<int?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(historicalMetrics);

        // Act
        var result = await _sut.GenerateForecastAsync("revenue", 7);

        // Assert
        result.Should().HaveCount(7);
        
        // Dates should be sequential (1 day apart)
        for (int i = 1; i < result.Count; i++)
        {
            var daysDifference = (result[i].Date - result[i - 1].Date).TotalDays;
            daysDifference.Should().BeApproximately(1.0, 0.1);
        }
    }

    [Fact]
    public async Task GenerateForecastAsync_LookbackPeriodAdjustsWithForecastHorizon()
    {
        // Arrange
        var historicalMetrics = CreateLinearTrendMetrics(baseValue: 100, days: 90, dailyIncrease: 1);
        DateTime? capturedStartDate = null;

        _mockMetricsRepository.GetFilteredMetricsAsync(
            Arg.Any<string?>(),  // category
            Arg.Any<string?>(),  // metricName
            Arg.Do<DateTime?>(d => { if (d.HasValue) capturedStartDate = d.Value; }),  // startDate
            Arg.Any<DateTime?>(),  // endDate
            Arg.Any<int?>(),  // take
            Arg.Any<bool>(),  // latestOnly
            Arg.Any<CancellationToken>())
            .Returns(historicalMetrics);

        // Act - Request 60-day forecast (should use 120-day lookback)
        await _sut.GenerateForecastAsync("long-term", 60);

        // Assert
        capturedStartDate.Should().NotBeNull();
        var daysDifference = (DateTime.UtcNow - capturedStartDate.Value).TotalDays;
        daysDifference.Should().BeInRange(118, 122, "lookback should be 2x forecast horizon (120 days)");
    }

    [Fact]
    public async Task GenerateForecastAsync_WithSingleDataPoint_ReturnsFlatForecast()
    {
        // Arrange
        var singleMetric = new List<OperationalMetric>
        {
            CreateMetric("metric", 100, DateTime.UtcNow.AddDays(-1))
        };
        _mockMetricsRepository.GetFilteredMetricsAsync(
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<int?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(singleMetric);

        // Act
        var result = await _sut.GenerateForecastAsync("single-point", 3);

        // Assert
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(fp => 
            fp.Value.Should().BeApproximately(100.0, 1.0));
    }

    [Fact]
    public async Task GenerateForecastAsync_RespectsCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        _mockMetricsRepository.GetFilteredMetricsAsync(
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<int?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(callInfo => 
            {
                callInfo.Arg<CancellationToken>().Should().Be(cts.Token);
                return new List<OperationalMetric>();
            });

        // Act
        await _sut.GenerateForecastAsync("metric", 7, cts.Token);

        // Assert
        await _mockMetricsRepository.Received(1).GetFilteredMetricsAsync(
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<int?>(),
            Arg.Any<bool>(),
            cts.Token);
    }

    #endregion

    #region Helper Methods

    private static List<OperationalMetric> CreateLinearTrendMetrics(double baseValue, int days, double dailyIncrease)
    {
        var metrics = new List<OperationalMetric>();
        var startDate = DateTime.UtcNow.AddDays(-days);

        for (int i = 0; i < days; i++)
        {
            var value = baseValue + (i * dailyIncrease);
            var date = startDate.AddDays(i);
            metrics.Add(CreateMetric($"metric-{i}", value, date));
        }

        return metrics;
    }

    private static OperationalMetric CreateMetric(string name, double value, DateTime timestamp)
    {
        // Use the public constructor
        var metric = new OperationalMetric(
            metricName: name,
            category: "Test",
            source: "TestSource",
            value: value,
            unit: "units",
            severity: MetricSeverity.Normal);
        
        // Set timestamp using reflection since it's private set
        var timestampProperty = typeof(OperationalMetric).GetProperty("Timestamp");
        timestampProperty?.SetValue(metric, timestamp);

        // Set Id using reflection
        var idProperty = typeof(OperationalMetric).GetProperty("Id");
        idProperty?.SetValue(metric, Guid.NewGuid());

        return metric;
    }

    #endregion
}


