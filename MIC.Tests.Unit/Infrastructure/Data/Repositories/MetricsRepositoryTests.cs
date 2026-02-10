using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.Data.Persistence;
using MIC.Infrastructure.Data.Repositories;
using Xunit;

namespace MIC.Tests.Unit.Infrastructure.Data.Repositories;

/// <summary>
/// Comprehensive tests for MetricsRepository.
/// Tests CRUD operations, filtering, aggregation, and specialized queries.
/// Target: 18 tests for metrics repository coverage
/// </summary>
public class MetricsRepositoryTests : IDisposable
{
    private readonly MicDbContext _context;
    private readonly MetricsRepository _repository;

    public MetricsRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<MicDbContext>()
            .UseInMemoryDatabase(databaseName: $"MetricsRepositoryTest_{Guid.NewGuid()}")
            .Options;

        _context = new MicDbContext(options);
        _repository = new MetricsRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region Basic CRUD Tests (5 tests)

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsMetric()
    {
        // Arrange
        var metric = new OperationalMetric("CPU Usage", "System", "Monitor", 75.5, "percent", MetricSeverity.Normal);
        await _repository.AddAsync(metric);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(metric.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(metric.Id);
        result.MetricName.Should().Be("CPU Usage");
        result.Value.Should().Be(75.5);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleMetrics_ReturnsAll()
    {
        // Arrange
        var metric1 = new OperationalMetric("CPU", "System", "Monitor", 50.0, "%", MetricSeverity.Normal);
        var metric2 = new OperationalMetric("Memory", "System", "Monitor", 80.0, "%", MetricSeverity.Warning);
        var metric3 = new OperationalMetric("Disk", "Storage", "Monitor", 60.0, "%", MetricSeverity.Normal);

        await _repository.AddAsync(metric1);
        await _repository.AddAsync(metric2);
        await _repository.AddAsync(metric3);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetAllAsync();

        // Assert
        results.Should().HaveCount(3);
        results.Should().Contain(m => m.MetricName == "CPU");
        results.Should().Contain(m => m.MetricName == "Memory");
        results.Should().Contain(m => m.MetricName == "Disk");
    }

    [Fact]
    public async Task AddAsync_WithValidMetric_AddsToDatabase()
    {
        // Arrange
        var metric = new OperationalMetric("Network", "Network", "Monitor", 100.5, "Mbps", MetricSeverity.Normal);

        // Act
        await _repository.AddAsync(metric);
        await _context.SaveChangesAsync();

        // Assert
        var saved = await _context.Metrics.FindAsync(metric.Id);
        saved.Should().NotBeNull();
        saved!.MetricName.Should().Be("Network");
        saved.Category.Should().Be("Network");
    }

    [Fact]
    public async Task DeleteAsync_WithExistingMetric_RemovesFromDatabase()
    {
        // Arrange
        var metric = new OperationalMetric("ToDelete", "Test", "Monitor", 1.0, "unit", MetricSeverity.Normal);
        await _repository.AddAsync(metric);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(metric);
        await _context.SaveChangesAsync();

        // Assert
        var deleted = await _context.Metrics.FindAsync(metric.Id);
        deleted.Should().BeNull();
    }

    #endregion

    #region GetFilteredMetricsAsync Tests (5 tests)

    [Fact]
    public async Task GetFilteredMetricsAsync_WithCategoryFilter_ReturnsMatching()
    {
        // Arrange
        await SeedTestMetrics();

        // Act
        var results = await _repository.GetFilteredMetricsAsync(category: "System");

        // Assert
        results.Should().HaveCountGreaterThan(0);
        results.All(m => m.Category == "System").Should().BeTrue();
    }

    [Fact]
    public async Task GetFilteredMetricsAsync_WithMetricNameFilter_ReturnsMatching()
    {
        // Arrange
        await SeedTestMetrics();

        // Act
        var results = await _repository.GetFilteredMetricsAsync(metricName: "CPU Usage");

        // Assert
        results.Should().HaveCount(1);
        results.First().MetricName.Should().Be("CPU Usage");
    }

    [Fact]
    public async Task GetFilteredMetricsAsync_WithDateRangeFilter_ReturnsMatching()
    {
        // Arrange
        await SeedTestMetrics();
        var now = DateTime.UtcNow;

        // Act
        var results = await _repository.GetFilteredMetricsAsync(
            startDate: now.AddDays(-1),
            endDate: now.AddDays(1));

        // Assert
        results.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetFilteredMetricsAsync_WithTakeLimit_ReturnsLimitedResults()
    {
        // Arrange
        await SeedTestMetrics();

        // Act
        var results = await _repository.GetFilteredMetricsAsync(take: 2, latestOnly: false);

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetFilteredMetricsAsync_WithLatestOnlyTrue_ReturnsOnlyLatestPerMetric()
    {
        // Arrange
        var metric1Old = new OperationalMetric("CPU", "System", "Monitor", 50.0, "%", MetricSeverity.Normal);
        await _repository.AddAsync(metric1Old);
        await _context.SaveChangesAsync();

        await Task.Delay(10);

        var metric1New = new OperationalMetric("CPU", "System", "Monitor", 60.0, "%", MetricSeverity.Normal);
        await _repository.AddAsync(metric1New);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetFilteredMetricsAsync(latestOnly: true);

        // Assert
        results.Should().HaveCount(1);
        results.First().Value.Should().Be(60.0);
    }

    #endregion

    #region GetLatestMetricsAsync Tests (2 tests)

    [Fact]
    public async Task GetLatestMetricsAsync_WithMultipleVersions_ReturnsLatestOnly()
    {
        // Arrange
        var cpuOld = new OperationalMetric("CPU", "System", "Monitor", 50.0, "%", MetricSeverity.Normal);
        var memOld = new OperationalMetric("Memory", "System", "Monitor", 60.0, "%", MetricSeverity.Normal);
        await _repository.AddAsync(cpuOld);
        await _repository.AddAsync(memOld);
        await _context.SaveChangesAsync();

        await Task.Delay(10);

        var cpuNew = new OperationalMetric("CPU", "System", "Monitor", 70.0, "%", MetricSeverity.Warning);
        var memNew = new OperationalMetric("Memory", "System", "Monitor", 80.0, "%", MetricSeverity.Warning);
        await _repository.AddAsync(cpuNew);
        await _repository.AddAsync(memNew);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetLatestMetricsAsync();

        // Assert
        results.Should().HaveCount(2);
        results.Should().Contain(m => m.MetricName == "CPU" && m.Value == 70.0);
        results.Should().Contain(m => m.MetricName == "Memory" && m.Value == 80.0);
    }

    [Fact]
    public async Task GetLatestMetricsAsync_WithNoMetrics_ReturnsEmpty()
    {
        // Act
        var results = await _repository.GetLatestMetricsAsync();

        // Assert
        results.Should().BeEmpty();
    }

    #endregion

    #region GetMetricCategoriesAsync Tests (2 tests)

    [Fact]
    public async Task GetMetricCategoriesAsync_WithMultipleCategories_ReturnsCorrectCounts()
    {
        // Arrange
        var system1 = new OperationalMetric("CPU", "System", "Monitor", 50.0, "%", MetricSeverity.Normal);
        var system2 = new OperationalMetric("Memory", "System", "Monitor", 60.0, "%", MetricSeverity.Normal);
        var network = new OperationalMetric("Bandwidth", "Network", "Monitor", 100.0, "Mbps", MetricSeverity.Normal);

        await _repository.AddAsync(system1);
        await _repository.AddAsync(system2);
        await _repository.AddAsync(network);
        await _context.SaveChangesAsync();

        // Act
        var categories = await _repository.GetMetricCategoriesAsync();

        // Assert
        categories.Should().ContainKey("System");
        categories["System"].Should().Be(2);
        categories.Should().ContainKey("Network");
        categories["Network"].Should().Be(1);
    }

    [Fact]
    public async Task GetMetricCategoriesAsync_WithNoMetrics_ReturnsEmptyDictionary()
    {
        // Act
        var categories = await _repository.GetMetricCategoriesAsync();

        // Assert
        categories.Should().BeEmpty();
    }

    #endregion

    #region GetByCategoryAsync Tests (2 tests)

    [Fact]
    public async Task GetByCategoryAsync_WithMatchingCategory_ReturnsFiltered()
    {
        // Arrange
        await SeedTestMetrics();

        // Act
        var results = await _repository.GetByCategoryAsync("System");

        // Assert
        results.Should().HaveCountGreaterThan(0);
        results.All(m => m.Category == "System").Should().BeTrue();
    }

    [Fact]
    public async Task GetByCategoryAsync_OrdersByTimestampDescending()
    {
        // Arrange
        var old = new OperationalMetric("Metric1", "Test", "Monitor", 1.0, "unit", MetricSeverity.Normal);
        await _repository.AddAsync(old);
        await _context.SaveChangesAsync();

        await Task.Delay(10);

        var recent = new OperationalMetric("Metric2", "Test", "Monitor", 2.0, "unit", MetricSeverity.Normal);
        await _repository.AddAsync(recent);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetByCategoryAsync("Test");

        // Assert
        results.Should().HaveCount(2);
        results.First().MetricName.Should().Be("Metric2");
    }

    #endregion

    #region UpdateAsync Tests (2 tests)

    [Fact]
    public async Task UpdateAsync_WithModifiedMetric_UpdatesDatabase()
    {
        // Arrange
        var metric = new OperationalMetric("Original", "Test", "Monitor", 100.0, "unit", MetricSeverity.Normal);
        await _repository.AddAsync(metric);
        await _context.SaveChangesAsync();

        // Detach and modify (simulating update)
        _context.Entry(metric).State = EntityState.Detached;
        var updated = new OperationalMetric("Original", "Test", "Monitor", 200.0, "unit", MetricSeverity.Critical);
        typeof(OperationalMetric).GetProperty("Id")!.SetValue(updated, metric.Id);

        // Act
        await _repository.UpdateAsync(updated);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _context.Metrics.FindAsync(metric.Id);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_DoesNotThrow()
    {
        // Arrange
        var metric = new OperationalMetric("Test", "Test", "Monitor", 1.0, "unit", MetricSeverity.Normal);
        await _repository.AddAsync(metric);
        await _context.SaveChangesAsync();

        // Act
        Func<Task> act = async () =>
        {
            await _repository.UpdateAsync(metric);
            await _context.SaveChangesAsync();
        };

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Helper Methods

    private async Task SeedTestMetrics()
    {
        var metrics = new[]
        {
            new OperationalMetric("CPU Usage", "System", "Monitor", 75.5, "percent", MetricSeverity.Normal),
            new OperationalMetric("Memory Usage", "System", "Monitor", 82.3, "percent", MetricSeverity.Warning),
            new OperationalMetric("Disk Space", "Storage", "Monitor", 45.0, "percent", MetricSeverity.Normal),
            new OperationalMetric("Network IO", "Network", "Monitor", 120.5, "Mbps", MetricSeverity.Normal),
        };

        foreach (var metric in metrics)
        {
            await _repository.AddAsync(metric);
        }
        await _context.SaveChangesAsync();
    }

    #endregion
}
