using FluentValidation;

namespace MIC.Core.Application.Emails.Commands.DeleteEmail;

public class DeleteEmailCommandValidator : AbstractValidator<DeleteEmailCommand>
{
    public DeleteEmailCommandValidator()
    {
        RuleFor(x => x.EmailId).NotEmpty().WithMessage("Email ID is required");
        RuleFor(x => x.EmailAccountId).NotEmpty().WithMessage("Email account ID is required");
    }
}

public class MoveEmailCommandValidator : AbstractValidator<MoveEmailCommand>
{
    public MoveEmailCommandValidator()
    {
        RuleFor(x => x.EmailId).NotEmpty().WithMessage("Email ID is required");
        RuleFor(x => x.EmailAccountId).NotEmpty().WithMessage("Email account ID is required");
        RuleFor(x => x.TargetFolderName).NotEmpty().WithMessage("Target folder name is required");
        RuleFor(x => x.TargetFolderName).Must(f => new[] { "Inbox", "Archive", "Trash", "Sent", "Drafts", "Spam" }.Contains(f, StringComparer.OrdinalIgnoreCase)).WithMessage("Invalid target folder");
    }
}

public class MarkEmailReadCommandValidator : AbstractValidator<MarkEmailReadCommand>
{
    public MarkEmailReadCommandValidator()
    {
        RuleFor(x => x.EmailId).NotEmpty().WithMessage("Email ID is required");
    }
}

