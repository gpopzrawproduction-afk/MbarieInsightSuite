            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var filePath = Path.Combine(documentsPath, fileName);

            await File.WriteAllTextAsync(filePath, csv.ToString());

            _logger.LogInformation("Forecast exported to {Path}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting forecast");
            HasError = true;
            ErrorMessage = "Failed to export forecast data";
        }
    }

    private (DateTime from, DateTime to) GetDateRange()
    {
        var to = DateTime.UtcNow;
        var from = SelectedHistoryRange switch
        {
            HistoryRange.Hours24 => to.AddHours(-24),
            HistoryRange.Days7 => to.AddDays(-7),
            HistoryRange.Days30 => to.AddDays(-30),
            HistoryRange.Days90 => to.AddDays(-90),
            _ => to.AddDays(-30)
        };

        return (from, to);
    }

    private void PopulateInsightCards(ForecastResultDto forecast)
    {
        Trend = forecast.Trend;
        TrendLabel = forecast.Trend switch
        {
            TrendDirection.Upward => "UPWARD",
            TrendDirection.Downward => "DOWNWARD",
            _ => "STABLE"
        };
        TrendIcon = forecast.Trend switch
        {
            TrendDirection.Upward => "?",
            TrendDirection.Downward => "?",
            _ => "?"
        };
        TrendColor = forecast.Trend switch
        {
            TrendDirection.Upward => "#00FF6A",
            TrendDirection.Downward => "#FF0055",
            _ => "#00D9FF"
        };

        ForecastedNextValue = forecast.ForecastedNextValue;
        ForecastedNextValueFormatted = ForecastedNextValue.ToString("F2");

        ConfidenceScore = forecast.ConfidenceScore;
        ConfidencePercent = (int)(forecast.ConfidenceScore * 100);

        AnomalyRisk = forecast.AnomalyRisk;
        AnomalyRiskLabel = forecast.AnomalyRisk switch
        {
            AnomalyRisk.Low => "LOW",
            AnomalyRisk.Medium => "MEDIUM",
            _ => "HIGH"
        };
        AnomalyRiskColor = forecast.AnomalyRisk switch
        {
            AnomalyRisk.Low => "#00FF6A",
            AnomalyRisk.Medium => "#FFB800",
            _ => "#FF0055"
        };
    }

    private void PopulateCharts(
        List<MetricDataPointDto> historical,
        ForecastResultDto forecast)
    {
        HistoricalPoints.Clear();
        foreach (var point in historical)
        {
            HistoricalPoints.Add(point);
        }

        ForecastPoints.Clear();
        foreach (var point in forecast.ForecastPoints)
        {
            ForecastPoints.Add(point);
        }
    }

    private void PopulateForecastTable(List<MetricDataPointDto> forecastPoints)
    {
        ForecastTableRows.Clear();

        double? previousValue = null;
        foreach (var point in forecastPoints)
        {
            var changePercent = previousValue.HasValue
                ? ((point.Value - previousValue.Value) / previousValue.Value) * 100
                : 0;

            ForecastTableRows.Add(new ForecastTableRowDto
            {
                Date = point.Timestamp,
                PredictedValue = point.Value,
                LowerBound = point.LowerBound ?? 0,
                UpperBound = point.UpperBound ?? 0,
                ChangePercent = changePercent
            });

            previousValue = point.Value;
        }
    }
}

public class ForecastTableRowDto
{
    public DateTime Date { get; set; }
    public double PredictedValue { get; set; }
    public double LowerBound { get; set; }
    public double UpperBound { get; set; }
    public double ChangePercent { get; set; }
}
