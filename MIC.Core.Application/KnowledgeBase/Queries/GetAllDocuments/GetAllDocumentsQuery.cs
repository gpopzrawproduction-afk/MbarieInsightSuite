using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.KnowledgeBase.Common;
using ErrorOr;

namespace MIC.Core.Application.KnowledgeBase.Queries.GetAllDocuments;

/// <summary>
/// Query to retrieve all documents in the knowledge base
/// </summary>
public record GetAllDocumentsQuery : IQuery<List<DocumentDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SortBy { get; init; }
}
