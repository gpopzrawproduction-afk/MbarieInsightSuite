namespace MIC.Core.Application.Reports.Common;

/// <summary>
/// Data transfer object for report information
/// </summary>
public class ReportDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string Format { get; set; } = "PDF";
    public DateTime GeneratedAt { get; set; }
    public string? Content { get; set; }
    public string? DownloadUrl { get; set; }
}
