using FluentValidation;

namespace MIC.Core.Application.Users.Commands.UpdateNotificationPreferences;

public class UpdateNotificationPreferencesCommandValidator : AbstractValidator<UpdateNotificationPreferencesCommand>
{
    public UpdateNotificationPreferencesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required");
    }
}
