using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;

namespace MIC.Core.Application.Notifications.Commands.MarkAllRead;

public sealed class MarkAllNotificationsReadCommandHandler(
    INotificationRepository notificationRepository,
    ILogger<MarkAllNotificationsReadCommandHandler> logger) : ICommandHandler<MarkAllNotificationsReadCommand, bool>
{
    public async Task<ErrorOr<bool>> Handle(MarkAllNotificationsReadCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation("Marking all notifications as read for user: {UserId}", command.UserId);

        try
        {
            await notificationRepository.MarkAllAsReadAsync(command.UserId, cancellationToken);

            logger.LogInformation("All notifications marked as read for user: {UserId}", command.UserId);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error marking all notifications as read");
            return Error.Failure("notification.mark_all_read_error", $"Failed to mark all notifications as read: {ex.Message}");
        }
    }
}
