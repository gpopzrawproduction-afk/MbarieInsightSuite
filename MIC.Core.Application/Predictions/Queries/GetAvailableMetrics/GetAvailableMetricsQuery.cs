using MIC.Core.Application.Common.Interfaces;

namespace MIC.Core.Application.Predictions.Queries.GetAvailableMetrics;

/// <summary>
/// Retrieves the list of all available metric names in the system.
/// </summary>
public record GetAvailableMetricsQuery : IQuery<List<string>>;
