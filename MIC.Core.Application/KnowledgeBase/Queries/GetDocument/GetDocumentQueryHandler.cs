using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.KnowledgeBase.Common;
using ErrorOr;

namespace MIC.Core.Application.KnowledgeBase.Queries.GetDocument;

public class GetDocumentQueryHandler : IQueryHandler<GetDocumentQuery, DocumentDto>
{
    private readonly ILogger<GetDocumentQueryHandler> _logger;

    public GetDocumentQueryHandler(ILogger<GetDocumentQueryHandler> logger)
    {
        _logger = logger;
    }

    public async Task<ErrorOr<DocumentDto>> Handle(GetDocumentQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving document: {DocumentId} by user {UserId}", request.DocumentId, request.UserId);

        try
        {
            if (request.DocumentId == Guid.Empty)
            {
                return Error.Validation(code: "Document.ValidationFailed", description: "Document ID is required");
            }

            // In real implementation, retrieve from database
            // var document = await _repository.GetByIdAsync(request.DocumentId, cancellationToken);
            // if (document == null) return Error.NotFound(...);

            var documentDto = new DocumentDto
            {
                Id = request.DocumentId,
                FileName = "Sample Document",
                FileSize = 1024,
                ContentType = "application/pdf",
                UploadedBy = request.UserId,
                UploadedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Document retrieved successfully: {DocumentId}", request.DocumentId);
            return documentDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document");
            return Error.Unexpected(code: "Document.UnexpectedError", description: ex.Message);
        }
    }
}
