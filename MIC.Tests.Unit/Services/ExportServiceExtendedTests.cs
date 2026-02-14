using FluentAssertions;
using MIC.Desktop.Avalonia.Services;
using MIC.Core.Application.Alerts.Common;
using MIC.Core.Application.Metrics.Common;
using MIC.Core.Domain.Entities;

namespace MIC.Tests.Unit.Services;

/// <summary>
/// Extended export service tests focusing on HTML reports, JSON export,
/// PredictionExportRow, and edge cases not covered by primary test suite.
/// </summary>
public class ExportServiceExtendedTests : IDisposable
{
    private readonly ExportService _sut;
    private readonly List<string> _createdFiles = new();

    public ExportServiceExtendedTests()
    {
        _sut = new ExportService();
    }

    public void Dispose()
    {
        foreach (var file in _createdFiles.Where(File.Exists))
        {
            try { File.Delete(file); } catch { }
        }
    }

    #region HTML Report Generation

    [Fact]
    public async Task GenerateHtmlReportAsync_WithAlertsAndMetrics_ShouldCreateValidHtml()
    {
        var alerts = new[]
        {
            new AlertDto
            {
                AlertName = "CPU Spike",
                Severity = AlertSeverity.Critical,
                Status = AlertStatus.Active,
                Source = "Monitor",
                TriggeredAt = DateTime.UtcNow
            },
            new AlertDto
            {
                AlertName = "Memory Low",
                Severity = AlertSeverity.Warning,
                Status = AlertStatus.Acknowledged,
                Source = "System",
                TriggeredAt = DateTime.UtcNow
            }
        };

        var metrics = new[]
        {
            new MetricDto
            {
                MetricName = "Revenue",
                Category = "Financial",
                Value = 150000,
                Unit = "$",
                Timestamp = DateTime.UtcNow
            }
        };

        var filename = $"test_extended_report_{Guid.NewGuid():N}.html";
        var filepath = await _sut.GenerateHtmlReportAsync(alerts, metrics, filename);
        _createdFiles.Add(filepath);

        File.Exists(filepath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(filepath);
        content.Should().Contain("<!DOCTYPE html>");
        content.Should().Contain("CPU Spike");
        content.Should().Contain("Memory Low");
        content.Should().Contain("Revenue");
        content.Should().Contain("Mbarie Intelligence Console");
    }

    [Fact]
    public async Task GenerateHtmlReportAsync_ShouldCountCriticalAlerts()
    {
        var alerts = new[]
        {
            new AlertDto { Severity = AlertSeverity.Critical, AlertName = "A1", Source = "S", Status = AlertStatus.Active },
            new AlertDto { Severity = AlertSeverity.Info, AlertName = "A2", Source = "S", Status = AlertStatus.Resolved },
            new AlertDto { Severity = AlertSeverity.Critical, AlertName = "A3", Source = "S", Status = AlertStatus.Active }
        };

        var filename = $"test_critical_{Guid.NewGuid():N}.html";
        var filepath = await _sut.GenerateHtmlReportAsync(alerts, Array.Empty<MetricDto>(), filename);
        _createdFiles.Add(filepath);

        var content = await File.ReadAllTextAsync(filepath);
        // 2 critical alerts
        content.Should().Contain(">2<");
        // 2 active (non-resolved) alerts
        content.Should().Contain("ACTIVE");
    }

    [Fact]
    public async Task GenerateHtmlReportAsync_EmptyData_ShouldStillCreateStructuredReport()
    {
        var filename = $"test_empty_report_{Guid.NewGuid():N}.html";
        var filepath = await _sut.GenerateHtmlReportAsync(
            Array.Empty<AlertDto>(), Array.Empty<MetricDto>(), filename);
        _createdFiles.Add(filepath);

        var content = await File.ReadAllTextAsync(filepath);
        content.Should().Contain("TOTAL ALERTS");
        content.Should().Contain("METRICS");
        content.Should().Contain(">0<"); // 0 alerts/metrics
    }

    [Fact]
    public async Task GenerateHtmlReportAsync_WithHtmlSpecialChars_ShouldEscape()
    {
        var alerts = new[]
        {
            new AlertDto
            {
                AlertName = "<script>alert('xss')</script>",
                Source = "Test&Source",
                Severity = AlertSeverity.Info,
                Status = AlertStatus.Active,
                TriggeredAt = DateTime.UtcNow
            }
        };

        var filename = $"test_escape_{Guid.NewGuid():N}.html";
        var filepath = await _sut.GenerateHtmlReportAsync(alerts, Array.Empty<MetricDto>(), filename);
        _createdFiles.Add(filepath);

        var content = await File.ReadAllTextAsync(filepath);
        // HtmlEncode should escape < > &
        content.Should().Contain("&lt;script&gt;");
        content.Should().Contain("Test&amp;Source");
    }

    [Fact]
    public async Task GenerateHtmlReportAsync_NoFilename_ShouldGenerateDefault()
    {
        var filepath = await _sut.GenerateHtmlReportAsync(
            Array.Empty<AlertDto>(), Array.Empty<MetricDto>());
        _createdFiles.Add(filepath);

        Path.GetFileName(filepath).Should().StartWith("report_");
        filepath.Should().EndWith(".html");
    }

    [Fact]
    public async Task GenerateHtmlReportAsync_LimitedTo20Alerts()
    {
        var alerts = Enumerable.Range(0, 30)
            .Select(i => new AlertDto
            {
                AlertName = $"Alert #{i}",
                Source = "Src",
                Severity = AlertSeverity.Info,
                Status = AlertStatus.Active,
                TriggeredAt = DateTime.UtcNow
            })
            .ToArray();

        var filename = $"test_limit_{Guid.NewGuid():N}.html";
        var filepath = await _sut.GenerateHtmlReportAsync(alerts, Array.Empty<MetricDto>(), filename);
        _createdFiles.Add(filepath);

        var content = await File.ReadAllTextAsync(filepath);
        // Only first 20 should appear in the table
        content.Should().Contain("Alert #0");
        content.Should().Contain("Alert #19");
        content.Should().NotContain("Alert #20");
    }

    #endregion

    #region JSON Export

    [Fact]
    public async Task ExportToJsonAsync_WithTypedData_ShouldCreateIndentedJson()
    {
        var data = new[]
        {
            new MetricDto { MetricName = "CPU", Value = 85.0, Unit = "%" },
            new MetricDto { MetricName = "Memory", Value = 72.0, Unit = "%" }
        };

        var filename = $"test_json_{Guid.NewGuid():N}.json";
        var filepath = await _sut.ExportToJsonAsync(data, "metrics", filename);
        _createdFiles.Add(filepath);

        var content = await File.ReadAllTextAsync(filepath);
        content.Should().Contain("\"MetricName\": \"CPU\"");
        content.Should().Contain("\"MetricName\": \"Memory\"");
    }

    [Fact]
    public async Task ExportToJsonAsync_EmptyCollection_ShouldCreateEmptyArray()
    {
        var filename = $"test_json_empty_{Guid.NewGuid():N}.json";
        var filepath = await _sut.ExportToJsonAsync(Array.Empty<object>(), "empty", filename);
        _createdFiles.Add(filepath);

        var content = await File.ReadAllTextAsync(filepath);
        content.Should().Be("[]");
    }

    [Fact]
    public async Task ExportToJsonAsync_NoFilename_ShouldUseName()
    {
        var filepath = await _sut.ExportToJsonAsync(Array.Empty<object>(), "customname");
        _createdFiles.Add(filepath);

        Path.GetFileName(filepath).Should().StartWith("customname_");
        filepath.Should().EndWith(".json");
    }

    #endregion

    #region PredictionExportRow

    [Fact]
    public void PredictionExportRow_ShouldSetAllProperties()
    {
        var row = new PredictionExportRow("Revenue", 100000, 120000, 20.0, 0.85, "Up", "30 days");

        row.MetricName.Should().Be("Revenue");
        row.CurrentValue.Should().Be(100000);
        row.PredictedValue.Should().Be(120000);
        row.ChangePercent.Should().Be(20.0);
        row.Confidence.Should().Be(0.85);
        row.Direction.Should().Be("Up");
        row.TimeFrame.Should().Be("30 days");
    }

    [Fact]
    public void PredictionExportRow_RecordEquality()
    {
        var r1 = new PredictionExportRow("M", 1, 2, 100, 0.9, "Up", "7d");
        var r2 = new PredictionExportRow("M", 1, 2, 100, 0.9, "Up", "7d");
        r1.Should().Be(r2);
    }

    [Fact]
    public void PredictionExportRow_Inequality()
    {
        var r1 = new PredictionExportRow("M", 1, 2, 100, 0.9, "Up", "7d");
        var r2 = new PredictionExportRow("N", 1, 2, 100, 0.9, "Up", "7d");
        r1.Should().NotBe(r2);
    }

    [Fact]
    public void PredictionExportRow_NegativeChangePercent()
    {
        var row = new PredictionExportRow("Cost", 100, 80, -20.0, 0.75, "Down", "90 days");
        row.ChangePercent.Should().Be(-20.0);
        row.Direction.Should().Be("Down");
    }

    #endregion

    #region Instance

    [Fact]
    public void Instance_ShouldNotBeNull()
    {
        ExportService.Instance.Should().NotBeNull();
    }

    [Fact]
    public void Instance_ShouldBeSingleton()
    {
        ExportService.Instance.Should().BeSameAs(ExportService.Instance);
    }

    #endregion

    #region ExportDirectory Property

    [Fact]
    public void ExportDirectory_ShouldContainMicExports()
    {
        _sut.ExportDirectory.Should().Contain("MIC Exports");
    }

    [Fact]
    public void ExportDirectory_ShouldExist()
    {
        Directory.Exists(_sut.ExportDirectory).Should().BeTrue();
    }

    #endregion

    #region CSV Edge Cases

    [Fact]
    public async Task ExportAlertsToCsvAsync_WithQuotesInFields_ShouldEscapeDoubleQuotes()
    {
        var alerts = new[]
        {
            new AlertDto
            {
                AlertName = "Alert with \"special\" chars",
                Description = "Desc with \"quotes\"",
                Source = "Source\"Test"
            }
        };

        var filename = $"test_csv_escape_{Guid.NewGuid():N}.csv";
        var filepath = await _sut.ExportAlertsToCsvAsync(alerts, filename);
        _createdFiles.Add(filepath);

        var content = await File.ReadAllTextAsync(filepath);
        content.Should().Contain("\"\"special\"\"");
    }

    [Fact]
    public async Task ExportMetricsToCsvAsync_MultipleRows_ShouldHaveCorrectLineCount()
    {
        var metrics = Enumerable.Range(0, 5)
            .Select(i => new MetricDto
            {
                MetricName = $"Metric {i}",
                Category = "Cat",
                Value = i * 10,
                Unit = "%",
                Timestamp = DateTime.UtcNow
            }).ToArray();

        var filename = $"test_csv_multi_{Guid.NewGuid():N}.csv";
        var filepath = await _sut.ExportMetricsToCsvAsync(metrics, filename);
        _createdFiles.Add(filepath);

        var lines = await File.ReadAllLinesAsync(filepath);
        lines.Should().HaveCount(6); // 1 header + 5 data rows
    }

    #endregion
}
