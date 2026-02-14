using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Reports.Commands.GenerateReport;

namespace MIC.Core.Application.Reports.Queries.GetReports;

public sealed class GetReportsQueryHandler(
    IReportRepository reportRepository,
    ILogger<GetReportsQueryHandler> logger) : IQueryHandler<GetReportsQuery, List<ReportSummaryDto>>
{
    public async Task<ErrorOr<List<ReportSummaryDto>>> Handle(GetReportsQuery query, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching reports: page {PageNumber}, size {PageSize}", query.PageNumber, query.PageSize);

        try
        {
            var reports = await reportRepository.GetPagedAsync(query.PageNumber, query.PageSize, cancellationToken);

            var summaries = reports.Select(r => new ReportSummaryDto
            {
                Id = r.GetType().GetProperty("Id")?.GetValue(r) as Guid? ?? Guid.Empty,
                Name = r.GetType().GetProperty("FileName")?.GetValue(r) as string ?? string.Empty,
                Type = (ReportType)(r.GetType().GetProperty("Type")?.GetValue(r) ?? ReportType.AlertSummary),
                Format = (ReportFormat)(r.GetType().GetProperty("Format")?.GetValue(r) ?? ReportFormat.PDF),
                GeneratedAt = r.GetType().GetProperty("GeneratedAt")?.GetValue(r) as DateTime? ?? DateTime.UtcNow,
                FileSizeBytes = r.GetType().GetProperty("FileSizeBytes")?.GetValue(r) as long? ?? 0,
                FileSizeFormatted = FormatFileSize(r.GetType().GetProperty("FileSizeBytes")?.GetValue(r) as long? ?? 0),
                OutputFilePath = r.GetType().GetProperty("OutputFilePath")?.GetValue(r) as string ?? string.Empty,
                FileExists = File.Exists(r.GetType().GetProperty("OutputFilePath")?.GetValue(r) as string ?? string.Empty)
            }).ToList();

            logger.LogInformation("Fetched {Count} reports", summaries.Count);

            return summaries;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching reports");
            return Error.Failure("report.fetch_error", $"Failed to fetch reports: {ex.Message}");
        }
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:F2} {sizes[order]}";
    }
}
