using FluentValidation;

namespace MIC.Core.Application.Predictions.Commands.GeneratePrediction;

public class GeneratePredictionCommandValidator : AbstractValidator<GeneratePredictionCommand>
{
    public GeneratePredictionCommandValidator()
    {
        RuleFor(x => x.MetricId).GreaterThan(0).WithMessage("Metric ID must be greater than 0");
        RuleFor(x => x.FromDate).NotEqual(default(DateTime)).WithMessage("FromDate is required");
        RuleFor(x => x.ToDate).NotEqual(default(DateTime)).WithMessage("ToDate is required");
        RuleFor(x => x.ToDate).GreaterThan(x => x.FromDate).WithMessage("ToDate must be after FromDate");
    }
}
