using FluentValidation;

namespace MIC.Core.Application.KnowledgeBase.Commands.DeleteDocument;

public class DeleteDocumentCommandValidator : AbstractValidator<DeleteDocumentCommand>
{
    public DeleteDocumentCommandValidator()
    {
        RuleFor(x => x.DocumentId).NotEqual(Guid.Empty).WithMessage("Document ID is required");
        RuleFor(x => x.UserId).NotEqual(Guid.Empty).WithMessage("User ID is required");
    }
}
