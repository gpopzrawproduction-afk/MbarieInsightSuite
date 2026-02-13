using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using ErrorOr;

namespace MIC.Core.Application.KnowledgeBase.Commands.DeleteDocument;

public class DeleteDocumentCommandHandler : ICommandHandler<DeleteDocumentCommand, bool>
{
    private readonly ILogger<DeleteDocumentCommandHandler> _logger;

    public DeleteDocumentCommandHandler(ILogger<DeleteDocumentCommandHandler> logger)
    {
        _logger = logger;
    }

    public async Task<ErrorOr<bool>> Handle(DeleteDocumentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting document: {DocumentId} by user {UserId}", request.DocumentId, request.UserId);

        try
        {
            var validator = new DeleteDocumentCommandValidator();
            var validationResult = validator.Validate(request);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Error.Validation(code: "Document.ValidationFailed", description: errors);
            }

            // In real implementation, soft delete from database/storage
            // await _repository.DeleteAsync(request.DocumentId, cancellationToken);

            _logger.LogInformation("Document deleted successfully: {DocumentId}", request.DocumentId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document");
            return Error.Unexpected(code: "Document.UnexpectedError", description: ex.Message);
        }
    }
}
