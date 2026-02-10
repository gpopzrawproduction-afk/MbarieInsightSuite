using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using FluentAssertions;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Metrics.Queries.GetMetrics;
using MIC.Core.Application.Metrics.Queries.GetMetricTrend;
using MIC.Core.Domain.Entities;
using Moq;
using Xunit;

namespace MIC.Tests.Unit.Application.Metrics;

/// <summary>
/// Comprehensive tests for Metrics CQRS query handlers.
/// Tests metrics retrieval, filtering, trend analysis, and statistics.
/// Target: 13 tests for metrics handler coverage
/// </summary>
public class MetricsHandlersTests
{
    private readonly Mock<IMetricsRepository> _mockRepository;

    public MetricsHandlersTests()
    {
        _mockRepository = new Mock<IMetricsRepository>();
    }

    #region GetMetricsQueryHandler Tests (7 tests)

    [Fact]
    public async Task GetMetricsQuery_WithNoFilters_ReturnsAllMetrics()
    {
        // Arrange
        var query = new GetMetricsQuery();
        var metrics = new List<OperationalMetric>
        {
            CreateMetric("CPU", "System", 75.0),
            CreateMetric("Memory", "System", 60.0),
            CreateMetric("Disk", "System", 85.0)
        };

        _mockRepository.Setup(x => x.GetFilteredMetricsAsync(null, null, null, null, 100, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        var handler = new GetMetricsQueryHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(3);
        result.Value.Should().Contain(m => m.MetricName == "CPU");
        result.Value.Should().Contain(m => m.MetricName == "Memory");
        result.Value.Should().Contain(m => m.MetricName == "Disk");
    }

    [Fact]
    public async Task GetMetricsQuery_WithCategoryFilter_ReturnsFilteredMetrics()
    {
        // Arrange
        var query = new GetMetricsQuery { Category = "System" };
        var metrics = new List<OperationalMetric>
        {
            CreateMetric("CPU", "System", 75.0),
            CreateMetric("Memory", "System", 60.0)
        };

        _mockRepository.Setup(x => x.GetFilteredMetricsAsync("System", null, null, null, 100, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        var handler = new GetMetricsQueryHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(2);
        result.Value.All(m => m.Category == "System").Should().BeTrue();
    }

    [Fact]
    public async Task GetMetricsQuery_WithMetricNameFilter_ReturnsSingleMetric()
    {
        // Arrange
        var query = new GetMetricsQuery { MetricName = "CPU", Category = "System" };
        var metrics = new List<OperationalMetric>
        {
            CreateMetric("CPU", "System", 75.0)
        };

        _mockRepository.Setup(x => x.GetFilteredMetricsAsync("System", "CPU", null, null, 100, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        var handler = new GetMetricsQueryHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(1);
        result.Value.First().MetricName.Should().Be("CPU");
    }

    [Fact]
    public async Task GetMetricsQuery_WithDateRange_ReturnsMetricsInRange()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow;
        var query = new GetMetricsQuery
        {
            StartDate = startDate,
            EndDate = endDate
        };

        var metrics = new List<OperationalMetric>
        {
            CreateMetric("CPU", "System", 75.0),
            CreateMetric("Memory", "System", 60.0)
        };

        _mockRepository.Setup(x => x.GetFilteredMetricsAsync(null, null, startDate, endDate, 100, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        var handler = new GetMetricsQueryHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMetricsQuery_WithTakeLimit_ReturnsLimitedResults()
    {
        // Arrange
        var query = new GetMetricsQuery { Take = 2 };
        var metrics = new List<OperationalMetric>
        {
            CreateMetric("CPU", "System", 75.0),
            CreateMetric("Memory", "System", 60.0)
        };

        _mockRepository.Setup(x => x.GetFilteredMetricsAsync(null, null, null, null, 2, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        var handler = new GetMetricsQueryHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMetricsQuery_WithLatestOnly_ReturnsLatestMetrics()
    {
        // Arrange
        var query = new GetMetricsQuery { LatestOnly = true };
        var metrics = new List<OperationalMetric>
        {
            CreateMetric("CPU", "System", 80.0) // Latest value
        };

        _mockRepository.Setup(x => x.GetFilteredMetricsAsync(null, null, null, null, 100, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        var handler = new GetMetricsQueryHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(1);
        result.Value.First().Value.Should().Be(80.0);
    }

    [Fact]
    public async Task GetMetricsQuery_WithNoResults_ReturnsEmptyList()
    {
        // Arrange
        var query = new GetMetricsQuery { Category = "NonExistent" };
        var metrics = new List<OperationalMetric>();

        _mockRepository.Setup(x => x.GetFilteredMetricsAsync("NonExistent", null, null, null, 100, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        var handler = new GetMetricsQueryHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeEmpty();
    }

    #endregion

    #region GetMetricTrendQueryHandler Tests (6 tests)

    [Fact]
    public async Task GetMetricTrendQuery_WithMultipleDataPoints_ReturnsCompleteTrend()
    {
        // Arrange
        var query = new GetMetricTrendQuery { MetricName = "CPU", Category = "System", Days = 7 };
        var metrics = CreateTrendMetrics("CPU", "System", new[] { 70.0, 75.0, 80.0, 85.0, 90.0 });

        _mockRepository.Setup(x => x.GetFilteredMetricsAsync(
            "System",
            "CPU",
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            null,
            false,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        var handler = new GetMetricTrendQueryHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        var trend = result.Value;
        trend.MetricName.Should().Be("CPU");
        trend.Category.Should().Be("System");
        trend.DataPoints.Should().HaveCount(5);
        trend.CurrentValue.Should().Be(90.0);
        trend.TrendSlope.Should().BeGreaterThan(0); // Upward trend
        trend.TrendDirection.Should().Be("Upward");
    }

    [Fact]
    public async Task GetMetricTrendQuery_WithNoData_ReturnsNotFoundError()
    {
        // Arrange
        var query = new GetMetricTrendQuery { MetricName = "NonExistent", Days = 7 };
        var metrics = new List<OperationalMetric>();

        _mockRepository.Setup(x => x.GetFilteredMetricsAsync(
            null,
            "NonExistent",
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            null,
            false,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        var handler = new GetMetricTrendQueryHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
        result.FirstError.Code.Should().Be("Metric.NotFound");
        result.FirstError.Description.Should().Contain("NonExistent");
    }

    [Fact]
    public async Task GetMetricTrendQuery_WithSingleDataPoint_CalculatesCorrectly()
    {
        // Arrange
        var query = new GetMetricTrendQuery { MetricName = "Memory", Category = "System", Days = 1 };
        var metrics = CreateTrendMetrics("Memory", "System", new[] { 50.0 });

        _mockRepository.Setup(x => x.GetFilteredMetricsAsync(
            "System",
            "Memory",
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            null,
            false,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        var handler = new GetMetricTrendQueryHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        var trend = result.Value;
        trend.DataPoints.Should().HaveCount(1);
        trend.CurrentValue.Should().Be(50.0);
        trend.AverageValue.Should().Be(50.0);
        trend.MinValue.Should().Be(50.0);
        trend.MaxValue.Should().Be(50.0);
        trend.TrendSlope.Should().Be(0); // No trend with single point
    }

    [Fact]
    public async Task GetMetricTrendQuery_CalculatesStatisticsCorrectly()
    {
        // Arrange
        var query = new GetMetricTrendQuery { MetricName = "CPU", Days = 5 };
        var metrics = CreateTrendMetrics("CPU", "System", new[] { 60.0, 70.0, 80.0, 90.0, 100.0 });

        _mockRepository.Setup(x => x.GetFilteredMetricsAsync(
            null,
            "CPU",
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            null,
            false,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        var handler = new GetMetricTrendQueryHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        var trend = result.Value;
        trend.CurrentValue.Should().Be(100.0);
        trend.AverageValue.Should().Be(80.0);
        trend.MinValue.Should().Be(60.0);
        trend.MaxValue.Should().Be(100.0);
    }

    [Fact]
    public async Task GetMetricTrendQuery_WithDownwardTrend_IdentifiesCorrectDirection()
    {
        // Arrange
        var query = new GetMetricTrendQuery { MetricName = "ResponseTime", Category = "Performance", Days = 7 };
        var metrics = CreateTrendMetrics("ResponseTime", "Performance", new[] { 100.0, 90.0, 80.0, 70.0, 60.0 });

        _mockRepository.Setup(x => x.GetFilteredMetricsAsync(
            "Performance",
            "ResponseTime",
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            null,
            false,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        var handler = new GetMetricTrendQueryHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        var trend = result.Value;
        trend.TrendSlope.Should().BeLessThan(0); // Downward trend
        trend.TrendDirection.Should().Be("Downward");
    }

    [Fact]
    public async Task GetMetricTrendQuery_WithStableTrend_IdentifiesStableDirection()
    {
        // Arrange
        var query = new GetMetricTrendQuery { MetricName = "Latency", Days = 5 };
        var metrics = CreateTrendMetrics("Latency", "Network", new[] { 50.0, 50.1, 49.9, 50.0, 50.1 });

        _mockRepository.Setup(x => x.GetFilteredMetricsAsync(
            null,
            "Latency",
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            null,
            false,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        var handler = new GetMetricTrendQueryHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        var trend = result.Value;
        trend.TrendDirection.Should().Be("Stable");
        Math.Abs(trend.TrendSlope).Should().BeLessThan(0.01);
    }

    #endregion

    #region Helper Methods

    private OperationalMetric CreateMetric(string name, string category, double value)
    {
        return new OperationalMetric(
            metricName: name,
            category: category,
            source: "TestSource",
            value: value,
            unit: "%",
            severity: MetricSeverity.Normal);
    }

    private List<OperationalMetric> CreateTrendMetrics(string name, string category, double[] values)
    {
        var metrics = new List<OperationalMetric>();
        var baseTime = DateTime.UtcNow.AddDays(-values.Length);

        for (var i = 0; i < values.Length; i++)
        {
            var metric = new OperationalMetric(
                metricName: name,
                category: category,
                source: "TestSource",
                value: values[i],
                unit: "%",
                severity: MetricSeverity.Normal);

            // Set timestamp via reflection to create time series
            var timestampProperty = typeof(OperationalMetric).GetProperty("Timestamp");
            timestampProperty?.SetValue(metric, baseTime.AddDays(i));

            metrics.Add(metric);
        }

        return metrics;
    }

    #endregion
}
