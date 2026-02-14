using FluentValidation;

namespace MIC.Core.Application.Notifications.Commands.MarkRead;

public sealed class MarkNotificationReadCommandValidator : AbstractValidator<MarkNotificationReadCommand>
{
    public MarkNotificationReadCommandValidator()
    {
        RuleFor(c => c.NotificationId)
            .NotEmpty()
            .WithMessage("Notification ID is required");
    }
}
