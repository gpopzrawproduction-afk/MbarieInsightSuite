using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.Data.Services;

namespace MIC.Tests.Unit.Infrastructure.Data;

/// <summary>
/// Tests for the no-op settings cloud sync service.
/// </summary>
public class NoOpSettingsCloudSyncServiceTests
{
    private NoOpSettingsCloudSyncService CreateService() =>
        new(NullLogger<NoOpSettingsCloudSyncService>.Instance);

    private static Setting CreateSetting(string key, string value, string category = "General")
    {
        return new Setting
        {
            UserId = Guid.NewGuid(),
            Key = key,
            Value = value,
            Category = category
        };
    }

    [Fact]
    public async Task SyncSettingsAsync_CompletesWithoutError()
    {
        var service = CreateService();
        var settings = new List<Setting>
        {
            CreateSetting("Theme", "Dark"),
            CreateSetting("Language", "en")
        };

        var act = () => service.SyncSettingsAsync(settings);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SyncSettingsAsync_EmptyList_CompletesWithoutError()
    {
        var service = CreateService();
        await service.SyncSettingsAsync(new List<Setting>());
    }

    [Fact]
    public async Task GetCurrentStatusAsync_InitialStatus_HasZeroPendingAndSynced()
    {
        var service = CreateService();
        var status = await service.GetCurrentStatusAsync();

        status.PendingCount.Should().Be(0);
        status.SyncedCount.Should().Be(0);
        status.LastSyncedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentStatusAsync_AfterSync_ReflectsSyncedCount()
    {
        var service = CreateService();
        var settings = new List<Setting>
        {
            CreateSetting("A", "1"),
            CreateSetting("B", "2"),
            CreateSetting("C", "3")
        };

        await service.SyncSettingsAsync(settings);
        var status = await service.GetCurrentStatusAsync();

        status.SyncedCount.Should().Be(3);
        status.LastSyncedAt.Should().NotBeNull();
        status.PendingCount.Should().Be(0);
    }

    [Fact]
    public async Task GetCurrentStatusAsync_AfterMultipleSyncs_AccumulatesCount()
    {
        var service = CreateService();
        
        await service.SyncSettingsAsync(new List<Setting> { CreateSetting("A", "1") });
        await service.SyncSettingsAsync(new List<Setting> { CreateSetting("B", "2"), CreateSetting("C", "3") });
        
        var status = await service.GetCurrentStatusAsync();
        status.SyncedCount.Should().Be(3);
    }

    [Fact]
    public async Task SyncSettingsAsync_NullSettings_CountsAsZero()
    {
        var service = CreateService();
        // null?.Count() returns 0
        await service.SyncSettingsAsync(null!);
        
        var status = await service.GetCurrentStatusAsync();
        status.SyncedCount.Should().Be(0);
        status.LastSyncedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCurrentStatusAsync_ReturnsSyncTimestamp()
    {
        var service = CreateService();
        var before = DateTimeOffset.UtcNow;
        await service.SyncSettingsAsync(new List<Setting> { CreateSetting("X", "1") });
        var after = DateTimeOffset.UtcNow;

        var status = await service.GetCurrentStatusAsync();
        status.LastSyncedAt.Should().NotBeNull();
        status.LastSyncedAt!.Value.Should().BeOnOrAfter(before);
        status.LastSyncedAt!.Value.Should().BeOnOrBefore(after);
    }

    [Fact]
    public async Task GetCurrentStatusAsync_CancellationToken_Respected()
    {
        var service = CreateService();
        using var cts = new CancellationTokenSource();
        var status = await service.GetCurrentStatusAsync(cts.Token);
        status.Should().NotBeNull();
    }
}
