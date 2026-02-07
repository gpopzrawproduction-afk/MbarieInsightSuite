using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using FluentAssertions;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Metrics.Queries.GetMetricTrend;
using MIC.Core.Domain.Entities;
using NSubstitute;
using Xunit;

namespace MIC.Tests.Unit.Features.Metrics;

public class GetMetricTrendQueryHandlerTests
{
    private readonly GetMetricTrendQueryHandler _sut;
    private readonly IMetricsRepository _metricsRepository;

    public GetMetricTrendQueryHandlerTests()
    {
        _metricsRepository = Substitute.For<IMetricsRepository>();
        _sut = new GetMetricTrendQueryHandler(_metricsRepository);
    }

    [Fact]
    public async Task Handle_WhenMetricsExist_ComputesTrendStatistics()
    {
        var query = new GetMetricTrendQuery
        {
            MetricName = "ResponseTime",
            Category = "Engagement",
            Days = 7
        };

        var baseTime = DateTime.UtcNow;
        var metrics = new List<OperationalMetric>
        {
            CreateMetric("ResponseTime", "Engagement", 10, baseTime.AddDays(-3)),
            CreateMetric("ResponseTime", "Engagement", 20, baseTime.AddDays(-2)),
            CreateMetric("ResponseTime", "Engagement", 30, baseTime.AddDays(-1))
        };

        _metricsRepository
            .GetFilteredMetricsAsync(
                query.Category,
                query.MetricName,
                Arg.Any<DateTime?>(),
                Arg.Any<DateTime?>(),
                Arg.Any<int?>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OperationalMetric>>(metrics));

        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsError.Should().BeFalse();
        var dto = result.Value;

        dto.MetricName.Should().Be("ResponseTime");
        dto.Category.Should().Be("Engagement");
        dto.Unit.Should().Be("ms");
        dto.DataPoints.Should().HaveCount(3);
        dto.CurrentValue.Should().Be(30);
        dto.AverageValue.Should().BeApproximately(20, 1e-6);
        dto.MinValue.Should().Be(10);
        dto.MaxValue.Should().Be(30);
        dto.TrendSlope.Should().BeApproximately(0.5, 1e-6);

        await _metricsRepository.Received(1).GetFilteredMetricsAsync(
            query.Category,
            query.MetricName,
            Arg.Is<DateTime?>(d => d.HasValue && Math.Abs((DateTime.UtcNow - d.Value).TotalDays - query.Days) <= 0.05),
            Arg.Is<DateTime?>(d => d.HasValue && Math.Abs((DateTime.UtcNow - d.Value).TotalDays) <= 0.05),
            Arg.Is<int?>(t => t == null),
            Arg.Is<bool>(latestOnly => latestOnly == false),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoMetricsExist_ReturnsNotFoundError()
    {
        var query = new GetMetricTrendQuery
        {
            MetricName = "ResponseTime",
            Category = "Engagement",
            Days = 14
        };

        _metricsRepository
            .GetFilteredMetricsAsync(
                query.Category,
                query.MetricName,
                Arg.Any<DateTime?>(),
                Arg.Any<DateTime?>(),
                Arg.Any<int?>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OperationalMetric>>(Array.Empty<OperationalMetric>()));

        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
        result.FirstError.Code.Should().Be("Metric.NotFound");

        await _metricsRepository.Received(1).GetFilteredMetricsAsync(
            query.Category,
            query.MetricName,
            Arg.Any<DateTime?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<int?>(),
            Arg.Is<bool>(latestOnly => latestOnly == false),
            Arg.Any<CancellationToken>());
    }

    private static OperationalMetric CreateMetric(string metricName, string category, double value, DateTime timestamp)
    {
        var metric = new OperationalMetric(
            metricName: metricName,
            category: category,
            source: "System",
            value: value,
            unit: "ms",
            severity: MetricSeverity.Normal);

        typeof(OperationalMetric)
            .GetProperty("Timestamp", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(metric, timestamp);

        return metric;
    }
}
