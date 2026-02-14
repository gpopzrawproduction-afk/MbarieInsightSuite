using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.Data.Services;
using NSubstitute;
using Xunit;

namespace MIC.Tests.Unit.Infrastructure.Data.Services;

/// <summary>
/// Tests for NoOpSettingsCloudSyncService covering no-op sync behavior.
/// </summary>
public class NoOpSettingsCloudSyncServiceTests
{
    private readonly ILogger<NoOpSettingsCloudSyncService> _logger;
    private readonly NoOpSettingsCloudSyncService _service;

    public NoOpSettingsCloudSyncServiceTests()
    {
        _logger = Substitute.For<ILogger<NoOpSettingsCloudSyncService>>();
        _service = new NoOpSettingsCloudSyncService(_logger);
    }

    [Fact]
    public async Task SyncSettingsAsync_CompletesSuccessfully()
    {
        var settings = new List<Setting> { new() { Key = "Theme", Value = "Dark" } };

        var act = async () => await _service.SyncSettingsAsync(settings);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SyncSettingsAsync_HandlesEmptyCollection()
    {
        var act = async () => await _service.SyncSettingsAsync(Enumerable.Empty<Setting>());

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SyncSettingsAsync_HandlesNull()
    {
        var act = async () => await _service.SyncSettingsAsync(null!);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetCurrentStatusAsync_ReturnsZeroPendingCount()
    {
        var status = await _service.GetCurrentStatusAsync();

        status.PendingCount.Should().Be(0);
    }

    [Fact]
    public async Task GetCurrentStatusAsync_ReturnsZeroSyncedCount_Initially()
    {
        var status = await _service.GetCurrentStatusAsync();

        status.SyncedCount.Should().Be(0);
        status.LastSyncedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentStatusAsync_TracksSyncedCount_AfterSync()
    {
        var settings = new List<Setting>
        {
            new() { Key = "A", Value = "1" },
            new() { Key = "B", Value = "2" }
        };

        await _service.SyncSettingsAsync(settings);
        var status = await _service.GetCurrentStatusAsync();

        status.SyncedCount.Should().Be(2);
        status.LastSyncedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCurrentStatusAsync_AccumulatesSyncedCount()
    {
        await _service.SyncSettingsAsync(new List<Setting> { new() { Key = "A", Value = "1" } });
        await _service.SyncSettingsAsync(new List<Setting> { new() { Key = "B", Value = "2" }, new() { Key = "C", Value = "3" } });

        var status = await _service.GetCurrentStatusAsync();
        status.SyncedCount.Should().Be(3);
    }
}
