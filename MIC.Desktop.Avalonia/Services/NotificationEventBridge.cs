using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;

namespace MIC.Desktop.Avalonia.Services;

/// <summary>
/// Bridges domain-level events into the notification service so that the notification center captures them.
/// </summary>
public sealed class NotificationEventBridge : IDisposable
{
    private readonly INotificationService _notifications;
    private readonly RealTimeDataService _realTime;
    private readonly CompositeDisposable _subscriptions = new();

    private int? _previousActiveAlerts;
    private int? _previousCriticalAlerts;
    private DateTime _lastDataSyncNotification = DateTime.MinValue;
    private string? _lastDataSyncMessage;

    public NotificationEventBridge(INotificationService notifications)
    {
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _realTime = RealTimeDataService.Instance;

        _subscriptions.Add(_realTime.AlertEvents
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(HandleAlertEvent));

        _subscriptions.Add(_realTime.DataUpdates
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(HandleDataUpdate));
    }

    private void HandleAlertEvent(AlertEvent alertEvent)
    {
        if (!string.IsNullOrWhiteSpace(alertEvent.Title))
        {
            NotifyForSeverity(alertEvent.Severity, alertEvent.Title!, alertEvent.Message ?? string.Empty);
            return;
        }

        if (_previousActiveAlerts.HasValue)
        {
            if (alertEvent.ActiveCount > _previousActiveAlerts)
            {
                _notifications.ShowWarning(
                    $"Active alerts increased to {alertEvent.ActiveCount}",
                    "Alerts Updated",
                    "Alerts");
            }
            else if (alertEvent.ActiveCount < _previousActiveAlerts)
            {
                _notifications.ShowSuccess(
                    $"Alerts resolved. {alertEvent.ActiveCount} active remaining",
                    "Alerts Resolved",
                    "Alerts");
            }
        }

        if (_previousCriticalAlerts.HasValue)
        {
            if (alertEvent.CriticalCount > _previousCriticalAlerts)
            {
                _notifications.ShowError(
                    $"{alertEvent.CriticalCount} critical alerts require attention",
                    "Critical Alerts",
                    "Alerts");
            }
            else if (_previousCriticalAlerts > 0 && alertEvent.CriticalCount == 0)
            {
                _notifications.ShowSuccess(
                    "All critical alerts resolved",
                    "Critical Alerts",
                    "Alerts");
            }
        }

        _previousActiveAlerts = alertEvent.ActiveCount;
        _previousCriticalAlerts = alertEvent.CriticalCount;
    }

    private void HandleDataUpdate(DataUpdateEvent updateEvent)
    {
        if (updateEvent.IsError)
        {
            _notifications.ShowError(updateEvent.Message, $"{updateEvent.Source} Sync Error", "Sync");
            return;
        }

        var now = DateTime.Now;
        if (_lastDataSyncMessage == updateEvent.Message && (now - _lastDataSyncNotification) < TimeSpan.FromMinutes(5))
        {
            return;
        }

        _notifications.ShowInfo(updateEvent.Message, "Data Synchronized", "Sync");
        _lastDataSyncNotification = now;
        _lastDataSyncMessage = updateEvent.Message;
    }

    private void NotifyForSeverity(string? severity, string title, string message)
    {
        switch (severity)
        {
            case "Critical":
                _notifications.ShowError(message, title, "Alerts");
                break;
            case "Warning":
                _notifications.ShowWarning(message, title, "Alerts");
                break;
            default:
                _notifications.ShowInfo(message, title, "Alerts");
                break;
        }
    }

    public void Dispose()
    {
        _subscriptions.Dispose();
    }
}
