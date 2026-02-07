using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using ReactiveUI;
using Serilog;

namespace MIC.Desktop.Avalonia.Services;

/// <summary>
/// Global toast notification service for user feedback.
/// </summary>
public interface INotificationService
{
    ObservableCollection<ToastNotification> Notifications { get; }
    ReadOnlyObservableCollection<NotificationEntry> NotificationHistory { get; }
    event EventHandler? HistoryChanged;
    void ShowSuccess(string message, string? title = null, string? category = null);
    void ShowError(string message, string? title = null, string? category = null);
    void ShowWarning(string message, string? title = null, string? category = null);
    void ShowInfo(string message, string? title = null, string? category = null);
    void Dismiss(ToastNotification notification);
    void DismissAll();
    void MarkAsRead(Guid notificationId);
    void MarkAllAsRead();
    void Remove(Guid notificationId);
    void ClearHistory();
    int UnreadCount { get; }
}

public class NotificationService : INotificationService
{
    private static NotificationService? _instance;
    public static NotificationService Instance => _instance ??= new NotificationService();

    public ObservableCollection<ToastNotification> Notifications { get; } = new();
    private readonly ObservableCollection<NotificationEntry> _history = new();
    public ReadOnlyObservableCollection<NotificationEntry> NotificationHistory { get; }

    private readonly string _historyFilePath;
    private readonly SemaphoreSlim _persistenceLock = new(1, 1);
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public event EventHandler? HistoryChanged;

    private NotificationService()
    {
        NotificationHistory = new ReadOnlyObservableCollection<NotificationEntry>(_history);
        _historyFilePath = BuildHistoryPath();
        LoadHistory();
    }

    public void ShowSuccess(string message, string? title = null, string? category = null)
    {
        Show(new ToastNotification
        {
            Type = ToastType.Success,
            Title = title ?? "Success",
            Message = message,
            Icon = "✔",
            IconColor = "#39FF14",
            BorderColor = "#339900",
            BackgroundColor = "#20339900",
            Category = category ?? "Success"
        });
    }

    public void ShowError(string message, string? title = null, string? category = null)
    {
        Show(new ToastNotification
        {
            Type = ToastType.Error,
            Title = title ?? "Error",
            Message = message,
            Icon = "⚠",
            IconColor = "#FF3366",
            BorderColor = "#FF0055",
            BackgroundColor = "#30FF0055",
            Duration = TimeSpan.FromSeconds(8), // Errors stay longer
            Category = category ?? "Errors"
        });
    }

    public void ShowWarning(string message, string? title = null, string? category = null)
    {
        Show(new ToastNotification
        {
            Type = ToastType.Warning,
            Title = title ?? "Warning",
            Message = message,
            Icon = "⚠",
            IconColor = "#FF9500",
            BorderColor = "#FF9500",
            BackgroundColor = "#30FF9500",
            Category = category ?? "Warnings"
        });
    }

    public void ShowInfo(string message, string? title = null, string? category = null)
    {
        Show(new ToastNotification
        {
            Type = ToastType.Info,
            Title = title ?? "Info",
            Message = message,
            Icon = "ℹ",
            IconColor = "#00E5FF",
            BorderColor = "#00E5FF",
            BackgroundColor = "#2000E5FF",
            Category = category ?? "General"
        });
    }

    public void Dismiss(ToastNotification notification)
    {
        if (Notifications.Contains(notification))
        {
            Notifications.Remove(notification);
        }
    }

    public void DismissAll()
    {
        Notifications.Clear();
    }

    public void MarkAsRead(Guid notificationId)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_history.FirstOrDefault(n => n.Id == notificationId) is { } entry)
            {
                if (!entry.IsRead)
                {
                    entry.IsRead = true;
                    NotifyHistoryChanged();
                    _ = PersistHistoryAsync();
                }
            }
        });
    }

    public void MarkAllAsRead()
    {
        Dispatcher.UIThread.Post(() =>
        {
            var updated = false;
            foreach (var entry in _history)
            {
                if (!entry.IsRead)
                {
                    entry.IsRead = true;
                    updated = true;
                }
            }
            if (updated)
            {
                NotifyHistoryChanged();
                _ = PersistHistoryAsync();
            }
        });
    }

    public void Remove(Guid notificationId)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_history.FirstOrDefault(n => n.Id == notificationId) is { } entry)
            {
                _history.Remove(entry);
                NotifyHistoryChanged();
                _ = PersistHistoryAsync();
            }
        });
    }

    public void ClearHistory()
    {
        Dispatcher.UIThread.Post(() =>
        {
            _history.Clear();
            NotifyHistoryChanged();
            _ = PersistHistoryAsync();
        });
    }

    public int UnreadCount => _history.Count(n => !n.IsRead);

    private void Show(ToastNotification notification)
    {
        // Add to collection (on UI thread if needed)
        Dispatcher.UIThread.Post(() =>
        {
            Notifications.Insert(0, notification);

            AddToHistory(notification);

            // Auto-dismiss after duration
            if (notification.Duration > TimeSpan.Zero)
            {
                Observable.Timer(notification.Duration)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(_ => Dismiss(notification));
            }

            // Limit max visible notifications
            while (Notifications.Count > 5)
            {
                Notifications.RemoveAt(Notifications.Count - 1);
            }
        });
    }

    private void AddToHistory(ToastNotification notification)
    {
        var entry = new NotificationEntry
        {
            Id = notification.Id,
            Title = notification.Title,
            Message = notification.Message,
            Category = notification.Category,
            Type = notification.Type,
            Icon = notification.Icon,
            CreatedAt = notification.CreatedAt
        };

        _history.Insert(0, entry);

        TrimHistory();
        NotifyHistoryChanged();
        _ = PersistHistoryAsync();
    }

    private void TrimHistory()
    {
        const int maxHistoryItems = 100;
        while (_history.Count > maxHistoryItems)
        {
            _history.RemoveAt(_history.Count - 1);
        }
    }

    private void NotifyHistoryChanged()
    {
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    private string BuildHistoryPath()
    {
        var basePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MIC",
            "notifications");
        Directory.CreateDirectory(basePath);
        return Path.Combine(basePath, "history.json");
    }

    private void LoadHistory()
    {
        try
        {
            if (!File.Exists(_historyFilePath))
            {
                return;
            }

            using var stream = File.OpenRead(_historyFilePath);
            var records = JsonSerializer.Deserialize<List<NotificationRecord>>(stream, _serializerOptions);
            if (records == null)
            {
                return;
            }

            _history.Clear();
            foreach (var record in records.OrderByDescending(r => r.CreatedAt))
            {
                _history.Add(new NotificationEntry
                {
                    Id = record.Id,
                    Title = record.Title,
                    Message = record.Message,
                    Category = record.Category,
                    Type = record.Type,
                    Icon = record.Icon,
                    CreatedAt = record.CreatedAt,
                    IsRead = record.IsRead
                });
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to load notification history");
        }
    }

    private async Task PersistHistoryAsync()
    {
        try
        {
            await _persistenceLock.WaitAsync().ConfigureAwait(false);
            var snapshot = _history
                .Select(entry => new NotificationRecord
                {
                    Id = entry.Id,
                    Title = entry.Title,
                    Message = entry.Message,
                    Category = entry.Category,
                    Type = entry.Type,
                    Icon = entry.Icon,
                    CreatedAt = entry.CreatedAt,
                    IsRead = entry.IsRead
                })
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            using var stream = File.Create(_historyFilePath);
            await JsonSerializer.SerializeAsync(stream, snapshot, _serializerOptions).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to persist notification history");
        }
        finally
        {
            _persistenceLock.Release();
        }
    }
}

internal sealed class NotificationRecord
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public ToastType Type { get; set; }
    public string Icon { get; set; } = "ℹ";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsRead { get; set; }
}

public class ToastNotification : ReactiveObject
{
    public Guid Id { get; } = Guid.NewGuid();
    public ToastType Type { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Icon { get; init; } = "?";
    public string Category { get; init; } = "General";
    public TimeSpan Duration { get; init; } = TimeSpan.FromSeconds(4);
    public DateTime CreatedAt { get; } = DateTime.Now;

    public string BackgroundColor { get; init; } = "#20FFFFFF";
    public string BorderColor { get; init; } = "#FFFFFF";
    public string IconColor { get; init; } = "#FFFFFF";
}

public enum ToastType
{
    Info,
    Success,
    Warning,
    Error
}

public class NotificationEntry : ReactiveObject
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Category { get; init; } = "General";
    public ToastType Type { get; init; }
    public string Icon { get; init; } = "ℹ";
    public DateTime CreatedAt { get; init; } = DateTime.Now;

    private bool _isRead;
    public bool IsRead
    {
        get => _isRead;
        set => this.RaiseAndSetIfChanged(ref _isRead, value);
    }

    public string TimeAgo => FormatTimeAgo(DateTime.Now - CreatedAt);

    private static string FormatTimeAgo(TimeSpan elapsed)
    {
        if (elapsed.TotalSeconds < 60)
        {
            return "Just now";
        }

        if (elapsed.TotalMinutes < 60)
        {
            return $"{Math.Floor(elapsed.TotalMinutes)}m ago";
        }

        if (elapsed.TotalHours < 24)
        {
            return $"{Math.Floor(elapsed.TotalHours)}h ago";
        }

        return $"{Math.Floor(elapsed.TotalDays)}d ago";
    }
}
