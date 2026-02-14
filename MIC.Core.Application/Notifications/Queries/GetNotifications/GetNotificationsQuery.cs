using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;

namespace MIC.Core.Application.Notifications.Queries.GetNotifications;

/// <summary>
/// Query to retrieve notifications with optional filtering and pagination.
/// </summary>
public record GetNotificationsQuery : IQuery<NotificationsResultDto>
{
    public NotificationType? FilterType { get; init; }
    public bool IncludeRead { get; init; } = false;
    public int PageSize { get; init; } = 50;
}

public record NotificationsResultDto
{
    public List<NotificationDto> Notifications { get; init; } = new();
    public int UnreadCount { get; init; }
    public int TotalCount { get; init; }
}

public record NotificationDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public NotificationType Type { get; init; }
    public NotificationSeverity Severity { get; init; }
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
    public string TimeAgo { get; init; } = string.Empty;
    public string? ActionRoute { get; init; }
    public string IconColor { get; init; } = string.Empty;
}
