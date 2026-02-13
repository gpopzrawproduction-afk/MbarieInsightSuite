using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Predictions.Common;
using ErrorOr;

namespace MIC.Core.Application.Predictions.Queries.GetMetricPredictions;

/// <summary>
/// Query to retrieve metric predictions
/// </summary>
public record GetMetricPredictionsQuery : IQuery<List<PredictionDto>>
{
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
    public int? MetricId { get; init; }
    public double? MinConfidence { get; init; }
}
