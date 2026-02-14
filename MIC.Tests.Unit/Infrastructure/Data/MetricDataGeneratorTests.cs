using FluentAssertions;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.Data.Services;

namespace MIC.Tests.Unit.Infrastructure.Data;

/// <summary>
/// Tests for MetricDataGenerator static utility class.
/// </summary>
public class MetricDataGeneratorTests
{
    [Fact]
    public void GenerateSampleMetrics_ReturnsNonEmptyCollection()
    {
        var metrics = MetricDataGenerator.GenerateSampleMetrics().ToList();
        metrics.Should().NotBeEmpty();
    }

    [Fact]
    public void GenerateSampleMetrics_Generates90DaysPerMetricType()
    {
        var metrics = MetricDataGenerator.GenerateSampleMetrics().ToList();
        // 10 metric types × 90 days = 900 metrics
        metrics.Should().HaveCount(900);
    }

    [Fact]
    public void GenerateSampleMetrics_ContainsRevenueMetrics()
    {
        var metrics = MetricDataGenerator.GenerateSampleMetrics().ToList();
        metrics.Should().Contain(m => m.MetricName == "Revenue");
    }

    [Fact]
    public void GenerateSampleMetrics_ContainsOperatingCostsMetrics()
    {
        var metrics = MetricDataGenerator.GenerateSampleMetrics().ToList();
        metrics.Should().Contain(m => m.MetricName == "Operating Costs");
    }

    [Fact]
    public void GenerateSampleMetrics_ContainsEfficiencyMetrics()
    {
        var metrics = MetricDataGenerator.GenerateSampleMetrics().ToList();
        metrics.Should().Contain(m => m.MetricName == "Operational Efficiency");
    }

    [Fact]
    public void GenerateSampleMetrics_ContainsCustomerSatisfaction()
    {
        var metrics = MetricDataGenerator.GenerateSampleMetrics().ToList();
        metrics.Should().Contain(m => m.MetricName == "Customer Satisfaction");
    }

    [Fact]
    public void GenerateSampleMetrics_ContainsSystemUptime()
    {
        var metrics = MetricDataGenerator.GenerateSampleMetrics().ToList();
        metrics.Should().Contain(m => m.MetricName == "System Uptime");
    }

    [Fact]
    public void GenerateSampleMetrics_ContainsAvgResponseTime()
    {
        var metrics = MetricDataGenerator.GenerateSampleMetrics().ToList();
        metrics.Should().Contain(m => m.MetricName == "Avg Response Time");
    }

    [Fact]
    public void GenerateSampleMetrics_ContainsTransactionsPerSec()
    {
        var metrics = MetricDataGenerator.GenerateSampleMetrics().ToList();
        metrics.Should().Contain(m => m.MetricName == "Transactions/sec");
    }

    [Fact]
    public void GenerateSampleMetrics_ContainsActiveUsers()
    {
        var metrics = MetricDataGenerator.GenerateSampleMetrics().ToList();
        metrics.Should().Contain(m => m.MetricName == "Active Users");
    }

    [Fact]
    public void GenerateSampleMetrics_ContainsErrorRate()
    {
        var metrics = MetricDataGenerator.GenerateSampleMetrics().ToList();
        metrics.Should().Contain(m => m.MetricName == "Error Rate");
    }

    [Fact]
    public void GenerateSampleMetrics_ContainsProfitMargin()
    {
        var metrics = MetricDataGenerator.GenerateSampleMetrics().ToList();
        metrics.Should().Contain(m => m.MetricName == "Profit Margin");
    }

    [Fact]
    public void GenerateSampleMetrics_AllValuesAreNonNegative()
    {
        var metrics = MetricDataGenerator.GenerateSampleMetrics().ToList();
        metrics.Should().OnlyContain(m => m.Value >= 0);
    }

    [Fact]
    public void GenerateSampleMetrics_PercentageMetrics_AreCappedAt100()
    {
        var metrics = MetricDataGenerator.GenerateSampleMetrics().ToList();
        var percentMetrics = metrics.Where(m => m.Unit == "%").ToList();
        percentMetrics.Should().OnlyContain(m => m.Value <= 100);
    }

    [Fact]
    public void GenerateSampleMetrics_AllMetricsHaveCategory()
    {
        var metrics = MetricDataGenerator.GenerateSampleMetrics().ToList();
        metrics.Should().OnlyContain(m => !string.IsNullOrWhiteSpace(m.Category));
    }

    [Fact]
    public void GenerateSampleMetrics_AllMetricsHaveSource()
    {
        var metrics = MetricDataGenerator.GenerateSampleMetrics().ToList();
        metrics.Should().OnlyContain(m => !string.IsNullOrWhiteSpace(m.Source));
    }

    [Fact]
    public void GenerateSampleMetrics_CoversDifferentCategories()
    {
        var metrics = MetricDataGenerator.GenerateSampleMetrics().ToList();
        var categories = metrics.Select(m => m.Category).Distinct().ToList();
        categories.Should().Contain("Financial");
        categories.Should().Contain("Operations");
        categories.Should().Contain("Customer");
        categories.Should().Contain("Performance");
    }

    [Fact]
    public void GenerateSampleMetrics_CoversDifferentSeverities()
    {
        var metrics = MetricDataGenerator.GenerateSampleMetrics().ToList();
        var severities = metrics.Select(m => m.Severity).Distinct().ToList();
        severities.Should().Contain(MetricSeverity.Normal);
        // Due to randomness, Warning and Critical may or may not appear
        // but at least Normal should always be present
    }

    [Fact]
    public void GenerateSampleMetrics_ValuesAreRoundedToTwoDecimalPlaces()
    {
        var metrics = MetricDataGenerator.GenerateSampleMetrics().ToList();
        foreach (var metric in metrics.Take(50)) // Check first 50
        {
            var rounded = Math.Round(metric.Value, 2);
            metric.Value.Should().Be(rounded);
        }
    }

    #region GetMetricTargets

    [Fact]
    public void GetMetricTargets_ReturnsTenTargets()
    {
        var targets = MetricDataGenerator.GetMetricTargets();
        targets.Should().HaveCount(10);
    }

    [Fact]
    public void GetMetricTargets_ContainsRevenue()
    {
        var targets = MetricDataGenerator.GetMetricTargets();
        targets.Should().ContainKey("Revenue");
        targets["Revenue"].Should().Be(150000);
    }

    [Fact]
    public void GetMetricTargets_ContainsOperatingCosts()
    {
        var targets = MetricDataGenerator.GetMetricTargets();
        targets.Should().ContainKey("Operating Costs");
        targets["Operating Costs"].Should().Be(80000);
    }

    [Fact]
    public void GetMetricTargets_ContainsEfficiency()
    {
        var targets = MetricDataGenerator.GetMetricTargets();
        targets.Should().ContainKey("Operational Efficiency");
        targets["Operational Efficiency"].Should().Be(85);
    }

    [Fact]
    public void GetMetricTargets_ContainsCustomerSatisfaction()
    {
        var targets = MetricDataGenerator.GetMetricTargets();
        targets["Customer Satisfaction"].Should().Be(4.5);
    }

    [Fact]
    public void GetMetricTargets_ContainsSystemUptime()
    {
        var targets = MetricDataGenerator.GetMetricTargets();
        targets["System Uptime"].Should().Be(99.9);
    }

    [Fact]
    public void GetMetricTargets_ContainsAvgResponseTime()
    {
        var targets = MetricDataGenerator.GetMetricTargets();
        targets["Avg Response Time"].Should().Be(100);
    }

    [Fact]
    public void GetMetricTargets_ContainsTransactionsPerSec()
    {
        var targets = MetricDataGenerator.GetMetricTargets();
        targets["Transactions/sec"].Should().Be(1500);
    }

    [Fact]
    public void GetMetricTargets_ContainsActiveUsers()
    {
        var targets = MetricDataGenerator.GetMetricTargets();
        targets["Active Users"].Should().Be(10000);
    }

    [Fact]
    public void GetMetricTargets_ContainsErrorRate()
    {
        var targets = MetricDataGenerator.GetMetricTargets();
        targets["Error Rate"].Should().Be(0.5);
    }

    [Fact]
    public void GetMetricTargets_ContainsProfitMargin()
    {
        var targets = MetricDataGenerator.GetMetricTargets();
        targets["Profit Margin"].Should().Be(35);
    }

    #endregion
}
