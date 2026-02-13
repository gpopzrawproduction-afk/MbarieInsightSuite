using FluentValidation;

namespace MIC.Core.Application.KnowledgeBase.Commands.UploadDocument;

public class UploadDocumentCommandValidator : AbstractValidator<UploadDocumentCommand>
{
    public UploadDocumentCommandValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().WithMessage("File name is required").MaximumLength(255).WithMessage("File name cannot exceed 255 characters");
        RuleFor(x => x.Content).NotEmpty().WithMessage("File content is required");
        RuleFor(x => x.FileSize).GreaterThan(0).WithMessage("File size must be greater than 0").LessThanOrEqualTo(52428800).WithMessage("File size cannot exceed 50 MB");
        RuleFor(x => x.ContentType).NotEmpty().WithMessage("Content type is required");
        RuleFor(x => x.UserId).NotEqual(Guid.Empty).WithMessage("User ID is required");
    }
}
