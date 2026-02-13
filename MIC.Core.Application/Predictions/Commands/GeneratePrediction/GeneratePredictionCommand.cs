using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Predictions.Common;
using ErrorOr;

namespace MIC.Core.Application.Predictions.Commands.GeneratePrediction;

/// <summary>
/// Command to generate new predictions
/// </summary>
public record GeneratePredictionCommand : ICommand<PredictionDto>
{
    public int MetricId { get; init; }
    public string? ModelName { get; init; }
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
}
