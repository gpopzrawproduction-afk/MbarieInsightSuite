using MIC.Core.Application.Common.Interfaces;

namespace MIC.Core.Application.Notifications.Commands.Dismiss;

public record DismissNotificationCommand : ICommand<bool>
{
    public required Guid NotificationId { get; init; }
}
