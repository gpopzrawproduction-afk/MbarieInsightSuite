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
/// Expanded tests for AlertRepository focusing on complex scenarios.
/// Tests complex queries, transaction handling, concurrency, and edge cases.
/// Target: 8 tests for advanced repository coverage
/// </summary>
public class AlertRepositoryExpandedTests : IDisposable
{
    private readonly MicDbContext _context;
    private readonly AlertRepository _repository;
    private readonly string _databaseName;

    public AlertRepositoryExpandedTests()
    {
        _databaseName = $"AlertRepositoryExpanded_{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<MicDbContext>()
            .UseInMemoryDatabase(databaseName: _databaseName)
            .Options;

        _context = new MicDbContext(options);
        _repository = new AlertRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetFilteredAlerts_WithAllParametersCombined_FiltersCorrectly()
    {
        // Arrange - Create diverse alert set
        var alerts = new List<IntelligenceAlert>
        {
            CreateAlert("Critical Active", AlertSeverity.Critical, AlertStatus.Active, "Source1"),
            CreateAlert("Critical Acknowledged", AlertSeverity.Critical, AlertStatus.Acknowledged, "Source1"),
            CreateAlert("Warning Active", AlertSeverity.Warning, AlertStatus.Active, "Source2"),
            CreateAlert("Info Resolved", AlertSeverity.Info, AlertStatus.Resolved, "Source3"),
            CreateAlert("Emergency Active", AlertSeverity.Emergency, AlertStatus.Active, "Source1")
        };

        foreach (var alert in alerts)
        {
            await _repository.AddAsync(alert);
        }
        await _context.SaveChangesAsync();

        // Act - Apply multiple filters simultaneously
        var result = await _repository.GetFilteredAlertsAsync(
            severity: AlertSeverity.Critical,
            status: AlertStatus.Active,
            startDate: DateTime.UtcNow.AddHours(-2),
            endDate: DateTime.UtcNow,
            searchText: "Critical Active",
            skip: 0,
            take: 10,
            includeDeleted: false);

        // Assert
        result.Should().HaveCount(1);
        result.First().AlertName.Should().Be("Critical Active");
    }

    [Fact]
    public async Task GetFilteredAlerts_WithPagination_ReturnsCorrectPage()
    {
        // Arrange - Create 25 alerts
        for (int i = 1; i <= 25; i++)
        {
            var alert = CreateAlert($"Alert {i:D2}", AlertSeverity.Info, AlertStatus.Active, "Source");
            await _repository.AddAsync(alert);
            await Task.Delay(2); // Small delay to ensure different timestamps
        }
        await _context.SaveChangesAsync();

        // Act - Get first page (skip 0, take 10)
        var page1 = await _repository.GetFilteredAlertsAsync(
            severity: null,
            status: null,
            startDate: null,
            endDate: null,
            searchText: null,
            skip: 0,
            take: 10,
            includeDeleted: false);

        // Get second page (skip 10, take 10)
        var page2 = await _repository.GetFilteredAlertsAsync(
            severity: null,
            status: null,
            startDate: null,
            endDate: null,
            searchText: null,
            skip: 10,
            take: 10,
            includeDeleted: false);

        // Assert
        page1.Should().HaveCount(10);
        page2.Should().HaveCount(10);
        page1.Should().NotIntersectWith(page2); // Pages should not overlap
    }

    [Fact]
    public async Task GetFilteredAlerts_WithDateRangeBoundary_HandlesEdgeCases()
    {
        // Arrange - Create alerts with slight delays to ensure different timestamps
        var beforeTime = DateTime.UtcNow;

        var oldAlert = CreateAlert("Old Alert", AlertSeverity.Warning, AlertStatus.Active, "Source");
        await _repository.AddAsync(oldAlert);
        await _context.SaveChangesAsync();

        await Task.Delay(10); // Ensure time difference
        var midwayTime = DateTime.UtcNow;

        await Task.Delay(10);
        var recentAlert = CreateAlert("Recent Alert", AlertSeverity.Warning, AlertStatus.Active, "Source");
        await _repository.AddAsync(recentAlert);
        await _context.SaveChangesAsync();

        var afterTime = DateTime.UtcNow;

        // Act - Query for alerts after midway point (should only get recent alert)
        var result = await _repository.GetFilteredAlertsAsync(
            severity: null,
            status: null,
            startDate: midwayTime,
            endDate: afterTime,
            searchText: null,
            skip: 0,
            take: 10,
            includeDeleted: false);

        // Assert - Should only include recent alert
        result.Should().HaveCount(1);
        result.First().AlertName.Should().Be("Recent Alert");
    }

    [Fact]
    public async Task GetFilteredAlerts_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        var alert = CreateAlert("Test", AlertSeverity.Info, AlertStatus.Active, "Source1");
        await _repository.AddAsync(alert);
        await _context.SaveChangesAsync();

        // Act - Query with filters that won't match
        var result = await _repository.GetFilteredAlertsAsync(
            severity: AlertSeverity.Critical,
            status: AlertStatus.Resolved,
            startDate: DateTime.UtcNow.AddYears(-1),
            endDate: DateTime.UtcNow.AddYears(-1).AddDays(1),
            searchText: "NonExistentAlert",
            skip: 0,
            take: 10,
            includeDeleted: false);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ConcurrentAdd_MultipleThreads_AllAlertsAdded()
    {
        // Arrange
        var tasks = new List<Task>();
        var alertCount = 10;
        var dbOptions = new DbContextOptionsBuilder<MicDbContext>()
            .UseInMemoryDatabase(databaseName: _databaseName)
            .Options;

        // Act - Add alerts concurrently using separate contexts per task
        for (int i = 0; i < alertCount; i++)
        {
            var index = i; // Capture for closure
            tasks.Add(Task.Run(async () =>
            {
                using var taskContext = new MicDbContext(dbOptions);
                var taskRepository = new AlertRepository(taskContext);
                var alert = CreateAlert($"Concurrent Alert {index}", AlertSeverity.Info, AlertStatus.Active, "ConcurrentSource");
                await taskRepository.AddAsync(alert);
                await taskContext.SaveChangesAsync();
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - Use fresh context to verify
        var allAlerts = await _repository.GetAllAsync();
        allAlerts.Should().HaveCount(alertCount);
    }

    [Fact]
    public async Task Update_WithConcurrentModification_LastWriteWins()
    {
        // Arrange
        var alert = CreateAlert("Original", AlertSeverity.Info, AlertStatus.Active, "Source");
        await _repository.AddAsync(alert);
        await _context.SaveChangesAsync();

        // Act - Simulate concurrent updates
        var alertCopy1 = await _repository.GetByIdAsync(alert.Id);
        var alertCopy2 = await _repository.GetByIdAsync(alert.Id);

        alertCopy1!.Acknowledge("User1");
        await _repository.UpdateAsync(alertCopy1);
        await _context.SaveChangesAsync();

        alertCopy2!.Resolve("User2", "Resolved by User2");
        await _repository.UpdateAsync(alertCopy2);
        await _context.SaveChangesAsync();

        // Assert - Last write should win
        var final = await _repository.GetByIdAsync(alert.Id);
        final!.Status.Should().Be(AlertStatus.Resolved);
    }

    [Fact]
    public async Task GetUnresolvedAlerts_WithLargeDataset_PerformsEfficiently()
    {
        // Arrange - Create 100 alerts (mix of resolved and active)
        for (int i = 0; i < 100; i++)
        {
            var alert = CreateAlert($"Alert {i}", AlertSeverity.Info, i % 3 == 0 ? AlertStatus.Resolved : AlertStatus.Active, "Source");
            await _repository.AddAsync(alert);
        }
        await _context.SaveChangesAsync();

        // Act
        var startTime = DateTime.UtcNow;
        var active = await _repository.GetFilteredAlertsAsync(
            severity: null,
            status: AlertStatus.Active,
            startDate: null,
            endDate: null,
            searchText: null,
            skip: 0,
            take: 100,
            includeDeleted: false);
        var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

        // Assert - 34 resolved (i % 3 == 0: 0,3,6...99), so 66 active
        active.Should().HaveCount(66);
        duration.Should().BeLessThan(1000); // Should complete in less than 1 second
    }

    [Fact]
    public async Task SoftDelete_ThenQuery_ExcludesDeletedAlerts()
    {
        // Arrange
        var alert1 = CreateAlert("Active Alert", AlertSeverity.Warning, AlertStatus.Active, "Source");
        var alert2 = CreateAlert("To Be Deleted", AlertSeverity.Warning, AlertStatus.Active, "Source");
        await _repository.AddAsync(alert1);
        await _repository.AddAsync(alert2);
        await _context.SaveChangesAsync();

        // Act - Soft delete alert2
        alert2.MarkAsDeleted("TestUser");
        await _repository.UpdateAsync(alert2);
        await _context.SaveChangesAsync();

        // Query for all alerts
        var allAlerts = await _repository.GetAllAsync();

        // Assert - Should only return non-deleted alerts
        allAlerts.Should().HaveCount(1);
        allAlerts.First().AlertName.Should().Be("Active Alert");
    }

    #region Helper Methods

    private IntelligenceAlert CreateAlert(
        string alertName,
        AlertSeverity severity,
        AlertStatus status,
        string source)
    {
        var alert = new IntelligenceAlert(alertName, "Test Description", severity, source);

        // Set status if not default
        if (status == AlertStatus.Acknowledged)
        {
            alert.Acknowledge("TestUser");
        }
        else if (status == AlertStatus.Resolved)
        {
            alert.Resolve("TestUser", "Resolved in test setup");
        }

        return alert;
    }

    #endregion
}
