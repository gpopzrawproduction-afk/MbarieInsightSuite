using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Predictions.Queries.GetMetricHistory;
using static MIC.Core.Application.Predictions.Queries.GenerateForecast.GenerateForecastQuery;

namespace MIC.Core.Application.Predictions.Queries.GenerateForecast;

public sealed class GenerateForecastQueryHandler(
    ILogger<GenerateForecastQueryHandler> logger) : IQueryHandler<GenerateForecastQuery, ForecastResultDto>
{
    private const int MinDataPointsRequired = 7;

    public async Task<ErrorOr<ForecastResultDto>> Handle(GenerateForecastQuery query, CancellationToken cancellationToken)
    {
        logger.LogInformation("Generating forecast for {MetricName} with {DataPoints} historical points, {PeriodsAhead} periods ahead",
            query.MetricName, query.HistoricalData.Count, query.PeriodsAhead);

        // Validate input
        if (query.HistoricalData.Count < MinDataPointsRequired)
        {
            logger.LogWarning("Insufficient data points for forecasting: {Count} < {Required}",
                query.HistoricalData.Count, MinDataPointsRequired);
            return Error.Validation("forecast.insufficient_data",
                $"At least {MinDataPointsRequired} data points required for forecasting");
        }

        try
        {
            var historicalValues = query.HistoricalData.OrderBy(d => d.Timestamp).ToList();
            var values = historicalValues.Select(d => d.Value).ToList();

            // Calculate linear regression parameters
            var (slope, intercept, rSquared) = CalculateLinearRegression(values);

            // Calculate standard deviation
            var mean = values.Average();
            var variance = values.Sum(v => Math.Pow(v - mean, 2)) / values.Count;
            var stdDev = Math.Sqrt(variance);

            // Determine trend direction and percentage
            var trendPercent = Math.Abs(slope / mean) * 100; // as percentage
            var trendDirection = DetermineTrend(slope, mean);

            // Generate forecast points
            var forecastPoints = GenerateForecastPoints(historicalValues, slope, intercept, stdDev, query.PeriodsAhead);

            // Calculate anomaly risk
            var nextForecastedValue = forecastPoints.First().Value;
            var anomalyRisk = CalculateAnomalyRisk(nextForecastedValue, mean, stdDev);

            var result = new ForecastResultDto
            {
                ForecastPoints = forecastPoints,
                Trend = trendDirection,
                TrendPercentage = trendPercent,
                ConfidenceScore = Math.Max(0, Math.Min(1, rSquared)), // Clamp 0-1
                AnomalyRiskScore = CalculateAnomalyRiskScore(nextForecastedValue, mean, stdDev),
                AnomalyRisk = anomalyRisk,
                ForecastedNextValue = nextForecastedValue,
                StandardDeviation = stdDev
            };

            logger.LogInformation("Forecast generated successfully: Trend={Trend}, Confidence={Confidence}, AnomalyRisk={Risk}",
                result.Trend, result.ConfidenceScore, result.AnomalyRisk);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating forecast for {MetricName}", query.MetricName);
            return Error.Failure("forecast.generation_error", $"Failed to generate forecast: {ex.Message}");
        }
    }

    /// <summary>
    /// Calculates linear regression parameters using least squares method.
    /// Returns (slope, intercept, R-squared).
    /// </summary>
    private static (double slope, double intercept, double rSquared) CalculateLinearRegression(List<double> values)
    {
        int n = values.Count;
        double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0, sumY2 = 0;

        for (int i = 0; i < n; i++)
        {
            double x = i;
            double y = values[i];
            sumX += x;
            sumY += y;
            sumXY += x * y;
            sumX2 += x * x;
            sumY2 += y * y;
        }

        double denominator = (n * sumX2 - sumX * sumX);
        
        // Handle case where all values are the same (vertical line, no slope)
        if (Math.Abs(denominator) < 1e-10)
        {
            double slope = 0;
            double intercept = sumY / n;
            return (slope, intercept, 0.0); // No correlation
        }

        double slope_calc = (n * sumXY - sumX * sumY) / denominator;
        double intercept_calc = (sumY - slope_calc * sumX) / n;

        // Calculate R-squared
        double meanY = sumY / n;
        double ssRes = 0; // Sum of squares of residuals
        double ssTot = 0; // Total sum of squares
        for (int i = 0; i < n; i++)
        {
            double predicted = slope_calc * i + intercept_calc;
            ssRes += Math.Pow(values[i] - predicted, 2);
            ssTot += Math.Pow(values[i] - meanY, 2);
        }

        double rSquared = ssTot > 0 ? 1 - (ssRes / ssTot) : 0;

        return (slope_calc, intercept_calc, rSquared);
    }

    /// <summary>
    /// Determines trend direction based on slope and mean.
    /// </summary>
    private static TrendDirection DetermineTrend(double slope, double mean)
    {
        double threshold = 0.02 * Math.Abs(mean);

        if (slope > threshold)
            return TrendDirection.Upward;
        if (slope < -threshold)
            return TrendDirection.Downward;
        return TrendDirection.Stable;
    }

    /// <summary>
    /// Generates forecast points for the requested number of periods ahead.
    /// </summary>
    private static List<MetricDataPointDto> GenerateForecastPoints(
        List<MetricDataPointDto> historical,
        double slope,
        double intercept,
        double stdDev,
        int periodsAhead)
    {
        var forecastPoints = new List<MetricDataPointDto>();
        var lastTimestamp = historical.Last().Timestamp;
        var interval = CalculateInterval(historical);

        for (int i = 1; i <= periodsAhead; i++)
        {
            int xIndex = historical.Count + i - 1;
            double predictedValue = slope * xIndex + intercept;
            var timestamp = lastTimestamp.Add(TimeSpan.FromSeconds(interval * i));

            // Calculate confidence bounds
            double lowerBound = predictedValue - (1.5 * stdDev);
            double upperBound = predictedValue + (1.5 * stdDev);

            forecastPoints.Add(new MetricDataPointDto
            {
                Timestamp = timestamp,
                Value = Math.Max(0, predictedValue), // Prevent negative values for metrics
                LowerBound = Math.Max(0, lowerBound),
                UpperBound = upperBound,
                IsPredicted = true
            });
        }

        return forecastPoints;
    }

    /// <summary>
    /// Calculates the average interval (in seconds) between data points.
    /// </summary>
    private static double CalculateInterval(List<MetricDataPointDto> data)
    {
        if (data.Count < 2)
            return 3600; // Default to 1 hour

        double totalSeconds = (data.Last().Timestamp - data.First().Timestamp).TotalSeconds;
        return totalSeconds / (data.Count - 1);
    }

    /// <summary>
    /// Determines anomaly risk level based on how far the value is from the mean.
    /// </summary>
    private static AnomalyRisk CalculateAnomalyRisk(double value, double mean, double stdDev)
    {
        if (stdDev == 0)
            return AnomalyRisk.Low;

        double zScore = Math.Abs(value - mean) / stdDev;

        if (zScore <= 1.0)
            return AnomalyRisk.Low;
        if (zScore <= 2.0)
            return AnomalyRisk.Medium;
        return AnomalyRisk.High;
    }

    /// <summary>
    /// Calculates anomaly risk score (0.0 to 1.0).
    /// </summary>
    private static double CalculateAnomalyRiskScore(double value, double mean, double stdDev)
    {
        if (stdDev == 0)
            return 0.0;

        double zScore = Math.Abs(value - mean) / stdDev;
        // Sigmoid-like function: 0 at zScore=0, approaches 1 as zScore increases
        return 1.0 / (1.0 + Math.Exp(-zScore + 1.0));
    }
}
