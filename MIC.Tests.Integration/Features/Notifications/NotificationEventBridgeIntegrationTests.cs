using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Reflection;
using FluentAssertions;
using MIC.Desktop.Avalonia.Services;
using ReactiveUI;
using Xunit;

namespace MIC.Tests.Integration.Features.Notifications;

public class NotificationEventBridgeIntegrationTests : IDisposable
{
    private readonly IDisposable _bridge;
    private readonly RecordingNotificationService _notifications;
    private readonly RealTimeDataService _realTime;

    public NotificationEventBridgeIntegrationTests()
    {
        RxApp.MainThreadScheduler = CurrentThreadScheduler.Instance;

        _realTime = ResetRealTimeDataService();
        _notifications = new RecordingNotificationService();
        _bridge = new NotificationEventBridge(_notifications);
    }

    [Fact]
    public void CriticalAlert_ProducesErrorNotification()
    {
        PublishAlert(new AlertEvent
        {
            Title = "Database Outage",
            Message = "Primary cluster unavailable",
            Severity = "Critical"
        });

        _notifications.Calls.Should().ContainSingle(call =>
            call.Kind == NotificationKind.Error &&
            call.Title == "Database Outage" &&
            call.Message == "Primary cluster unavailable" &&
            call.Category == "Alerts");
    }

    [Fact]
    public void AlertCounts_EmitEscalationAndResolutionNotifications()
    {
        PublishAlert(new AlertEvent
        {
            ActiveCount = 2,
            CriticalCount = 0
        });

        PublishAlert(new AlertEvent
        {
            ActiveCount = 5,
            CriticalCount = 1
        });

        PublishAlert(new AlertEvent
        {
            ActiveCount = 1,
            CriticalCount = 0
        });

        _notifications.Calls.Should().ContainEquivalentOf(new NotificationCall(NotificationKind.Warning,
            "Alerts Updated",
            "Active alerts increased to 5",
            "Alerts"));

        _notifications.Calls.Should().ContainEquivalentOf(new NotificationCall(NotificationKind.Error,
            "Critical Alerts",
            "1 critical alerts require attention",
            "Alerts"));

        _notifications.Calls.Should().ContainEquivalentOf(new NotificationCall(NotificationKind.Success,
            "Alerts Resolved",
            "Alerts resolved. 1 active remaining",
            "Alerts"));

        _notifications.Calls.Should().ContainEquivalentOf(new NotificationCall(NotificationKind.Success,
            "Critical Alerts",
            "All critical alerts resolved",
            "Alerts"));
    }

    [Fact]
    public void DataUpdates_DeduplicateWithinWindowAndSurfaceErrors()
    {
        PublishDataUpdate(new DataUpdateEvent
        {
            Source = "Sync",
            Message = "Data synchronized",
            IsError = false
        });

        PublishDataUpdate(new DataUpdateEvent
        {
            Source = "Sync",
            Message = "Data synchronized",
            IsError = false
        });

        PublishDataUpdate(new DataUpdateEvent
        {
            Source = "Sync",
            Message = "Failed to reach server",
            IsError = true
        });

        _notifications.Calls.Count(call => call.Kind == NotificationKind.Info).Should().Be(1);
        _notifications.Calls.Should().ContainEquivalentOf(new NotificationCall(NotificationKind.Info,
            "Data Synchronized",
            "Data synchronized",
            "Sync"));

        _notifications.Calls.Should().ContainEquivalentOf(new NotificationCall(NotificationKind.Error,
            "Sync Sync Error",
            "Failed to reach server",
            "Sync"));
    }

    public void Dispose()
    {
        (_bridge as IDisposable)?.Dispose();
        _notifications.Dispose();
    }

    private void PublishAlert(AlertEvent alertEvent)
    {
        var subject = (ISubject<AlertEvent>)typeof(RealTimeDataService)
            .GetField("_alertEvents", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(_realTime)!;

        subject.OnNext(alertEvent);
    }

    private void PublishDataUpdate(DataUpdateEvent updateEvent)
    {
        var subject = (ISubject<DataUpdateEvent>)typeof(RealTimeDataService)
            .GetField("_dataUpdates", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(_realTime)!;

        subject.OnNext(updateEvent);
    }

    private static RealTimeDataService ResetRealTimeDataService()
    {
        var instanceField = typeof(RealTimeDataService)
            .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
        instanceField.SetValue(null, null);
        return RealTimeDataService.Instance;
    }

    private sealed class RecordingNotificationService : INotificationService, IDisposable
    {
        private readonly ObservableCollection<ToastNotification> _notifications = new();
        private readonly ReadOnlyObservableCollection<NotificationEntry> _history;

        public RecordingNotificationService()
        {
            _history = new ReadOnlyObservableCollection<NotificationEntry>(new ObservableCollection<NotificationEntry>());
        }

        public ObservableCollection<ToastNotification> Notifications => _notifications;
        public ReadOnlyObservableCollection<NotificationEntry> NotificationHistory => _history;
        public event EventHandler? HistoryChanged;
        public List<NotificationCall> Calls { get; } = new();
        public int UnreadCount => 0;

        public void ShowSuccess(string message, string? title = null, string? category = null) =>
            Record(NotificationKind.Success, title, message, category);

        public void ShowError(string message, string? title = null, string? category = null) =>
            Record(NotificationKind.Error, title, message, category);

        public void ShowWarning(string message, string? title = null, string? category = null) =>
            Record(NotificationKind.Warning, title, message, category);

        public void ShowInfo(string message, string? title = null, string? category = null) =>
            Record(NotificationKind.Info, title, message, category);

        public void Dismiss(ToastNotification notification)
        {
        }

        public void DismissAll()
        {
        }

        public void MarkAsRead(Guid notificationId)
        {
        }

        public void MarkAllAsRead()
        {
        }

        public void Remove(Guid notificationId)
        {
        }

        public void ClearHistory()
        {
        }

        public void Dispose()
        {
        }

        private void Record(NotificationKind kind, string? title, string message, string? category)
        {
            Calls.Add(new NotificationCall(kind, title ?? string.Empty, message, category ?? string.Empty));
            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private enum NotificationKind
    {
        Info,
        Success,
        Warning,
        Error
    }

    private readonly record struct NotificationCall(NotificationKind Kind, string Title, string Message, string Category);
}
