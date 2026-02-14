using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using FluentAssertions;
using MediatR;
using MIC.Core.Application.Metrics.Common;
using MIC.Core.Application.Metrics.Queries.GetMetrics;
using MIC.Core.Application.Metrics.Queries.GetMetricTrend;
using MIC.Infrastructure.AI.Plugins;
using NSubstitute;
using Xunit;

namespace MIC.Tests.Unit.Plugins;

/// <summary>
/// Tests for MetricsPlugin covering metric querying, trend analysis, and comparison via Semantic Kernel functions.
/// </summary>
public class MetricsPluginTests
{
    private readonly IMediator _mediator;
    private readonly MetricsPlugin _plugin;

    public MetricsPluginTests()
    {
        _mediator = Substitute.For<IMediator>();
        _plugin = new MetricsPlugin(_mediator);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ThrowsOnNullMediator()
    {
        var act = () => new MetricsPlugin(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_AcceptsValidMediator()
    {
        var plugin = new MetricsPlugin(_mediator);
        plugin.Should().NotBeNull();
    }

    #endregion

    #region GetMetricsAsync Tests

    [Fact]
    public async Task GetMetricsAsync_ReturnsFormattedMetrics()
    {
        var metrics = new List<MetricDto>
        {
            new MetricDto { MetricName = "Revenue", Value = 100000, Unit = "$", Category = "Financial", ChangePercent = 5.2 },
            new MetricDto { MetricName = "Efficiency", Value = 92.5, Unit = "%", Category = "Operations", ChangePercent = -1.3 }
        };
        ErrorOr<IReadOnlyList<MetricDto>> result = metrics.AsReadOnly();
        _mediator.Send(Arg.Any<GetMetricsQuery>(), Arg.Any<CancellationToken>())
            .Returns(result);

        var output = await _plugin.GetMetricsAsync();

        output.Should().Contain("Revenue");
        output.Should().Contain("Efficiency");
    }

    [Fact]
    public async Task GetMetricsAsync_WithCategoryFilter_PassesCategoryToQuery()
    {
        var metrics = new List<MetricDto>
        {
            new MetricDto { MetricName = "Revenue", Value = 50000, Unit = "$", Category = "Financial", ChangePercent = 2.0 }
        };
        ErrorOr<IReadOnlyList<MetricDto>> result = metrics.AsReadOnly();
        _mediator.Send(Arg.Any<GetMetricsQuery>(), Arg.Any<CancellationToken>())
            .Returns(result);

        var output = await _plugin.GetMetricsAsync(category: "Financial");

        output.Should().Contain("Revenue");
        await _mediator.Received(1).Send(
            Arg.Is<GetMetricsQuery>(q => q.Category == "Financial"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMetricsAsync_ReturnsNoDataMessage_WhenEmpty()
    {
        var metrics = new List<MetricDto>();
        ErrorOr<IReadOnlyList<MetricDto>> result = metrics.AsReadOnly();
        _mediator.Send(Arg.Any<GetMetricsQuery>(), Arg.Any<CancellationToken>())
            .Returns(result);

        var output = await _plugin.GetMetricsAsync();

        output.Should().Contain("No metrics");
    }

    [Fact]
    public async Task GetMetricsAsync_ReturnsErrorMessage_WhenMediatorFails()
    {
        ErrorOr<IReadOnlyList<MetricDto>> errorResult = Error.Failure("Metrics.Failed", "Database error");
        _mediator.Send(Arg.Any<GetMetricsQuery>(), Arg.Any<CancellationToken>())
            .Returns(errorResult);

        var output = await _plugin.GetMetricsAsync();

        output.Should().Contain("Unable to retrieve metrics");
    }

    [Fact]
    public async Task GetMetricsAsync_NullCategory_TreatedAsNoFilter()
    {
        var metrics = new List<MetricDto>
        {
            new MetricDto { MetricName = "Metric1", Value = 10, Unit = "%", Category = "Ops", ChangePercent = 0 }
        };
        ErrorOr<IReadOnlyList<MetricDto>> result = metrics.AsReadOnly();
        _mediator.Send(Arg.Any<GetMetricsQuery>(), Arg.Any<CancellationToken>())
            .Returns(result);

        var output = await _plugin.GetMetricsAsync(category: null);

        await _mediator.Received(1).Send(
            Arg.Is<GetMetricsQuery>(q => q.Category == null),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetMetricTrendAsync Tests

    [Fact]
    public async Task GetMetricTrendAsync_ReturnsFormattedTrend()
    {
        var trend = new MetricTrendDto
        {
            MetricName = "Revenue",
            CurrentValue = 100000,
            Unit = "$",
            AverageValue = 95000,
            MinValue = 80000,
            MaxValue = 120000,
            TrendSlope = 0.5,
            DataPoints = new List<MetricDataPoint>
            {
                new MetricDataPoint { Value = 90000, Timestamp = DateTime.UtcNow.AddDays(-7) },
                new MetricDataPoint { Value = 100000, Timestamp = DateTime.UtcNow }
            }
        };
        ErrorOr<MetricTrendDto> result = trend;
        _mediator.Send(Arg.Any<GetMetricTrendQuery>(), Arg.Any<CancellationToken>())
            .Returns(result);

        var output = await _plugin.GetMetricTrendAsync("Revenue", 30);

        output.Should().Contain("Revenue");
        output.Should().Contain("Current Value");
        output.Should().Contain("Data Points: 2");
    }

    [Fact]
    public async Task GetMetricTrendAsync_ClampsDaysToValidRange()
    {
        var trend = new MetricTrendDto
        {
            MetricName = "Test",
            DataPoints = new List<MetricDataPoint>()
        };
        ErrorOr<MetricTrendDto> result = trend;
        _mediator.Send(Arg.Any<GetMetricTrendQuery>(), Arg.Any<CancellationToken>())
            .Returns(result);

        await _plugin.GetMetricTrendAsync("Test", 200);

        await _mediator.Received(1).Send(
            Arg.Is<GetMetricTrendQuery>(q => q.Days == 90),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMetricTrendAsync_ReturnsError_WhenNameEmpty()
    {
        var output = await _plugin.GetMetricTrendAsync("", 30);

        output.Should().Contain("specify a metric name");
    }

    [Fact]
    public async Task GetMetricTrendAsync_ReturnsErrorMessage_WhenMediatorFails()
    {
        ErrorOr<MetricTrendDto> errorResult = Error.Failure("Trend.Failed", "Metric not found");
        _mediator.Send(Arg.Any<GetMetricTrendQuery>(), Arg.Any<CancellationToken>())
            .Returns(errorResult);

        var output = await _plugin.GetMetricTrendAsync("Unknown", 30);

        output.Should().Contain("Unable to retrieve trend");
    }

    #endregion

    #region CompareMetricsAsync Tests

    [Fact]
    public async Task CompareMetricsAsync_ReturnsBothMetrics()
    {
        var trend1 = new MetricTrendDto
        {
            MetricName = "Revenue",
            CurrentValue = 100000,
            TrendSlope = 0.5,
            DataPoints = new List<MetricDataPoint>()
        };
        var trend2 = new MetricTrendDto
        {
            MetricName = "Expenses",
            CurrentValue = 80000,
            TrendSlope = -0.2,
            DataPoints = new List<MetricDataPoint>()
        };

        _mediator.Send(Arg.Is<GetMetricTrendQuery>(q => q.MetricName == "Revenue"), Arg.Any<CancellationToken>())
            .Returns((ErrorOr<MetricTrendDto>)trend1);
        _mediator.Send(Arg.Is<GetMetricTrendQuery>(q => q.MetricName == "Expenses"), Arg.Any<CancellationToken>())
            .Returns((ErrorOr<MetricTrendDto>)trend2);

        var output = await _plugin.CompareMetricsAsync("Revenue", "Expenses", 30);

        output.Should().Contain("Revenue");
        output.Should().Contain("Expenses");
        output.Should().Contain("Comparison");
    }

    [Fact]
    public async Task CompareMetricsAsync_ReturnsError_WhenOneMetricFails()
    {
        var trend1 = new MetricTrendDto
        {
            MetricName = "Revenue",
            CurrentValue = 100000,
            TrendSlope = 0.5,
            DataPoints = new List<MetricDataPoint>()
        };
        ErrorOr<MetricTrendDto> errorResult = Error.Failure("Trend.Failed", "Not found");

        _mediator.Send(Arg.Is<GetMetricTrendQuery>(q => q.MetricName == "Revenue"), Arg.Any<CancellationToken>())
            .Returns((ErrorOr<MetricTrendDto>)trend1);
        _mediator.Send(Arg.Is<GetMetricTrendQuery>(q => q.MetricName == "Missing"), Arg.Any<CancellationToken>())
            .Returns(errorResult);

        var output = await _plugin.CompareMetricsAsync("Revenue", "Missing", 30);

        output.Should().Contain("Unable to retrieve");
    }

    #endregion
}
