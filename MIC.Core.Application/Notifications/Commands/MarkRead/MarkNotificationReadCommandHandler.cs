using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;

namespace MIC.Core.Application.Notifications.Commands.MarkRead;

public sealed class MarkNotificationReadCommandHandler(
    INotificationRepository notificationRepository,
    ILogger<MarkNotificationReadCommandHandler> logger) : ICommandHandler<MarkNotificationReadCommand, bool>
{
    public async Task<ErrorOr<bool>> Handle(MarkNotificationReadCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation("Marking notification as read: {NotificationId}", command.NotificationId);

        try
        {
            var notification = await notificationRepository.GetByIdAsync(command.NotificationId, cancellationToken);

            if (notification == null)
            {
                logger.LogWarning("Notification not found: {NotificationId}", command.NotificationId);
                return Error.NotFound("notification.not_found", "Notification not found");
            }

            notification.MarkAsRead();
            await notificationRepository.UpdateAsync(notification, cancellationToken);

            logger.LogInformation("Notification marked as read: {NotificationId}", command.NotificationId);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error marking notification as read");
            return Error.Failure("notification.mark_read_error", $"Failed to mark notification as read: {ex.Message}");
        }
    }
}
