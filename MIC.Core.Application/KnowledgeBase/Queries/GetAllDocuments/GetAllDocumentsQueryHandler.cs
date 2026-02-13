using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.KnowledgeBase.Common;
using ErrorOr;

namespace MIC.Core.Application.KnowledgeBase.Queries.GetAllDocuments;

public class GetAllDocumentsQueryHandler : IQueryHandler<GetAllDocumentsQuery, List<DocumentDto>>
{
    private readonly ILogger<GetAllDocumentsQueryHandler> _logger;

    public GetAllDocumentsQueryHandler(ILogger<GetAllDocumentsQueryHandler> logger)
    {
        _logger = logger;
    }

    public async Task<ErrorOr<List<DocumentDto>>> Handle(GetAllDocumentsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving all documents - Page {PageNumber}, Size {PageSize}", request.PageNumber, request.PageSize);

        try
        {
            if (request.PageNumber < 1)
            {
                return Error.Validation(code: "Document.ValidationFailed", description: "Page number must be greater than 0");
            }

            // In real implementation, query database with pagination
            // var documents = await _repository.GetPagedAsync(request.PageNumber, request.PageSize, cancellationToken);

            var documents = new List<DocumentDto>();

            _logger.LogInformation("Retrieved {DocumentCount} documents", documents.Count);
            return documents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents");
            return Error.Unexpected(code: "Document.UnexpectedError", description: ex.Message);
        }
    }
}
