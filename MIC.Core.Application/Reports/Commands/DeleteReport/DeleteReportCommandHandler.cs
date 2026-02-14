using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;

namespace MIC.Core.Application.Reports.Commands.DeleteReport;

public sealed class DeleteReportCommandHandler(
    IReportRepository reportRepository,
    ILogger<DeleteReportCommandHandler> logger) : ICommandHandler<DeleteReportCommand, bool>
{
    public async Task<ErrorOr<bool>> Handle(DeleteReportCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting report {ReportId}", command.ReportId);

        try
        {
            // Fetch report from DB
            var report = await reportRepository.GetByIdAsync(command.ReportId, cancellationToken);

            if (report == null)
            {
                logger.LogWarning("Report not found: {ReportId}", command.ReportId);
                return Error.NotFound("report.not_found", $"Report with ID {command.ReportId} not found");
            }

            // Delete file if it exists
            try
            {
                var filePath = report.GetType().GetProperty("OutputFilePath")?.GetValue(report) as string;
                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    File.Delete(filePath);
                    logger.LogInformation("Report file deleted: {FilePath}", filePath);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete report file");
                // Continue with DB deletion even if file deletion fails
            }

            // Delete from DB
            await reportRepository.DeleteAsync(command.ReportId, cancellationToken);

            logger.LogInformation("Report deleted successfully: {ReportId}", command.ReportId);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting report");
            return Error.Failure("report.deletion_error", $"Failed to delete report: {ex.Message}");
        }
    }
}
