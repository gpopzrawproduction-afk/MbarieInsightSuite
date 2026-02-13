using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Predictions.Common;
using ErrorOr;

namespace MIC.Core.Application.Predictions.Queries.GetMetricPredictions;

public class GetMetricPredictionsQueryHandler : IQueryHandler<GetMetricPredictionsQuery, List<PredictionDto>>
{
    private readonly ILogger<GetMetricPredictionsQueryHandler> _logger;

    public GetMetricPredictionsQueryHandler(ILogger<GetMetricPredictionsQueryHandler> logger)
    {
        _logger = logger;
    }

    public async Task<ErrorOr<List<PredictionDto>>> Handle(GetMetricPredictionsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving metric predictions from {FromDate} to {ToDate}", request.FromDate, request.ToDate);

        try
        {
            if (request.FromDate >= request.ToDate)
            {
                return Error.Validation(code: "Prediction.ValidationFailed", description: "FromDate must be before ToDate");
            }

            // In real implementation, query predictions from database or AI service
            // var predictions = await _predictionService.GetPredictionsAsync(request.MetricId, request.FromDate, request.ToDate, cancellationToken);

            var predictions = new List<PredictionDto>
            {
                new PredictionDto
                {
                    Id = Guid.NewGuid(),
                    MetricId = request.MetricId ?? 1,
                    PredictedValue = 85.5,
                    ActualValue = null,
                    Confidence = 0.92,
                    PredictionDate = DateTime.UtcNow.AddDays(1),
                    Model = "LinearRegression"
                }
            };

            _logger.LogInformation("Retrieved {PredictionCount} predictions", predictions.Count);
            return predictions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving predictions");
            return Error.Unexpected(code: "Prediction.UnexpectedError", description: ex.Message);
        }
    }
}
