using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;

namespace MIC.Infrastructure.Data.Services;

/// <summary>
/// Default no-op implementation of <see cref="ISettingsCloudSyncService"/> used until
/// a real cloud synchronization provider is configured.
/// </summary>
public sealed class NoOpSettingsCloudSyncService : ISettingsCloudSyncService
{
    private readonly ILogger<NoOpSettingsCloudSyncService> _logger;
    private DateTimeOffset? _lastSyncedAt;
    private int _syncedCount;

    public NoOpSettingsCloudSyncService(ILogger<NoOpSettingsCloudSyncService> logger)
    {
        _logger = logger;
    }

    public Task SyncSettingsAsync(IEnumerable<Setting> settings, CancellationToken cancellationToken = default)
    {
        var syncedNow = settings?.Count() ?? 0;
        _syncedCount += syncedNow;
        _lastSyncedAt = DateTimeOffset.UtcNow;
        _logger.LogInformation("No-op cloud sync executed for {Count} settings", syncedNow);
        return Task.CompletedTask;
    }

    public Task<SettingsSyncStatus> GetCurrentStatusAsync(CancellationToken cancellationToken = default)
    {
        var status = new SettingsSyncStatus(PendingCount: 0, SyncedCount: _syncedCount, LastSyncedAt: _lastSyncedAt);
        return Task.FromResult(status);
    }
}
