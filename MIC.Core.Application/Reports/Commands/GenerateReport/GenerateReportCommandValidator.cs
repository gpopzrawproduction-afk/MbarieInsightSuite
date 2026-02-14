using FluentValidation;
using MIC.Core.Application.Reports.Commands.GenerateReport;

namespace MIC.Core.Application.Reports.Commands.GenerateReport;

public sealed class GenerateReportCommandValidator : AbstractValidator<GenerateReportCommand>
{
    public GenerateReportCommandValidator()
    {
        RuleFor(c => c.FromDate)
            .LessThan(c => c.ToDate)
            .WithMessage("'From date' must be before 'To date'");

        RuleFor(c => c.ToDate)
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .WithMessage("'To date' must not be in the future beyond 1 day");

        RuleFor(c => new { c.FromDate, c.ToDate })
            .Must(d => (d.ToDate - d.FromDate).TotalDays <= 365)
            .WithMessage("Date range must not exceed 365 days");

        RuleFor(c => c.Type)
            .IsInEnum()
            .WithMessage("Invalid report type");

        RuleFor(c => c.Format)
            .IsInEnum()
            .WithMessage("Invalid report format");
    }
}
