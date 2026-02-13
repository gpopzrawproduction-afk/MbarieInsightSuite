using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Predictions.Common;
using ErrorOr;

namespace MIC.Core.Application.Predictions.Commands.GeneratePrediction;

public class GeneratePredictionCommandHandler : ICommandHandler<GeneratePredictionCommand, PredictionDto>
{
    private readonly ILogger<GeneratePredictionCommandHandler> _logger;

    public GeneratePredictionCommandHandler(ILogger<GeneratePredictionCommandHandler> logger)
    {
        _logger = logger;
    }

    public async Task<ErrorOr<PredictionDto>> Handle(GeneratePredictionCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating prediction for metric {MetricId} using model {Model}", request.MetricId, request.ModelName ?? "Default");

        try
        {
            var validator = new GeneratePredictionCommandValidator();
            var validationResult = validator.Validate(request);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Error.Validation(code: "Prediction.ValidationFailed", description: errors);
            }

            // In real implementation, call ML service to generate prediction
            // var prediction = await _predictionService.GeneratePredictionAsync(request.MetricId, request.ModelName, request.FromDate, request.ToDate, cancellationToken);

            var predictionDto = new PredictionDto
            {
                Id = Guid.NewGuid(),
                MetricId = request.MetricId,
                PredictedValue = 92.5,
                Confidence = 0.88,
                PredictionDate = DateTime.UtcNow.AddDays(1),
                Model = request.ModelName ?? "LinearRegression"
            };

            _logger.LogInformation("Prediction generated successfully: {PredictionId}", predictionDto.Id);
            return predictionDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating prediction");
            return Error.Unexpected(code: "Prediction.UnexpectedError", description: ex.Message);
        }
    }
}
