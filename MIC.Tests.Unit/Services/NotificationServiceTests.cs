using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text.Json;
using System.Threading;
using Avalonia.Threading;
using MIC.Desktop.Avalonia.Services;
using ReactiveUI;
using Xunit;

namespace MIC.Tests.Unit.Services;

[CollectionDefinition("NotificationServiceTests", DisableParallelization = true)]
public sealed class NotificationServiceTestsCollectionDefinition
{
}

[Collection("NotificationServiceTests")]
public sealed class NotificationServiceTests : IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly NotificationHistoryScope _scope;

    static NotificationServiceTests()
    {
        RxApp.MainThreadScheduler = CurrentThreadScheduler.Instance;
        RxApp.TaskpoolScheduler = CurrentThreadScheduler.Instance;
    }

    public NotificationServiceTests()
    {
        _scope = new NotificationHistoryScope(clearHistory: true);
        ResetSingleton();
    }

    [Fact]
    public void ShowSuccess_AddsNotificationAndHistory()
    {
        var service = NotificationService.Instance;
        var historyChanged = 0;
        service.HistoryChanged += (_, _) => historyChanged++;

        service.ShowSuccess("Profile updated", "Saved", "Operations");
        ProcessDispatcherJobs();

        Assert.Equal(1, historyChanged);
        Assert.Equal(1, service.UnreadCount);
        Assert.Single(service.Notifications);
        Assert.Single(service.NotificationHistory);

        var toast = service.Notifications.First();
        Assert.Equal(ToastType.Success, toast.Type);
        Assert.Equal("Saved", toast.Title);
        Assert.Equal("Operations", toast.Category);

        var entry = service.NotificationHistory.First();
        Assert.Equal(toast.Id, entry.Id);
        Assert.Equal(toast.Message, entry.Message);
        Assert.False(entry.IsRead);
    }

    [Fact]
    public void MarkAllAsRead_SetsHistoryEntriesToReadAndPersists()
    {
        var service = NotificationService.Instance;
        service.ShowInfo("First message");
        service.ShowWarning("Second message");
        ProcessDispatcherJobs();

        service.MarkAllAsRead();
        ProcessDispatcherJobs();

        Assert.Equal(0, service.UnreadCount);
        Assert.All(service.NotificationHistory, entry => Assert.True(entry.IsRead));

        Assert.True(WaitForHistoryWrite(_scope.HistoryPath));
        var payload = File.ReadAllText(_scope.HistoryPath);
        var records = JsonSerializer.Deserialize<List<NotificationRecordDto>>(payload, SerializerOptions);
        Assert.NotNull(records);
        Assert.All(records!, record => Assert.True(record.IsRead));
    }

    [Fact]
    public void Remove_RemovesEntryFromHistory()
    {
        var service = NotificationService.Instance;
        service.ShowInfo("Reminder");
        ProcessDispatcherJobs();
        var entryId = service.NotificationHistory.First().Id;

        service.Remove(entryId);
        ProcessDispatcherJobs();

        Assert.Empty(service.NotificationHistory);
    }

    [Fact]
    public void ShowInfo_LimitsActiveNotificationsToFive()
    {
        var service = NotificationService.Instance;
        for (var i = 0; i < 7; i++)
        {
            service.ShowInfo($"Message {i}");
        }
        ProcessDispatcherJobs();

        Assert.Equal(5, service.Notifications.Count);
        Assert.Equal("Message 6", service.Notifications.First().Message);
        Assert.Equal("Message 2", service.Notifications.Last().Message);
        Assert.Equal(7, service.NotificationHistory.Count);
    }

    [Fact]
    public void Constructor_WithExistingHistory_LoadsEntries()
    {
        var existing = new List<NotificationRecordDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Alert",
                Message = "Existing",
                Category = "Alerts",
                Type = ToastType.Warning,
                Icon = "!",
                CreatedAt = DateTime.Now.AddMinutes(-10),
                IsRead = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Info",
                Message = "Previous",
                Category = "General",
                Type = ToastType.Info,
                Icon = "i",
                CreatedAt = DateTime.Now.AddMinutes(-5),
                IsRead = true
            }
        };

        File.WriteAllText(_scope.HistoryPath, JsonSerializer.Serialize(existing, SerializerOptions));
        ResetSingleton();

        var service = NotificationService.Instance;

        Assert.Equal(2, service.NotificationHistory.Count);
        Assert.Collection(
            service.NotificationHistory,
            first => Assert.Equal(existing[1].Id, first.Id),
            second => Assert.Equal(existing[0].Id, second.Id));
    }

    [Fact]
    public void ShowError_AddsNotificationWithErrorType()
    {
        var service = NotificationService.Instance;
        service.ShowError("Connection failed", "Error", "Network");
        ProcessDispatcherJobs();

        Assert.Equal(1, service.UnreadCount);
        Assert.Single(service.Notifications);
        var toast = service.Notifications.First();
        Assert.Equal(ToastType.Error, toast.Type);
        Assert.Equal("Error", toast.Title);
        Assert.Equal("Network", toast.Category);
    }

    [Fact]
    public void MarkAsRead_SetsSpecificEntryToRead()
    {
        var service = NotificationService.Instance;
        service.ShowInfo("First");
        service.ShowInfo("Second");
        ProcessDispatcherJobs();

        var entryId = service.NotificationHistory.First().Id;
        service.MarkAsRead(entryId);
        ProcessDispatcherJobs();

        var entry = service.NotificationHistory.First(e => e.Id == entryId);
        Assert.True(entry.IsRead);
        Assert.Equal(1, service.UnreadCount);
    }

    [Fact]
    public void MarkAsRead_WithNonExistentId_DoesNotThrow()
    {
        var service = NotificationService.Instance;
        service.ShowInfo("Something");
        ProcessDispatcherJobs();

        service.MarkAsRead(Guid.NewGuid());
        ProcessDispatcherJobs();

        Assert.Equal(1, service.UnreadCount);
    }

    [Fact]
    public void Dismiss_RemovesFromActiveNotifications()
    {
        var service = NotificationService.Instance;
        service.ShowInfo("Dismissable");
        ProcessDispatcherJobs();

        var toast = service.Notifications.First();
        service.Dismiss(toast);
        ProcessDispatcherJobs();

        Assert.Empty(service.Notifications);
        Assert.Single(service.NotificationHistory);
    }

    [Fact]
    public void DismissAll_ClearsAllActiveNotifications()
    {
        var service = NotificationService.Instance;
        service.ShowInfo("First");
        service.ShowWarning("Second");
        ProcessDispatcherJobs();

        service.DismissAll();
        ProcessDispatcherJobs();

        Assert.Empty(service.Notifications);
        Assert.Equal(2, service.NotificationHistory.Count);
    }

    [Fact]
    public void ClearHistory_RemovesAllHistoryEntries()
    {
        var service = NotificationService.Instance;
        service.ShowInfo("First");
        service.ShowWarning("Second");
        ProcessDispatcherJobs();

        service.ClearHistory();
        ProcessDispatcherJobs();

        Assert.Empty(service.NotificationHistory);
        Assert.Equal(0, service.UnreadCount);
    }

    public void Dispose()
    {
        ResetSingleton();
        _scope.Dispose();
    }

    private static void ResetSingleton()
    {
        // Clear all notifications and timers from the current instance before resetting
        var currentInstance = NotificationService.Instance;
        if (currentInstance != null)
        {
            currentInstance.ClearAllForTests();
            ProcessDispatcherJobs(); // Ensure all pending dismissals are processed
        }

        var field = typeof(NotificationService).GetField("_instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        field?.SetValue(null, null);
    }

    private static bool WaitForHistoryWrite(string path)
    {
        var limit = TimeSpan.FromSeconds(1);
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < limit)
        {
            try
            {
                if (File.Exists(path))
                {
                    using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    if (stream.Length > 0)
                    {
                        return true;
                    }
                }
            }
            catch (IOException)
            {
                // File may still be locked; retry until timeout.
            }

            Thread.Sleep(10);
        }

        return false;
    }

    // Ensures dispatcher-queued work from NotificationService executes before assertions.
    private static void ProcessDispatcherJobs()
    {
        Dispatcher.UIThread.RunJobs(null);
    }

    private sealed class NotificationHistoryScope : IDisposable
    {
        private readonly string? _previousOverride;
        private readonly string? _previousAutoDismiss;

        public NotificationHistoryScope(bool clearHistory)
        {
            _previousOverride = Environment.GetEnvironmentVariable("MIC_NOTIFICATION_HISTORY_PATH");
            _previousAutoDismiss = Environment.GetEnvironmentVariable("MIC_NOTIFICATION_DISABLE_AUTODISMISS");
            HistoryPath = Path.Combine(Path.GetTempPath(), $"mic-history-{Guid.NewGuid():N}.json");
            Environment.SetEnvironmentVariable("MIC_NOTIFICATION_HISTORY_PATH", HistoryPath);
            Environment.SetEnvironmentVariable("MIC_NOTIFICATION_DISABLE_AUTODISMISS", "1");

            if (clearHistory && File.Exists(HistoryPath))
            {
                File.Delete(HistoryPath);
            }
        }

        public string HistoryPath { get; }

        public void Dispose()
        {
            try
            {
                if (File.Exists(HistoryPath))
                {
                    File.Delete(HistoryPath);
                }
            }
            catch
            {
                // Avoid masking test failures with cleanup errors.
            }
            finally
            {
                Environment.SetEnvironmentVariable("MIC_NOTIFICATION_HISTORY_PATH", _previousOverride);
                Environment.SetEnvironmentVariable("MIC_NOTIFICATION_DISABLE_AUTODISMISS", _previousAutoDismiss);
            }
        }
    }

    private sealed class NotificationRecordDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public ToastType Type { get; set; }
        public string Icon { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }
}
