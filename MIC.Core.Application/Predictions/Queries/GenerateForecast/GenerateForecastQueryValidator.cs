using FluentValidation;
using MIC.Core.Application.Predictions.Queries.GenerateForecast;

namespace MIC.Core.Application.Predictions.Queries.GenerateForecast;

public sealed class GenerateForecastQueryValidator : AbstractValidator<GenerateForecastQuery>
{
    public GenerateForecastQueryValidator()
    {
        RuleFor(q => q.MetricName)
            .NotEmpty()
            .WithMessage("Metric name is required")
            .MaximumLength(200)
            .WithMessage("Metric name must not exceed 200 characters");

        RuleFor(q => q.HistoricalData)
            .NotEmpty()
            .WithMessage("Historical data is required")
            .Must(d => d.Count >= 7)
            .WithMessage("At least 7 historical data points required for valid forecast");

        RuleFor(q => q.PeriodsAhead)
            .GreaterThan(0)
            .WithMessage("Periods ahead must be greater than 0")
            .LessThanOrEqualTo(365)
            .WithMessage("Periods ahead must not exceed 365");
    }
}
