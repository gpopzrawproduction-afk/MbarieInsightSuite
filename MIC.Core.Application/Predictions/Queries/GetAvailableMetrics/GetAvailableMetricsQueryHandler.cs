using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;

namespace MIC.Core.Application.Predictions.Queries.GetAvailableMetrics;

public sealed class GetAvailableMetricsQueryHandler(
    IMetricsRepository metricsRepository,
    ILogger<GetAvailableMetricsQueryHandler> logger) : IQueryHandler<GetAvailableMetricsQuery, List<string>>
{
    public async Task<ErrorOr<List<string>>> Handle(GetAvailableMetricsQuery query, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching available metrics");

        try
        {
            var metrics = await metricsRepository.GetDistinctMetricNamesAsync(cancellationToken);
            
            logger.LogInformation("Found {Count} available metrics", metrics.Count);
            
            return metrics.OrderBy(m => m).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching available metrics");
            return Error.Failure("metrics.fetch_error", "Failed to fetch available metrics");
        }
    }
}
