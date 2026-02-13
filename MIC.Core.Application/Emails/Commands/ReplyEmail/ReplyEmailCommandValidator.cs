using FluentValidation;

namespace MIC.Core.Application.Emails.Commands.ReplyEmail;

/// <summary>
/// Validates ReplyEmailCommand
/// </summary>
public class ReplyEmailCommandValidator : AbstractValidator<ReplyEmailCommand>
{
    public ReplyEmailCommandValidator()
    {
        RuleFor(x => x.FromEmailAccountId)
            .NotEmpty()
            .WithMessage("Email account is required");

        RuleFor(x => x.OriginalMessageId)
            .NotEmpty()
            .WithMessage("Original message ID is required");

        RuleFor(x => x.ReplyToAddress)
            .NotEmpty()
            .WithMessage("Reply-to address is required")
            .EmailAddress()
            .WithMessage("Invalid reply-to address");

        RuleFor(x => x.Body)
            .NotEmpty()
            .WithMessage("Reply body cannot be empty");

        RuleForEach(x => x.CcAddresses)
            .EmailAddress()
            .WithMessage("Invalid email address in Cc field");
    }
}

