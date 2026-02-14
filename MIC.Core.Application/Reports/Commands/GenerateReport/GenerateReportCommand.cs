using MIC.Core.Application.Common.Interfaces;

namespace MIC.Core.Application.Reports.Commands.GenerateReport;

/// <summary>
/// Command to generate a business intelligence report with specified parameters.
/// </summary>
public record GenerateReportCommand : ICommand<ReportGeneratedDto>
{
    public required ReportType Type { get; init; }
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
    public ReportFormat Format { get; init; } = ReportFormat.PDF;
    public bool IncludeCharts { get; init; } = true;
    public bool IncludeRawData { get; init; } = false;
    public bool EmailToSelf { get; init; } = false;
}

public enum ReportType
{
    AlertSummary,
    EmailActivity,
    MetricsTrend,
    AiChatSummary,
    FullDashboard
}

public enum ReportFormat { PDF, XLSX, CSV }

public record ReportGeneratedDto
{
    public Guid ReportId { get; init; }
    public string OutputFilePath { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public DateTime GeneratedAt { get; init; }
    public ReportFormat Format { get; init; }
    public ReportType Type { get; init; }
}
