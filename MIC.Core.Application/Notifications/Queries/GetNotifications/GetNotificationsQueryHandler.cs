using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;

namespace MIC.Core.Application.Notifications.Queries.GetNotifications;

public sealed class GetNotificationsQueryHandler(
    INotificationRepository notificationRepository,
    ISessionService sessionService,
    ILogger<GetNotificationsQueryHandler> logger) : IQueryHandler<GetNotificationsQuery, NotificationsResultDto>
{
    public async Task<ErrorOr<NotificationsResultDto>> Handle(GetNotificationsQuery query, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting notifications for user");

        try
        {
            var user = sessionService.GetUser();
            var userId = user?.Id ?? string.Empty;

            if (string.IsNullOrEmpty(userId))
            {
                return Error.Failure("auth.not_authenticated", "User not authenticated");
            }

            var notifications = await notificationRepository.GetUserNotificationsAsync(
                userId, query.FilterType, query.IncludeRead, query.PageSize, cancellationToken);

            var unreadCount = await notificationRepository.GetUnreadCountAsync(userId, cancellationToken);
            var totalCount = await notificationRepository.GetTotalCountAsync(userId, cancellationToken);

            var notificationDtos = notifications
                .Where(n => !n.IsDismissed)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    Severity = n.Severity,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    TimeAgo = GetTimeAgo(n.CreatedAt),
                    ActionRoute = n.ActionRoute,
                    IconColor = GetIconColor(n.Type, n.Severity)
                })
                .ToList();

            return new NotificationsResultDto
            {
                Notifications = notificationDtos,
                UnreadCount = unreadCount,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting notifications");
            return Error.Failure("notification.fetch_error", $"Failed to fetch notifications: {ex.Message}");
        }
    }

    private static string GetTimeAgo(DateTime createdAt)
    {
        var diff = DateTime.UtcNow - createdAt;

        return diff.TotalMinutes < 1 ? "just now"
             : diff.TotalMinutes < 60 ? $"{(int)diff.TotalMinutes} min ago"
             : diff.TotalHours < 24 ? $"{(int)diff.TotalHours} hr ago"
             : diff.TotalDays < 7 ? $"{(int)diff.TotalDays} days ago"
             : createdAt.ToString("dd MMM");
    }

    private static string GetIconColor(NotificationType type, NotificationSeverity severity)
    {
        return (type, severity) switch
        {
            (NotificationType.Alert, NotificationSeverity.Critical) => "#FF0055", // DangerRed
            (NotificationType.Alert, NotificationSeverity.Warning) => "#FFB800",  // AccentAmber
            (NotificationType.Email, _) => "#00D9FF",                             // AccentCyan
            (NotificationType.AiEvent, _) => "#BF40FF",                           // AccentMagenta
            _ => "#999AAA"                                                        // TextSecondary
        };
    }
}
