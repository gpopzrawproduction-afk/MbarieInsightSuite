using System.Linq;
using FluentAssertions;
using MIC.Infrastructure.Data.Services;
using Xunit;

namespace MIC.Tests.Unit.Infrastructure.Data.Services;

/// <summary>
/// Tests for MetricDataGenerator static class.
/// </summary>
public class MetricDataGeneratorTests
{
    [Fact]
    public void GenerateSampleMetrics_ReturnsMetrics()
    {
        var metrics = MetricDataGenerator.GenerateSampleMetrics().ToList();

        metrics.Should().NotBeEmpty();
    }

    [Fact]
    public void GenerateSampleMetrics_Returns90DaysOf10Metrics()
    {
        var metrics = MetricDataGenerator.GenerateSampleMetrics().ToList();

        // 10 metric types x 90 days = 900
        metrics.Should().HaveCount(900);
    }

    [Fact]
    public void GenerateSampleMetrics_ContainsExpectedMetricNames()
    {
        var metrics = MetricDataGenerator.GenerateSampleMetrics().ToList();
        var names = metrics.Select(m => m.MetricName).Distinct().ToList();

        names.Should().Contain("Revenue");
        names.Should().Contain("Operating Costs");
        names.Should().Contain("Operational Efficiency");
        names.Should().Contain("Customer Satisfaction");
        names.Should().Contain("System Uptime");
        names.Should().Contain("Avg Response Time");
        names.Should().Contain("Transactions/sec");
        names.Should().Contain("Active Users");
        names.Should().Contain("Error Rate");
        names.Should().Contain("Profit Margin");
    }

    [Fact]
    public void GenerateSampleMetrics_HasCorrectCategories()
    {
        var metrics = MetricDataGenerator.GenerateSampleMetrics().ToList();
        var categories = metrics.Select(m => m.Category).Distinct().ToList();

        categories.Should().Contain("Financial");
        categories.Should().Contain("Operations");
        categories.Should().Contain("Performance");
        categories.Should().Contain("Customer");
    }

    [Fact]
    public void GenerateSampleMetrics_AllValuesAreNonNegative()
    {
        var metrics = MetricDataGenerator.GenerateSampleMetrics().ToList();

        metrics.Should().OnlyContain(m => m.Value >= 0);
    }

    [Fact]
    public void GenerateSampleMetrics_PercentageMetricsAreCapped()
    {
        var metrics = MetricDataGenerator.GenerateSampleMetrics()
            .Where(m => m.Unit == "%")
            .ToList();

        metrics.Should().OnlyContain(m => m.Value <= 100);
    }

    [Fact]
    public void GetMetricTargets_ReturnsAllTargets()
    {
        var targets = MetricDataGenerator.GetMetricTargets();

        targets.Should().HaveCount(10);
        targets.Should().ContainKey("Revenue");
        targets.Should().ContainKey("Error Rate");
        targets.Should().ContainKey("Profit Margin");
    }

    [Fact]
    public void GetMetricTargets_TargetsArePositive()
    {
        var targets = MetricDataGenerator.GetMetricTargets();

        targets.Values.Should().OnlyContain(v => v > 0);
    }

    [Fact]
    public void GetMetricTargets_ReturnsExpectedValues()
    {
        var targets = MetricDataGenerator.GetMetricTargets();

        targets["Revenue"].Should().Be(150000);
        targets["System Uptime"].Should().Be(99.9);
        targets["Error Rate"].Should().Be(0.5);
    }

    [Fact]
    public void GenerateSampleMetrics_EachMetricHas90DataPoints()
    {
        var metrics = MetricDataGenerator.GenerateSampleMetrics().ToList();
        var grouped = metrics.GroupBy(m => m.MetricName).ToList();

        grouped.Should().HaveCount(10);
        grouped.Should().OnlyContain(g => g.Count() == 90);
    }
}
