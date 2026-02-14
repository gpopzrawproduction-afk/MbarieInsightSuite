using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;

namespace MIC.Core.Application.Notifications.Commands.Dismiss;

public sealed class DismissNotificationCommandHandler(
    INotificationRepository notificationRepository,
    ILogger<DismissNotificationCommandHandler> logger) : ICommandHandler<DismissNotificationCommand, bool>
{
    public async Task<ErrorOr<bool>> Handle(DismissNotificationCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation("Dismissing notification: {NotificationId}", command.NotificationId);

        try
        {
            var notification = await notificationRepository.GetByIdAsync(command.NotificationId, cancellationToken);

            if (notification == null)
            {
                logger.LogWarning("Notification not found: {NotificationId}", command.NotificationId);
                return Error.NotFound("notification.not_found", "Notification not found");
            }

            notification.Dismiss();
            await notificationRepository.UpdateAsync(notification, cancellationToken);

            logger.LogInformation("Notification dismissed: {NotificationId}", command.NotificationId);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error dismissing notification");
            return Error.Failure("notification.dismiss_error", $"Failed to dismiss notification: {ex.Message}");
        }
    }
}
