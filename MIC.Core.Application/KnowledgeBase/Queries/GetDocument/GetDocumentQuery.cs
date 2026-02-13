using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.KnowledgeBase.Common;
using ErrorOr;

namespace MIC.Core.Application.KnowledgeBase.Queries.GetDocument;

/// <summary>
/// Query to retrieve a specific document from the knowledge base
/// </summary>
public record GetDocumentQuery : IQuery<DocumentDto>
{
    public Guid DocumentId { get; init; }
    public Guid UserId { get; init; }
}
