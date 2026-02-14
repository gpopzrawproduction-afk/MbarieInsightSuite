using MIC.Core.Domain.Abstractions;

namespace MIC.Core.Domain.Entities;

/// <summary>
/// Represents a notification in the system.
/// Notifications are used for alerts, emails, system events, AI events, and reports.
/// </summary>
public class Notification : BaseEntity
{
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public NotificationType Type { get; private set; }
    public NotificationSeverity Severity { get; private set; }
    public bool IsRead { get; private set; }
    public bool IsDismissed { get; private set; }
    public string? ActionRoute { get; private set; }  // e.g., "alerts/123" for deep link
    public string UserId { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private Notification() { }

    /// <summary>
    /// Creates a new notification.
    /// </summary>
    public static Notification Create(
        string title,
        string message,
        NotificationType type,
        NotificationSeverity severity,
        string userId,
        string? actionRoute = null)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            Title = title,
            Message = message,
            Type = type,
            Severity = severity,
            UserId = userId,
            ActionRoute = actionRoute,
            IsRead = false,
            IsDismissed = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Marks the notification as read.
    /// </summary>
    public void MarkAsRead() => IsRead = true;

    /// <summary>
    /// Dismisses the notification.
    /// </summary>
    public void Dismiss() => IsDismissed = true;
}

public enum NotificationType
{
    Alert,
    Email,
    System,
    AiEvent,
    Report
}

public enum NotificationSeverity
{
    Info,
    Warning,
    Critical
}
