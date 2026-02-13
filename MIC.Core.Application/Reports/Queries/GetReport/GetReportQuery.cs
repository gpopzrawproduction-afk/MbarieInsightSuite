using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Reports.Common;
using ErrorOr;

namespace MIC.Core.Application.Reports.Queries.GetReport;

/// <summary>
/// Query to retrieve an analytics report
/// </summary>
public record GetReportQuery : IQuery<ReportDto>
{
    public string ReportType { get; init; } = string.Empty;
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
    public string? Format { get; init; }
}
