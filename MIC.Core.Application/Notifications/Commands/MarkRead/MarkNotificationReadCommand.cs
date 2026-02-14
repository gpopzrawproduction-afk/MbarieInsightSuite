using MIC.Core.Application.Common.Interfaces;

namespace MIC.Core.Application.Notifications.Commands.MarkRead;

public record MarkNotificationReadCommand : ICommand<bool>
{
    public required Guid NotificationId { get; init; }
}
