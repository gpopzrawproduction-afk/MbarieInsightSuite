using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.Data.Persistence;
using MIC.Infrastructure.Data.Services;
using MIC.Tests.Integration.Infrastructure;
using Xunit;

namespace MIC.Tests.Integration.Features.Settings;

public sealed class SettingsServiceIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task SetSettingAsync_PersistsValueForCurrentUser()
    {
        var user = await SeedUserAsync("settingsuser", "Password123!");

        using var scope = CreateScope();
        var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();

        await settingsService.SetSettingAsync("ui", "theme", "dark");

        var storedValue = await settingsService.GetSettingAsync("ui", "theme", "light");
        storedValue.Should().Be("dark");

        var record = await QueryDbContextAsync(async context =>
            await context.Settings
                .Where(s => s.UserId == user.Id && s.Category == "ui" && s.Key == "theme")
                .SingleAsync());

        record.Value.Should().Be("dark");
        record.SyncStatus.Should().Be(Setting.SyncStatuses.Pending);
        record.LastModified.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task SetSettingAsync_WhenValueChanges_CreatesHistoryRecord()
    {
        var user = await SeedUserAsync("settingsuser2", "Password123!");

        using var scope = CreateScope();
        var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();

        await settingsService.SetSettingAsync("notifications", "enabled", true);
        await settingsService.SetSettingAsync("notifications", "enabled", false);

        var setting = await QueryDbContextAsync(async context =>
            await context.Settings
                .Include(s => s.History)
                .Where(s => s.UserId == user.Id && s.Category == "notifications" && s.Key == "enabled")
                .SingleAsync());

        setting.Value.Should().Be("false");

        setting.History.Should().ContainSingle();
        var entry = setting.History.Single();
        entry.OldValue.Should().Be("true");
        entry.NewValue.Should().Be("false");
        entry.ChangedBy.Should().Be(user.Email);
    }

    [Fact]
    public async Task SyncWithCloudAsync_PushesPendingSettingsAndUpdatesStatus()
    {
        var user = await SeedUserAsync("cloudsyncuser", "Password123!");

        var recordingSync = new RecordingCloudSyncService
        {
            StatusToReturn = new SettingsSyncStatus(0, 2, DateTimeOffset.UtcNow)
        };

        var tempFile = Path.Combine(Path.GetTempPath(), $"mic-settings-{Guid.NewGuid():N}.json");

        using var scope = CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<MicDbContext>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SettingsService>>();
        var session = scope.ServiceProvider.GetRequiredService<ISessionService>();

        var settingsService = new SettingsService(factory, logger, recordingSync, session, tempFile);

        try
        {
            await settingsService.SetSettingAsync("ui", "theme", "dark");
            await settingsService.SetSettingAsync("ui", "density", "compact");

            var pendingBefore = await QueryDbContextAsync(async context =>
                await context.Settings
                    .Where(s => s.UserId == user.Id && s.SyncStatus == Setting.SyncStatuses.Pending)
                    .ToListAsync());

            pendingBefore.Should().HaveCount(2);

            await settingsService.SyncWithCloudAsync();

            recordingSync.SyncCalls.Should().ContainSingle();
            var callSnapshot = recordingSync.SyncCalls.Single();
            callSnapshot.Should().BeEquivalentTo(new[]
            {
                new SettingSyncCall("ui", "theme", Setting.SyncStatuses.Pending),
                new SettingSyncCall("ui", "density", Setting.SyncStatuses.Pending)
            });

            var recordsAfter = await QueryDbContextAsync(async context =>
                await context.Settings
                    .Where(s => s.UserId == user.Id)
                    .ToListAsync());

            recordsAfter.Should().HaveCount(2);
            recordsAfter.Should().OnlyContain(r => r.SyncStatus == Setting.SyncStatuses.Synced);

            var status = await settingsService.GetSyncStatusAsync();
            status.PendingCount.Should().Be(0);
            status.SyncedCount.Should().Be(2);
            status.LastSyncedAt.Should().Be(recordingSync.StatusToReturn.LastSyncedAt);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    private sealed class RecordingCloudSyncService : ISettingsCloudSyncService
    {
        public List<IEnumerable<SettingSyncCall>> SyncCalls { get; } = new();

        public SettingsSyncStatus StatusToReturn { get; set; } = new(0, 0, null);

        public Task SyncSettingsAsync(IEnumerable<Setting> settings, CancellationToken cancellationToken = default)
        {
            var snapshot = settings
                .Select(s => new SettingSyncCall(s.Category, s.Key, s.SyncStatus))
                .ToArray();

            SyncCalls.Add(snapshot);
            return Task.CompletedTask;
        }

        public Task<SettingsSyncStatus> GetCurrentStatusAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(StatusToReturn);
    }

    private readonly record struct SettingSyncCall(string Category, string Key, string SyncStatus);
}
