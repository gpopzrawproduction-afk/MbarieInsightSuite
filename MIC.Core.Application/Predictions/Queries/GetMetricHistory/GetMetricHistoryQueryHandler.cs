using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Predictions.Queries.GetMetricHistory;

namespace MIC.Core.Application.Predictions.Queries.GetMetricHistory;

public sealed class GetMetricHistoryQueryHandler(
    IMetricsRepository metricsRepository,
    ILogger<GetMetricHistoryQueryHandler> logger) : IQueryHandler<GetMetricHistoryQuery, List<MetricDataPointDto>>
{
    public async Task<ErrorOr<List<MetricDataPointDto>>> Handle(GetMetricHistoryQuery query, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching metric history for {MetricName} from {From} to {To} with {Period} aggregation",
            query.MetricName, query.From, query.To, query.Period);

        try
        {
            // Fetch all metrics for the date range
            var allMetrics = await metricsRepository.GetMetricsByNameAndDateRangeAsync(
                query.MetricName, query.From, query.To, cancellationToken);

            if (!allMetrics.Any())
            {
                logger.LogWarning("No metrics found for {MetricName} in the specified date range", query.MetricName);
                return Error.NotFound($"metric.not_found", $"No metrics found for '{query.MetricName}'");
            }

            // Aggregate by the requested period
            var aggregatedData = AggregateMetricsByPeriod(allMetrics, query.Period);

            logger.LogInformation("Successfully fetched {Count} aggregated data points for {MetricName}",
                aggregatedData.Count, query.MetricName);

            return aggregatedData;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching metric history for {MetricName}", query.MetricName);
            return Error.Failure("metric.fetch_error", $"Failed to fetch metric history: {ex.Message}");
        }
    }

    private static List<MetricDataPointDto> AggregateMetricsByPeriod(
        IEnumerable<dynamic> metrics, 
        AggregationPeriod period)
    {
        var metricList = metrics.ToList();
        if (!metricList.Any())
            return new List<MetricDataPointDto>();

        // Group metrics by period
        var grouped = period switch
        {
            AggregationPeriod.Hourly => metricList.GroupBy(m =>
                new DateTime(m.Timestamp.Year, m.Timestamp.Month, m.Timestamp.Day, m.Timestamp.Hour, 0, 0)),
            AggregationPeriod.Daily => metricList.GroupBy(m =>
                m.Timestamp.Date),
            AggregationPeriod.Weekly => metricList.GroupBy(m =>
            {
                var culture = System.Globalization.CultureInfo.InvariantCulture;
                var date = m.Timestamp;
                var weekStart = date.AddDays(-(int)date.DayOfWeek);
                return weekStart.Date;
            }),
            AggregationPeriod.Monthly => metricList.GroupBy(m =>
                new DateTime(m.Timestamp.Year, m.Timestamp.Month, 1)),
            _ => metricList.GroupBy(m => m.Timestamp.Date)
        };

        // Aggregate each group
        return grouped
            .Select(g => new MetricDataPointDto
            {
                Timestamp = g.Key,
                Value = g.Average(m => (double)m.Value),
                IsPredicted = false
            })
            .OrderBy(p => p.Timestamp)
            .ToList();
    }
}

// Extension method to support the repository call (if not already present)
// This assumes IMetricsRepository has the method
