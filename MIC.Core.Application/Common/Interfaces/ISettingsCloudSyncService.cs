using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MIC.Core.Domain.Entities;

namespace MIC.Core.Application.Common.Interfaces;

/// <summary>
/// Provides cloud synchronization capabilities for persisted settings.
/// </summary>
public interface ISettingsCloudSyncService
{
    Task SyncSettingsAsync(IEnumerable<Setting> settings, CancellationToken cancellationToken = default);

    Task<SettingsSyncStatus> GetCurrentStatusAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents synchronization metrics for persisted settings.
/// </summary>
public sealed record SettingsSyncStatus(int PendingCount, int SyncedCount, DateTimeOffset? LastSyncedAt);
