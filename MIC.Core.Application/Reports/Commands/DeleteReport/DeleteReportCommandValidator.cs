using FluentValidation;
using MIC.Core.Application.Reports.Commands.DeleteReport;

namespace MIC.Core.Application.Reports.Commands.DeleteReport;

public sealed class DeleteReportCommandValidator : AbstractValidator<DeleteReportCommand>
{
    public DeleteReportCommandValidator()
    {
        RuleFor(c => c.ReportId)
            .NotEmpty()
            .WithMessage("Report ID is required");
    }
}
