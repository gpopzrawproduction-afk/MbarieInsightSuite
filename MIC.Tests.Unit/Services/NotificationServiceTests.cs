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

    public void Dispose()
    {
        ResetSingleton();
        _scope.Dispose();
    }

    private static void ResetSingleton()
    {
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
        private readonly string? _backupPath;
        private readonly bool _hadExisting;

        public NotificationHistoryScope(bool clearHistory)
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MIC",
                "notifications");
            Directory.CreateDirectory(directory);

            HistoryPath = Path.Combine(directory, "history.json");

            if (File.Exists(HistoryPath))
            {
                _backupPath = Path.Combine(Path.GetTempPath(), $"mic-history-backup-{Guid.NewGuid():N}.json");
                File.Copy(HistoryPath, _backupPath, overwrite: true);
                _hadExisting = true;
                if (clearHistory)
                {
                    File.Delete(HistoryPath);
                }
            }
            else if (clearHistory && File.Exists(HistoryPath))
            {
                File.Delete(HistoryPath);
            }
        }

        public string HistoryPath { get; }

        public void Dispose()
        {
            try
            {
                if (_hadExisting && _backupPath is not null)
                {
                    File.Copy(_backupPath, HistoryPath, overwrite: true);
                    File.Delete(_backupPath);
                }
                else if (File.Exists(HistoryPath))
                {
                    File.Delete(HistoryPath);
                }
                else if (_backupPath is not null && File.Exists(_backupPath))
                {
                    File.Delete(_backupPath);
                }
            }
            catch
            {
                // Avoid masking test failures with cleanup errors.
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
