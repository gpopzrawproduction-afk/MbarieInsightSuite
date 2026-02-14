using MIC.Core.Application.Common.Interfaces;
using MetricDataPointDto = MIC.Core.Application.Predictions.Queries.GetMetricHistory.MetricDataPointDto;

namespace MIC.Core.Application.Predictions.Queries.GenerateForecast;

/// <summary>
/// Generates a forecast for a metric based on historical data using linear regression.
/// </summary>
public record GenerateForecastQuery : IQuery<ForecastResultDto>
{
    public required string MetricName { get; init; }
    public required List<MetricDataPointDto> HistoricalData { get; init; }
    public int PeriodsAhead { get; init; } = 7;
}

public record ForecastResultDto
{
    public List<MetricDataPointDto> ForecastPoints { get; init; } = new();
    public TrendDirection Trend { get; init; }
    public double TrendPercentage { get; init; }
    public double ConfidenceScore { get; init; }
    public double AnomalyRiskScore { get; init; }
    public AnomalyRisk AnomalyRisk { get; init; }
    public double ForecastedNextValue { get; init; }
    public double StandardDeviation { get; init; }
}

public enum TrendDirection
{
    Upward,
    Downward,
    Stable
}

public enum AnomalyRisk
{
    Low,
    Medium,
    High
}
