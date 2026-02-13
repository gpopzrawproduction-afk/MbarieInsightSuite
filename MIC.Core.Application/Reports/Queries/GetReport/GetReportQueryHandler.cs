using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Reports.Common;
using ErrorOr;

namespace MIC.Core.Application.Reports.Queries.GetReport;

public class GetReportQueryHandler : IQueryHandler<GetReportQuery, ReportDto>
{
    private readonly ILogger<GetReportQueryHandler> _logger;

    public GetReportQueryHandler(ILogger<GetReportQueryHandler> logger)
    {
        _logger = logger;
    }

    public async Task<ErrorOr<ReportDto>> Handle(GetReportQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating {ReportType} report from {FromDate} to {ToDate}", request.ReportType, request.FromDate, request.ToDate);

        try
        {
            if (string.IsNullOrEmpty(request.ReportType))
            {
                return Error.Validation(code: "Report.ValidationFailed", description: "ReportType is required");
            }

            if (request.FromDate >= request.ToDate)
            {
                return Error.Validation(code: "Report.ValidationFailed", description: "FromDate must be before ToDate");
            }

            // In real implementation, generate report from data
            var reportDto = new ReportDto
            {
                Id = Guid.NewGuid(),
                Title = $"{request.ReportType} Report",
                ReportType = request.ReportType,
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                Format = request.Format ?? "PDF",
                GeneratedAt = DateTime.UtcNow,
                Content = GenerateReportContent(request.ReportType)
            };

            _logger.LogInformation("Report generated successfully: {ReportId}", reportDto.Id);
            return reportDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report");
            return Error.Unexpected(code: "Report.UnexpectedError", description: ex.Message);
        }
    }

    private string GenerateReportContent(string reportType) =>
        reportType.ToLower() switch
        {
            "summary" => "Summary Report: Key metrics and trends",
            "detailed" => "Detailed Report: Comprehensive analysis and insights",
            "executive" => "Executive Report: High-level overview for stakeholders",
            _ => "Custom Report: Data analysis based on selected parameters"
        };
}
