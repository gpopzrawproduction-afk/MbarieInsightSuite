using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.Data.Persistence;

namespace MIC.Infrastructure.Data.Services;

/// <summary>
/// Enterprise-grade settings service backed by EF Core with optional cloud synchronization.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private readonly IDbContextFactory<MicDbContext> _contextFactory;
    private readonly ILogger<SettingsService> _logger;
    private readonly ISettingsCloudSyncService _cloudSyncService;
    private readonly ISessionService? _sessionService;
    private readonly string _settingsFilePath;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private AppSettings _cachedSettings;

    public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    public SettingsService(
        IDbContextFactory<MicDbContext> contextFactory,
        ILogger<SettingsService> logger,
        ISettingsCloudSyncService cloudSyncService,
        ISessionService? sessionService = null,
        string? settingsFilePath = null)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _cloudSyncService = cloudSyncService;
        _sessionService = sessionService;

        if (string.IsNullOrWhiteSpace(settingsFilePath))
        {
            var roamingPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var micFolder = Path.Combine(roamingPath, "MIC");
            Directory.CreateDirectory(micFolder);
            _settingsFilePath = Path.Combine(micFolder, "settings.json");
        }
        else
        {
            var directory = Path.GetDirectoryName(settingsFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
            _settingsFilePath = settingsFilePath;
        }

        _cachedSettings = LoadSnapshotFromDisk();
    }

    public AppSettings GetSettings() => _cachedSettings;

    public async Task SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _cachedSettings = settings;

        var currentUser = GetCurrentUserId();
        if (currentUser != Guid.Empty)
        {
            await PersistForUserAsync(currentUser, settings, cancellationToken).ConfigureAwait(false);
        }

        await PersistSnapshotAsync(settings, cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveUserSettingsAsync(Guid userId, AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (userId == Guid.Empty)
        {
            _logger.LogWarning("SaveUserSettingsAsync invoked with empty user id; skipping persistence.");
            return;
        }

        if (IsCurrentUser(userId))
        {
            _cachedSettings = settings;
        }

        await PersistForUserAsync(userId, settings, cancellationToken).ConfigureAwait(false);
        await PersistSnapshotAsync(_cachedSettings, cancellationToken).ConfigureAwait(false);
    }

    public async Task<AppSettings> LoadUserSettingsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("LoadUserSettingsAsync invoked with empty user id; returning cached settings.");
            return _cachedSettings;
        }

        var settings = await BuildAppSettingsAsync(userId, cancellationToken).ConfigureAwait(false);

        if (IsCurrentUser(userId))
        {
            _cachedSettings = settings;
            await PersistSnapshotAsync(settings, cancellationToken).ConfigureAwait(false);
        }

        return settings;
    }

    public async Task<T> GetSettingAsync<T>(string category, string key, T defaultValue = default!, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return defaultValue;
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var record = await context.Settings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Category == category && s.Key == key, cancellationToken)
            .ConfigureAwait(false);

        return record is null
            ? defaultValue
            : DeserializeValue(record.Value, record.ValueType, defaultValue);
    }

    public async Task SetSettingAsync<T>(string category, string key, T value, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("SetSettingAsync skipped because no active user is available for category {Category} key {Key}.", category, key);
            return;
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var existing = await context.Settings
            .Include(s => s.History)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Category == category && s.Key == key, cancellationToken)
            .ConfigureAwait(false);

        var (serialized, valueType) = SerializeValue(value);
        var now = DateTimeOffset.UtcNow;
        var actor = GetCurrentUserDisplayName();

        if (existing is null)
        {
            existing = new Setting
            {
                UserId = userId,
                Category = category,
                Key = key,
                Value = serialized,
                ValueType = valueType,
                LastModified = now,
                SyncStatus = Setting.SyncStatuses.Pending
            };
            existing.MarkAsModified(actor);
            context.Settings.Add(existing);
        }
        else if (existing.Value != serialized || existing.ValueType != valueType)
        {
            context.SettingHistory.Add(new SettingHistory
            {
                SettingId = existing.Id,
                OldValue = existing.Value,
                NewValue = serialized,
                ChangedAt = now,
                ChangedBy = actor
            });

            existing.UpdateValue(serialized, valueType, actor);
        }
        else
        {
            return;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await RefreshCachedSettingsAsync(userId, context, cancellationToken).ConfigureAwait(false);
        RaiseSettingsChanged(category, key, value);
    }

    public async Task<bool> DeleteSettingAsync(string category, string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return false;
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var existing = await context.Settings
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Category == category && s.Key == key, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            return false;
        }

        context.SettingHistory.Add(new SettingHistory
        {
            SettingId = existing.Id,
            OldValue = existing.Value,
            NewValue = string.Empty,
            ChangedAt = DateTimeOffset.UtcNow,
            ChangedBy = GetCurrentUserDisplayName()
        });

        context.Settings.Remove(existing);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await RefreshCachedSettingsAsync(userId, context, cancellationToken).ConfigureAwait(false);
        RaiseSettingsChanged(category, key, null);
        return true;
    }

    public async Task<IDictionary<string, object>> GetCategorySettingsAsync(string category, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);

        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var records = await context.Settings.AsNoTracking()
            .Where(s => s.UserId == userId && s.Category == category)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var record in records)
        {
            var value = DeserializeValue(record.Value, record.ValueType, default(object)!);
            result[record.Key] = value ?? string.Empty;
        }

        return result;
    }

    public async Task SetMultipleSettingsAsync(string category, IDictionary<string, object> settings, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentNullException.ThrowIfNull(settings);

        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("SetMultipleSettingsAsync skipped because no active user is available for category {Category}.", category);
            return;
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await UpsertCategoryAsync(userId, category, settings, context, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await RefreshCachedSettingsAsync(userId, context, cancellationToken).ConfigureAwait(false);

        foreach (var entry in settings)
        {
            RaiseSettingsChanged(category, entry.Key, entry.Value);
        }
    }

    public async Task<IDictionary<string, IDictionary<string, object>>> GetAllSettingsAsync(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return new Dictionary<string, IDictionary<string, object>>(StringComparer.OrdinalIgnoreCase);
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var records = await context.Settings.AsNoTracking()
            .Where(s => s.UserId == userId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return GroupSettings(records);
    }

    public async Task<string> ExportSettingsAsync(CancellationToken cancellationToken = default)
    {
        var allSettings = await GetAllSettingsAsync(cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(allSettings, _serializerOptions);
    }

    public async Task ImportSettingsAsync(string json, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        var payload = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object?>>>(json, _serializerOptions)
                      ?? new Dictionary<string, Dictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);

        foreach (var (category, values) in payload)
        {
            var normalized = values
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Key))
                .ToDictionary(pair => pair.Key, pair => (object?)pair.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);

            await SetMultipleSettingsAsync(category, normalized, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task SyncWithCloudAsync(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            _logger.LogInformation("SyncWithCloudAsync skipped because no active user context is available.");
            return;
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var pending = await context.Settings
            .Where(s => s.UserId == userId && s.SyncStatus == Setting.SyncStatuses.Pending)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (pending.Count == 0)
        {
            _logger.LogInformation("No settings pending cloud synchronization.");
            return;
        }

        await _cloudSyncService.SyncSettingsAsync(pending, cancellationToken).ConfigureAwait(false);

        foreach (var item in pending)
        {
            item.SyncStatus = Setting.SyncStatuses.Synced;
            item.LastModified = DateTimeOffset.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<SettingsSyncStatus> GetSyncStatusAsync(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return new SettingsSyncStatus(0, 0, null);
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var pending = await context.Settings
            .CountAsync(s => s.UserId == userId && s.SyncStatus == Setting.SyncStatuses.Pending, cancellationToken)
            .ConfigureAwait(false);

        var synced = await context.Settings
            .CountAsync(s => s.UserId == userId && s.SyncStatus == Setting.SyncStatuses.Synced, cancellationToken)
            .ConfigureAwait(false);

        var cloudStatus = await _cloudSyncService.GetCurrentStatusAsync(cancellationToken).ConfigureAwait(false);

        return new SettingsSyncStatus(pending, synced, cloudStatus.LastSyncedAt);
    }

    private async Task PersistForUserAsync(Guid userId, AppSettings settings, CancellationToken cancellationToken)
    {
        var categories = ConvertToDictionary(settings);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        foreach (var (category, values) in categories)
        {
            if (values.Count == 0)
            {
                continue;
            }

            await UpsertCategoryAsync(userId, category, values, context, cancellationToken).ConfigureAwait(false);
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (IsCurrentUser(userId))
        {
            await RefreshCachedSettingsAsync(userId, context, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task UpsertCategoryAsync(Guid userId, string category, IDictionary<string, object> values, MicDbContext context, CancellationToken cancellationToken)
    {
        var existing = await context.Settings
            .Where(s => s.UserId == userId && s.Category == category)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var lookup = existing.ToDictionary(s => s.Key, StringComparer.OrdinalIgnoreCase);
        var actor = GetCurrentUserDisplayName();
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in values)
        {
            var (serialized, valueType) = SerializeValue(entry.Value);

            if (!lookup.TryGetValue(entry.Key, out var current))
            {
                current = new Setting
                {
                    UserId = userId,
                    Category = category,
                    Key = entry.Key,
                    Value = serialized,
                    ValueType = valueType,
                    LastModified = now,
                    SyncStatus = Setting.SyncStatuses.Pending
                };
                current.MarkAsModified(actor);
                context.Settings.Add(current);
            }
            else if (current.Value != serialized || current.ValueType != valueType)
            {
                context.SettingHistory.Add(new SettingHistory
                {
                    SettingId = current.Id,
                    OldValue = current.Value,
                    NewValue = serialized,
                    ChangedAt = now,
                    ChangedBy = actor
                });

                current.UpdateValue(serialized, valueType, actor);
            }
        }
    }

    private async Task RefreshCachedSettingsAsync(Guid userId, MicDbContext context, CancellationToken cancellationToken)
    {
        if (!IsCurrentUser(userId))
        {
            return;
        }

        var records = await context.Settings.AsNoTracking()
            .Where(s => s.UserId == userId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        _cachedSettings = BuildAppSettings(records);
        await PersistSnapshotAsync(_cachedSettings, cancellationToken).ConfigureAwait(false);
    }

    private AppSettings LoadSnapshotFromDisk()
    {
        if (!File.Exists(_settingsFilePath))
        {
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(_settingsFilePath);
            return JsonSerializer.Deserialize<AppSettings>(json, _serializerOptions) ?? new AppSettings();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read settings snapshot from {SettingsPath}.", _settingsFilePath);
            return new AppSettings();
        }
    }

    private async Task PersistSnapshotAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, _serializerOptions);
            await File.WriteAllTextAsync(_settingsFilePath, json, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write settings snapshot to {SettingsPath}.", _settingsFilePath);
        }
    }

    private async Task<AppSettings> BuildAppSettingsAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var records = await context.Settings.AsNoTracking()
            .Where(s => s.UserId == userId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return BuildAppSettings(records);
    }

    private static AppSettings BuildAppSettings(IEnumerable<Setting> records)
    {
        var grouped = GroupSettings(records);
        var result = new AppSettings();

        if (grouped.TryGetValue(nameof(AppSettings.AI), out var ai))
        {
            PopulateObject(result.AI, ai);
        }

        if (grouped.TryGetValue(nameof(AppSettings.EmailSync), out var sync))
        {
            PopulateObject(result.EmailSync, sync);
        }

        if (grouped.TryGetValue(nameof(AppSettings.UI), out var ui))
        {
            PopulateObject(result.UI, ui);
        }

        if (grouped.TryGetValue(nameof(AppSettings.Notifications), out var notifications))
        {
            PopulateObject(result.Notifications, notifications);
        }

        if (grouped.TryGetValue(nameof(AppSettings.General), out var general))
        {
            PopulateObject(result.General, general);
        }

        return result;
    }

    private static IDictionary<string, IDictionary<string, object>> GroupSettings(IEnumerable<Setting> records)
    {
        return records
            .GroupBy(setting => setting.Category, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IDictionary<string, object>)group.ToDictionary(
                    item => item.Key,
                    item => DeserializeValue(item.Value, item.ValueType, default(object)!) ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, IDictionary<string, object>> ConvertToDictionary(AppSettings settings)
    {
        return new Dictionary<string, IDictionary<string, object>>(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(AppSettings.AI)] = ToDictionary(settings.AI),
            [nameof(AppSettings.EmailSync)] = ToDictionary(settings.EmailSync),
            [nameof(AppSettings.UI)] = ToDictionary(settings.UI),
            [nameof(AppSettings.Notifications)] = ToDictionary(settings.Notifications),
            [nameof(AppSettings.General)] = ToDictionary(settings.General)
        };
    }

    private static IDictionary<string, object> ToDictionary<T>(T instance) where T : class
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in typeof(T).GetProperties())
        {
            if (!property.CanRead)
            {
                continue;
            }

            var value = property.GetValue(instance);
            if (value is not null)
            {
                result[property.Name] = value;
            }
        }

        return result;
    }

    private static void PopulateObject(object target, IDictionary<string, object> values)
    {
        var type = target.GetType();
        foreach (var entry in values)
        {
            var property = type.GetProperty(entry.Key);
            if (property is null || !property.CanWrite)
            {
                continue;
            }

            try
            {
                var converted = ConvertToPropertyType(entry.Value, property.PropertyType);
                property.SetValue(target, converted);
            }
            catch
            {
                // ignore conversion issues
            }
        }
    }

    private static object? ConvertToPropertyType(object value, Type targetType)
    {
        if (targetType == typeof(string))
        {
            return value?.ToString();
        }

        if (targetType == typeof(bool))
        {
            return value switch
            {
                bool b => b,
                string s when bool.TryParse(s, out var parsed) => parsed,
                _ => false
            };
        }

        if (targetType == typeof(int))
        {
            return value switch
            {
                int i => i,
                string s when int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
                _ => 0
            };
        }

        if (targetType == typeof(double))
        {
            return value switch
            {
                double d => d,
                string s when double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) => parsed,
                _ => 0d
            };
        }

        return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }

    private static (string Serialized, string ValueType) SerializeValue(object? value)
    {
        if (value is null)
        {
            return (string.Empty, "String");
        }

        return value switch
        {
            string s => (s, "String"),
            bool b => (b ? "true" : "false", "Bool"),
            int or long => (Convert.ToString(value, CultureInfo.InvariantCulture)!, "Int"),
            double or float or decimal => (Convert.ToString(value, CultureInfo.InvariantCulture)!, "Double"),
            _ => (JsonSerializer.Serialize(value), "Json")
        };
    }

    private static T DeserializeValue<T>(string value, string valueType, T defaultValue)
    {
        var result = DeserializeValue(value, valueType, defaultValue is null ? null : (object)defaultValue);
        return result is T typed ? typed : defaultValue;
    }

    private static object? DeserializeValue(string value, string valueType, object? defaultValue)
    {
        return valueType switch
        {
            "String" => value,
            "Bool" => bool.TryParse(value, out var b) ? b : defaultValue,
            "Int" => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : defaultValue,
            "Double" => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : defaultValue,
            "Json" => string.IsNullOrWhiteSpace(value) ? defaultValue : JsonSerializer.Deserialize<object>(value),
            _ => defaultValue
        };
    }

    private Guid GetCurrentUserId()
    {
        if (_sessionService?.IsAuthenticated == true)
        {
            var user = _sessionService.GetUser();
            if (user.Id != Guid.Empty)
            {
                return user.Id;
            }
        }

        return Guid.Empty;
    }

    private bool IsCurrentUser(Guid userId) => userId != Guid.Empty && userId == GetCurrentUserId();

    private string GetCurrentUserDisplayName()
    {
        if (_sessionService?.IsAuthenticated == true)
        {
            var user = _sessionService.GetUser();
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                return user.Email;
            }

            if (!string.IsNullOrWhiteSpace(user.Username))
            {
                return user.Username;
            }
        }

        return "system";
    }

    private void RaiseSettingsChanged(string category, string key, object? value)
    {
        try
        {
            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(category, key, value, _cachedSettings));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "A subscriber threw while handling SettingsChanged for {Category}/{Key}.", category, key);
        }
    }
}
