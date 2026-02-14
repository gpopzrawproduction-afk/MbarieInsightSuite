using MIC.Core.Domain.Entities;

namespace MIC.Core.Application.Common.Interfaces;

/// <summary>
/// Repository interface for notifications.
/// </summary>
public interface INotificationRepository : IRepository<Notification>
{
    /// <summary>
    /// Gets notifications for a user with optional filtering.
    /// </summary>
    Task<List<Notification>> GetUserNotificationsAsync(
        string userId,
        NotificationType? typeFilter = null,
        bool includeRead = false,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unread notification count for a user.
    /// </summary>
    Task<int> GetUnreadCountAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets total notification count for a user.
    /// </summary>
    Task<int> GetTotalCountAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks all notifications for a user as read.
    /// </summary>
    Task MarkAllAsReadAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
