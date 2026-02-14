using MIC.Core.Application.Common.Interfaces;

namespace MIC.Core.Application.Predictions.Queries.GetMetricHistory;

/// <summary>
/// Retrieves historical metric data for a date range with optional aggregation.
/// </summary>
public record GetMetricHistoryQuery : IQuery<List<MetricDataPointDto>>
{
    public required string MetricName { get; init; }
    public DateTime From { get; init; }
    public DateTime To { get; init; }
    public AggregationPeriod Period { get; init; } = AggregationPeriod.Daily;
}

public enum AggregationPeriod
{
    Hourly,
    Daily,
    Weekly,
    Monthly
}

public record MetricDataPointDto
{
    public DateTime Timestamp { get; init; }
    public double Value { get; init; }
    public double? LowerBound { get; init; }
    public double? UpperBound { get; init; }
    public bool IsPredicted { get; init; }
}
