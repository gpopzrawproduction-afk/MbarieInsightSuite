using MIC.Core.Application.Common.Interfaces;
using ErrorOr;

namespace MIC.Core.Application.KnowledgeBase.Commands.DeleteDocument;

/// <summary>
/// Command to delete a document from the knowledge base
/// </summary>
public record DeleteDocumentCommand : ICommand<bool>
{
    public Guid DocumentId { get; init; }
    public Guid UserId { get; init; }
}
