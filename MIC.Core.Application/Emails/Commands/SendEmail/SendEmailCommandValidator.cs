using FluentValidation;

namespace MIC.Core.Application.Emails.Commands.SendEmail;

public class SendEmailCommandValidator : AbstractValidator<SendEmailCommand>
{
    public SendEmailCommandValidator()
    {
        RuleFor(x => x.FromEmailAccountId).NotEmpty().WithMessage("Email account is required");
        RuleFor(x => x.ToAddresses)
            .NotNull().WithMessage("Recipients list cannot be null")
            .Must(x => x.Any()).When(x => x.CcAddresses.Count == 0 && x.BccAddresses.Count == 0)
            .WithMessage("At least one recipient (To, Cc, or Bcc) is required");
        RuleForEach(x => x.ToAddresses).EmailAddress().WithMessage("Invalid email address in To field");
        RuleForEach(x => x.CcAddresses).EmailAddress().WithMessage("Invalid email address in Cc field");
        RuleForEach(x => x.BccAddresses).EmailAddress().WithMessage("Invalid email address in Bcc field");
        RuleFor(x => x.Subject).NotEmpty().WithMessage("Subject is required").MaximumLength(998).WithMessage("Subject cannot exceed 998 characters");
        RuleFor(x => x.Body).NotEmpty().WithMessage("Email body cannot be empty").MaximumLength(10_000_000).WithMessage("Email body is too large");
    }
}

