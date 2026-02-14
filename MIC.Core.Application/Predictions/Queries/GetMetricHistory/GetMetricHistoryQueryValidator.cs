using FluentValidation;
using MIC.Core.Application.Predictions.Queries.GetMetricHistory;

namespace MIC.Core.Application.Predictions.Queries.GetMetricHistory;

public sealed class GetMetricHistoryQueryValidator : AbstractValidator<GetMetricHistoryQuery>
{
    public GetMetricHistoryQueryValidator()
    {
        RuleFor(q => q.MetricName)
            .NotEmpty()
            .WithMessage("Metric name is required")
            .MaximumLength(200)
            .WithMessage("Metric name must not exceed 200 characters");

        RuleFor(q => q.From)
            .LessThan(q => q.To)
            .WithMessage("'From' date must be before 'To' date");

        RuleFor(q => q.To)
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .WithMessage("'To' date must not be in the future beyond 1 day");

        RuleFor(q => q.Period)
            .IsInEnum()
            .WithMessage("Invalid aggregation period");
    }
}
