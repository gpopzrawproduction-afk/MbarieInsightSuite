namespace MIC.Core.Application.Predictions.Common;

/// <summary>
/// Data transfer object for prediction information
/// </summary>
public class PredictionDto
{
    public Guid Id { get; set; }
    public int MetricId { get; set; }
    public double PredictedValue { get; set; }
    public double? ActualValue { get; set; }
    public double Confidence { get; set; }
    public DateTime PredictionDate { get; set; }
    public string Model { get; set; } = string.Empty;
}
