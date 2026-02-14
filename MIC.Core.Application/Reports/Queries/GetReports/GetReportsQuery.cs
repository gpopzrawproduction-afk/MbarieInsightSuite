using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Reports.Commands.GenerateReport;

namespace MIC.Core.Application.Reports.Queries.GetReports;

/// <summary>
/// Query to retrieve paginated list of generated reports.
/// </summary>
public record GetReportsQuery : IQuery<List<ReportSummaryDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public record ReportSummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public ReportType Type { get; init; }
    public ReportFormat Format { get; init; }
    public DateTime GeneratedAt { get; init; }
    public long FileSizeBytes { get; init; }
    public string FileSizeFormatted { get; init; } = string.Empty;
    public string OutputFilePath { get; init; } = string.Empty;
    public bool FileExists { get; init; }
}
