using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.KnowledgeBase.Common;
using ErrorOr;

namespace MIC.Core.Application.KnowledgeBase.Queries.SearchDocuments;

/// <summary>
/// Query to search documents in the knowledge base
/// </summary>
public record SearchDocumentsQuery : IQuery<List<DocumentDto>>
{
    public string? SearchTerm { get; init; }
    public string? Tags { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
