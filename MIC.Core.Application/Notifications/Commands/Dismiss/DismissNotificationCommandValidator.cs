using FluentValidation;

namespace MIC.Core.Application.Notifications.Commands.Dismiss;

public sealed class DismissNotificationCommandValidator : AbstractValidator<DismissNotificationCommand>
{
    public DismissNotificationCommandValidator()
    {
        RuleFor(c => c.NotificationId)
            .NotEmpty()
            .WithMessage("Notification ID is required");
    }
}
