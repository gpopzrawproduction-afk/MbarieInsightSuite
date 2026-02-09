using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using Avalonia.Threading;
using FluentAssertions;
using MIC.Desktop.Avalonia.Services;
using ReactiveUI;
using Xunit;
using System.Reactive.Concurrency;

namespace MIC.Tests.Integration.Features.Notifications;

public sealed class NotificationServiceIntegrationTests : IDisposable
{
    private readonly NotificationHistoryScope _scope;

    static NotificationServiceIntegrationTests()
    {
        RxApp.MainThreadScheduler = CurrentThreadScheduler.Instance;
        RxApp.TaskpoolScheduler = CurrentThreadScheduler.Instance;
    }

    public NotificationServiceIntegrationTests()
    {
        _scope = new NotificationHistoryScope(clearHistory: true);
        ResetSingleton();
    }

    [Fact]
    public void ShowError_PersistsNotificationToHistory()
    {
        var service = NotificationService.Instance;

        service.ShowError("Primary cluster unavailable", title: "Database Outage", category: "Infra");
        Dispatcher.UIThread.RunJobs(null);

        WaitForHistoryWrite(_scope.HistoryPath).Should().BeTrue();

        var payload = File.ReadAllText(_scope.HistoryPath);
        var records = JsonSerializer.Deserialize<List<NotificationRecordDto>>(payload, NotificationHistoryScope.SerializerOptions);
        records.Should().NotBeNull();
        records!.Should().ContainSingle(record =>
            record.Title == "Database Outage" &&
            record.Message == "Primary cluster unavailable" &&
            record.Category == "Infra" &&
            record.Type == ToastType.Error);

        service.NotificationHistory.Should().ContainSingle(entry =>
            entry.Title == "Database Outage" &&
            entry.Message == "Primary cluster unavailable" &&
            entry.Category == "Infra" &&
            entry.Type == ToastType.Error);
    }

    [Fact]
    public void MarkAllAsRead_PersistsStateAcrossInstances()
    {
        var service = NotificationService.Instance;
        service.ShowInfo("Sync complete", title: "Data", category: "Sync");
        Dispatcher.UIThread.RunJobs(null);

        service.MarkAllAsRead();
        Dispatcher.UIThread.RunJobs(null);

        WaitForHistoryWrite(_scope.HistoryPath).Should().BeTrue();

        ResetSingleton();
        var reloaded = NotificationService.Instance;

        Dispatcher.UIThread.RunJobs(null);

        reloaded.UnreadCount.Should().Be(0);
        reloaded.NotificationHistory.Should().NotBeEmpty();
        reloaded.NotificationHistory.Should().OnlyContain(entry => entry.IsRead);
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
        var deadline = DateTime.UtcNow.AddSeconds(2);
        while (DateTime.UtcNow < deadline)
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
                // File may be locked; retry until deadline.
            }

            Thread.Sleep(25);
        }

        return false;
    }

    private sealed class NotificationHistoryScope : IDisposable
    {
        public static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

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

    private sealed record NotificationRecordDto
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public ToastType Type { get; init; }
        public string Icon { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        public bool IsRead { get; init; }
    }
}
