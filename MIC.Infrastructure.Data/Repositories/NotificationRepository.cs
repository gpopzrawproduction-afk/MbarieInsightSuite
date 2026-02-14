using Microsoft.EntityFrameworkCore;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.Data.Persistence;

namespace MIC.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for notifications.
/// </summary>
public sealed class NotificationRepository : Repository<Notification>, INotificationRepository
{
    public NotificationRepository(MicDbContext context) : base(context) { }

    public async Task<List<Notification>> GetUserNotificationsAsync(
        string userId,
        NotificationType? typeFilter = null,
        bool includeRead = false,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(n => n.UserId == userId);

        if (typeFilter.HasValue)
        {
            query = query.Where(n => n.Type == typeFilter.Value);
        }

        if (!includeRead)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .CountAsync(n => n.UserId == userId && !n.IsRead && !n.IsDismissed, cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .CountAsync(n => n.UserId == userId && !n.IsDismissed, cancellationToken);
    }

    public async Task MarkAllAsReadAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var notifications = await _dbSet
            .Where(n => n.UserId == userId && !n.IsRead && !n.IsDismissed)
            .ToListAsync(cancellationToken);

        foreach (var notification in notifications)
        {
            notification.MarkAsRead();
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
