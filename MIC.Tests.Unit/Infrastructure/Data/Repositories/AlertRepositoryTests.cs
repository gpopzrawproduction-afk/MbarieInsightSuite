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
/// Comprehensive tests for AlertRepository.
/// Tests CRUD operations, filtering, and specialized query methods.
/// Target: 20 tests for maximum repository coverage
/// </summary>
public class AlertRepositoryTests : IDisposable
{
    private readonly MicDbContext _context;
    private readonly AlertRepository _repository;

    public AlertRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<MicDbContext>()
            .UseInMemoryDatabase(databaseName: $"AlertRepositoryTest_{Guid.NewGuid()}")
            .Options;

        _context = new MicDbContext(options);
        _repository = new AlertRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region GetByIdAsync Tests (2 tests)

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsAlert()
    {
        // Arrange
        var alert = new IntelligenceAlert("Test Alert", "Test Description", AlertSeverity.Critical, "TestSource");
        await _repository.AddAsync(alert);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(alert.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(alert.Id);
        result.AlertName.Should().Be("Test Alert");
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

    #endregion

    #region GetAllAsync Tests (2 tests)

    [Fact]
    public async Task GetAllAsync_WithMultipleAlerts_ReturnsAllAlerts()
    {
        // Arrange
        var alert1 = new IntelligenceAlert("Alert 1", "Description 1", AlertSeverity.Warning, "Source1");
        var alert2 = new IntelligenceAlert("Alert 2", "Description 2", AlertSeverity.Critical, "Source2");
        var alert3 = new IntelligenceAlert("Alert 3", "Description 3", AlertSeverity.Info, "Source3");

        await _repository.AddAsync(alert1);
        await _repository.AddAsync(alert2);
        await _repository.AddAsync(alert3);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetAllAsync();

        // Assert
        results.Should().HaveCount(3);
        results.Should().Contain(a => a.AlertName == "Alert 1");
        results.Should().Contain(a => a.AlertName == "Alert 2");
        results.Should().Contain(a => a.AlertName == "Alert 3");
    }

    [Fact]
    public async Task GetAllAsync_WithNoAlerts_ReturnsEmptyList()
    {
        // Act
        var results = await _repository.GetAllAsync();

        // Assert
        results.Should().BeEmpty();
    }

    #endregion

    #region AddAsync Tests (2 tests)

    [Fact]
    public async Task AddAsync_WithValidAlert_AddsToDatabase()
    {
        // Arrange
        var alert = new IntelligenceAlert("New Alert", "New Description", AlertSeverity.Emergency, "NewSource");

        // Act
        await _repository.AddAsync(alert);
        await _context.SaveChangesAsync();

        // Assert
        var savedAlert = await _context.Alerts.FindAsync(alert.Id);
        savedAlert.Should().NotBeNull();
        savedAlert!.AlertName.Should().Be("New Alert");
        savedAlert.Severity.Should().Be(AlertSeverity.Emergency);
    }

    [Fact]
    public async Task AddAsync_WithMultipleAlerts_AddsAll()
    {
        // Arrange
        var alert1 = new IntelligenceAlert("Alert A", "Description A", AlertSeverity.Info, "SourceA");
        var alert2 = new IntelligenceAlert("Alert B", "Description B", AlertSeverity.Warning, "SourceB");

        // Act
        await _repository.AddAsync(alert1);
        await _repository.AddAsync(alert2);
        await _context.SaveChangesAsync();

        // Assert
        var count = await _context.Alerts.CountAsync();
        count.Should().Be(2);
    }

    #endregion

    #region UpdateAsync Tests (2 tests)

    [Fact]
    public async Task UpdateAsync_WithModifiedAlert_UpdatesDatabase()
    {
        // Arrange
        var alert = new IntelligenceAlert("Original", "Original Description", AlertSeverity.Info, "OriginalSource");
        await _repository.AddAsync(alert);
        await _context.SaveChangesAsync();

        // Modify
        alert.UpdateMetadata("Updated", "Updated Description", AlertSeverity.Critical, "UpdatedSource", "TestUser");

        // Act
        await _repository.UpdateAsync(alert);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.Alerts.FindAsync(alert.Id);
        updated.Should().NotBeNull();
        updated!.AlertName.Should().Be("Updated");
        updated.Severity.Should().Be(AlertSeverity.Critical);
    }

    [Fact]
    public async Task UpdateAsync_WithAcknowledgedAlert_PreservesState()
    {
        // Arrange
        var alert = new IntelligenceAlert("Test", "Test", AlertSeverity.Warning, "Test");
        await _repository.AddAsync(alert);
        await _context.SaveChangesAsync();

        alert.Acknowledge("TestUser");

        // Act
        await _repository.UpdateAsync(alert);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.Alerts.FindAsync(alert.Id);
        updated!.Status.Should().Be(AlertStatus.Acknowledged);
        updated.AcknowledgedBy.Should().Be("TestUser");
    }

    #endregion

    #region DeleteAsync Tests (2 tests)

    [Fact]
    public async Task DeleteAsync_WithExistingAlert_RemovesFromDatabase()
    {
        // Arrange
        var alert = new IntelligenceAlert("ToDelete", "Description", AlertSeverity.Info, "Source");
        await _repository.AddAsync(alert);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(alert);
        await _context.SaveChangesAsync();

        // Assert
        var deleted = await _context.Alerts.FindAsync(alert.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithMultipleDeletes_RemovesAll()
    {
        // Arrange
        var alert1 = new IntelligenceAlert("Delete1", "Desc1", AlertSeverity.Info, "Source1");
        var alert2 = new IntelligenceAlert("Delete2", "Desc2", AlertSeverity.Warning, "Source2");
        await _repository.AddAsync(alert1);
        await _repository.AddAsync(alert2);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(alert1);
        await _repository.DeleteAsync(alert2);
        await _context.SaveChangesAsync();

        // Assert
        var count = await _context.Alerts.CountAsync();
        count.Should().Be(0);
    }

    #endregion

    #region GetActiveAlertsAsync Tests (2 tests)

    [Fact]
    public async Task GetActiveAlertsAsync_WithActiveAlerts_ReturnsOnlyActive()
    {
        // Arrange
        var active1 = new IntelligenceAlert("Active1", "Desc", AlertSeverity.Critical, "Source");
        var active2 = new IntelligenceAlert("Active2", "Desc", AlertSeverity.Warning, "Source");
        var acknowledged = new IntelligenceAlert("Ack", "Desc", AlertSeverity.Info, "Source");
        acknowledged.Acknowledge("User");

        await _repository.AddAsync(active1);
        await _repository.AddAsync(active2);
        await _repository.AddAsync(acknowledged);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetActiveAlertsAsync();

        // Assert
        results.Should().HaveCount(2);
        results.All(a => a.Status == AlertStatus.Active).Should().BeTrue();
    }

    [Fact]
    public async Task GetActiveAlertsAsync_OrdersByTriggeredAtDescending()
    {
        // Arrange - Add alerts with slight delays to ensure ordering
        var old = new IntelligenceAlert("Old", "Desc", AlertSeverity.Info, "Source");
        await _repository.AddAsync(old);
        await _context.SaveChangesAsync();

        await Task.Delay(10);
        var recent = new IntelligenceAlert("Recent", "Desc", AlertSeverity.Critical, "Source");
        await _repository.AddAsync(recent);
        await _context.SaveChangesAsync();

        // Act
        var results = (await _repository.GetActiveAlertsAsync()).ToList();

        // Assert
        results.Should().HaveCount(2);
        results.First().AlertName.Should().Be("Recent");
    }

    #endregion

    #region GetAlertsBySeverityAsync Tests (2 tests)

    [Fact]
    public async Task GetAlertsBySeverityAsync_WithMatchingSeverity_ReturnsFiltered()
    {
        // Arrange
        var critical1 = new IntelligenceAlert("Critical1", "Desc", AlertSeverity.Critical, "Source");
        var critical2 = new IntelligenceAlert("Critical2", "Desc", AlertSeverity.Critical, "Source");
        var warning = new IntelligenceAlert("Warning", "Desc", AlertSeverity.Warning, "Source");

        await _repository.AddAsync(critical1);
        await _repository.AddAsync(critical2);
        await _repository.AddAsync(warning);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetAlertsBySeverityAsync(AlertSeverity.Critical);

        // Assert
        results.Should().HaveCount(2);
        results.All(a => a.Severity == AlertSeverity.Critical).Should().BeTrue();
    }

    [Fact]
    public async Task GetAlertsBySeverityAsync_WithNoMatches_ReturnsEmpty()
    {
        // Arrange
        var warning = new IntelligenceAlert("Warning", "Desc", AlertSeverity.Warning, "Source");
        await _repository.AddAsync(warning);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetAlertsBySeverityAsync(AlertSeverity.Emergency);

        // Assert
        results.Should().BeEmpty();
    }

    #endregion

    #region GetFilteredAlertsAsync Tests (4 tests)

    [Fact]
    public async Task GetFilteredAlertsAsync_WithSeverityFilter_ReturnsMatching()
    {
        // Arrange
        await SeedTestAlerts();

        // Act
        var results = await _repository.GetFilteredAlertsAsync(severity: AlertSeverity.Critical);

        // Assert
        results.Should().HaveCountGreaterThan(0);
        results.All(a => a.Severity == AlertSeverity.Critical).Should().BeTrue();
    }

    [Fact]
    public async Task GetFilteredAlertsAsync_WithSearchText_ReturnsMatching()
    {
        // Arrange
        await SeedTestAlerts();

        // Act
        var results = await _repository.GetFilteredAlertsAsync(searchText: "Database");

        // Assert
        results.Should().HaveCountGreaterThan(0);
        results.Any(a => a.AlertName.Contains("Database") || a.Description.Contains("Database")).Should().BeTrue();
    }

    [Fact]
    public async Task GetFilteredAlertsAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        await SeedTestAlerts();

        // Act
        var results = await _repository.GetFilteredAlertsAsync(take: 2, skip: 1);

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetFilteredAlertsAsync_WithDateRange_ReturnsMatching()
    {
        // Arrange
        await SeedTestAlerts();
        var now = DateTime.UtcNow;

        // Act
        var results = await _repository.GetFilteredAlertsAsync(
            startDate: now.AddDays(-1),
            endDate: now.AddDays(1));

        // Assert
        results.Should().NotBeEmpty();
    }

    #endregion

    #region GetAlertCountsBySeverityAsync Tests (2 tests)

    [Fact]
    public async Task GetAlertCountsBySeverityAsync_WithMixedSeverities_ReturnsCorrectCounts()
    {
        // Arrange
        var critical1 = new IntelligenceAlert("C1", "Desc", AlertSeverity.Critical, "Source");
        var critical2 = new IntelligenceAlert("C2", "Desc", AlertSeverity.Critical, "Source");
        var warning = new IntelligenceAlert("W1", "Desc", AlertSeverity.Warning, "Source");

        await _repository.AddAsync(critical1);
        await _repository.AddAsync(critical2);
        await _repository.AddAsync(warning);
        await _context.SaveChangesAsync();

        // Act
        var counts = await _repository.GetAlertCountsBySeverityAsync();

        // Assert
        counts.Should().ContainKey(AlertSeverity.Critical);
        counts[AlertSeverity.Critical].Should().Be(2);
        counts[AlertSeverity.Warning].Should().Be(1);
    }

    [Fact]
    public async Task GetAlertCountsBySeverityAsync_ExcludesResolvedAlerts()
    {
        // Arrange
        var active = new IntelligenceAlert("Active", "Desc", AlertSeverity.Critical, "Source");
        var resolved = new IntelligenceAlert("Resolved", "Desc", AlertSeverity.Critical, "Source");
        resolved.Resolve("User", "Fixed");

        await _repository.AddAsync(active);
        await _repository.AddAsync(resolved);
        await _context.SaveChangesAsync();

        // Act
        var counts = await _repository.GetAlertCountsBySeverityAsync();

        // Assert
        counts[AlertSeverity.Critical].Should().Be(1);
    }

    #endregion

    #region Helper Methods

    private async Task SeedTestAlerts()
    {
        var alerts = new[]
        {
            new IntelligenceAlert("Database Connection", "DB connection failed", AlertSeverity.Critical, "Database"),
            new IntelligenceAlert("API Timeout", "API response timeout", AlertSeverity.Warning, "API"),
            new IntelligenceAlert("Memory Usage", "High memory usage", AlertSeverity.Info, "System"),
            new IntelligenceAlert("Disk Space", "Low disk space", AlertSeverity.Critical, "System"),
        };

        foreach (var alert in alerts)
        {
            await _repository.AddAsync(alert);
        }
        await _context.SaveChangesAsync();
    }

    #endregion
}
