namespace MIC.Core.Application.KnowledgeBase.Common;

/// <summary>
/// Data transfer object for document information
/// </summary>
public class DocumentDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public Guid UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; }
    public string? Tags { get; set; }
    public string? Description { get; set; }
    public string? DownloadUrl { get; set; }
    public int AccessCount { get; set; }
}
