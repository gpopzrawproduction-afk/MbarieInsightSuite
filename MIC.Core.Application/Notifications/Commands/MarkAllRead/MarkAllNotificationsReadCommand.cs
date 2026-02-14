using MIC.Core.Application.Common.Interfaces;

namespace MIC.Core.Application.Notifications.Commands.MarkAllRead;

public record MarkAllNotificationsReadCommand : ICommand<bool>
{
    public required string UserId { get; init; }
}
