using MIC.Core.Application.Common.Interfaces;

namespace MIC.Core.Application.Reports.Commands.DeleteReport;

/// <summary>
/// Command to delete a report and its associated file.
/// </summary>
public record DeleteReportCommand : ICommand<bool>
{
    public required Guid ReportId { get; init; }
}
