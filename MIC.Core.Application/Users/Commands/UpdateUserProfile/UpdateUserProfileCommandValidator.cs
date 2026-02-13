using FluentValidation;

namespace MIC.Core.Application.Users.Commands.UpdateUserProfile;

public class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required");
        RuleFor(x => x.FirstName).NotEmpty().WithMessage("First name is required").MaximumLength(100).WithMessage("First name cannot exceed 100 characters");
        RuleFor(x => x.LastName).NotEmpty().WithMessage("Last name is required").MaximumLength(100).WithMessage("Last name cannot exceed 100 characters");
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required").EmailAddress().WithMessage("Invalid email format");
        RuleFor(x => x.PhoneNumber).Matches(@"^\+?[\d\s\-\(\)]{10,}$").When(x => !string.IsNullOrEmpty(x.PhoneNumber)).WithMessage("Invalid phone number format");
    }
}
