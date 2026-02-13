using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.KnowledgeBase.Common;
using ErrorOr;

namespace MIC.Core.Application.KnowledgeBase.Queries.SearchDocuments;

public class SearchDocumentsQueryHandler : IQueryHandler<SearchDocumentsQuery, List<DocumentDto>>
{
    private readonly ILogger<SearchDocumentsQueryHandler> _logger;

    public SearchDocumentsQueryHandler(ILogger<SearchDocumentsQueryHandler> logger)
    {
        _logger = logger;
    }

    public async Task<ErrorOr<List<DocumentDto>>> Handle(SearchDocumentsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Searching documents with query: {SearchTerm}", request.SearchTerm);

        try
        {
            // In real implementation, query database with full-text search
            // var documents = await _repository.SearchAsync(request.SearchTerm, request.Tags, request.FromDate, request.ToDate, cancellationToken);

            var documents = new List<DocumentDto>();

            _logger.LogInformation("Found {DocumentCount} documents", documents.Count);
            return documents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching documents");
            return Error.Unexpected(code: "Document.SearchError", description: ex.Message);
        }
    }
}
