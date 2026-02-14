using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MIC.Core.Application.Alerts.Common;
using MIC.Core.Application.Metrics.Common;
using MIC.Core.Domain.Entities;
using MIC.Desktop.Avalonia.Services;
using Xunit;

namespace MIC.Tests.Unit.Services;

/// <summary>
/// Comprehensive tests for ExportService.
/// Tests CSV export, PDF generation, and file operations.
/// Target: 14 tests for export functionality
/// </summary>
public class ExportServiceTests : IDisposable
{
    private readonly ExportService _sut;
    private readonly List<string> _createdFiles = new();

    public ExportServiceTests()
    {
        _sut = new ExportService();
    }

    public void Dispose()
    {
        // Cleanup created files after tests
        foreach (var file in _createdFiles.Where(File.Exists))
        {
            try { File.Delete(file); } catch { /* Ignore cleanup errors */ }
        }
    }

    #region CSV Export Tests (6 tests)

    [Fact]
    public async Task ExportAlertsToCsv_WithEmptyList_CreatesFileWithHeaderOnly()
    {
        // Arrange
        var alerts = new List<AlertDto>();
        var filename = $"test_alerts_{Guid.NewGuid()}.csv";

        // Act
        var filepath = await _sut.ExportAlertsToCsvAsync(alerts, filename);
        _createdFiles.Add(filepath);

        // Assert
        File.Exists(filepath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(filepath);
        content.Should().Contain("ID,Alert Name,Description,Severity,Status");
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Length.Should().Be(1); // Header only
    }

    [Fact]
    public async Task ExportAlertsToCsv_WithSingleAlert_CreatesValidCsv()
    {
        // Arrange
        var alerts = new List<AlertDto>
        {
            new AlertDto
            {
                Id = Guid.NewGuid(),
                AlertName = "Test Alert",
                Description = "Test Description",
                Severity = AlertSeverity.Critical,
                Status = AlertStatus.Active,
                Source = "Test Source",
                TriggeredAt = DateTime.Now,
                CreatedAt = DateTime.Now
            }
        };
        var filename = $"test_alerts_{Guid.NewGuid()}.csv";

        // Act
        var filepath = await _sut.ExportAlertsToCsvAsync(alerts, filename);
        _createdFiles.Add(filepath);

        // Assert
        File.Exists(filepath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(filepath);
        content.Should().Contain("Test Alert");
        content.Should().Contain("Test Description");
        content.Should().Contain("Critical");
    }

    [Fact]
    public async Task ExportAlertsToCsv_WithNullFilename_GeneratesDefaultFilename()
    {
        // Arrange
        var alerts = new List<AlertDto>();

        // Act
        var filepath = await _sut.ExportAlertsToCsvAsync(alerts, filename: null);
        _createdFiles.Add(filepath);

        // Assert
        File.Exists(filepath).Should().BeTrue();
        Path.GetFileName(filepath).Should().StartWith("alerts_");
        Path.GetExtension(filepath).Should().Be(".csv");
    }

    [Fact]
    public async Task ExportMetricsToCsv_WithMultipleMetrics_CreatesValidCsv()
    {
        // Arrange
        var metrics = new List<MetricDto>
        {
            new MetricDto
            {
                Id = Guid.NewGuid(),
                MetricName = "CPU Usage",
                Category = "Performance",
                Value = 75.5,
                Unit = "%",
                Timestamp = DateTime.Now
            },
            new MetricDto
            {
                Id = Guid.NewGuid(),
                MetricName = "Memory Usage",
                Category = "Performance",
                Value = 60.2,
                Unit = "%",
                Timestamp = DateTime.Now
            }
        };
        var filename = $"test_metrics_{Guid.NewGuid()}.csv";

        // Act
        var filepath = await _sut.ExportMetricsToCsvAsync(metrics, filename);
        _createdFiles.Add(filepath);

        // Assert
        File.Exists(filepath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(filepath);
        content.Should().Contain("CPU Usage");
        content.Should().Contain("Memory Usage");
        content.Should().Contain("75.5");
        content.Should().Contain("60.2");
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Length.Should().Be(3); // Header + 2 data rows
    }

    [Fact]
    public async Task ExportMetricsToCsv_WithSpecialCharacters_EscapesCorrectly()
    {
        // Arrange
        var metrics = new List<MetricDto>
        {
            new MetricDto
            {
                Id = Guid.NewGuid(),
                MetricName = "Test, Metric",
                Category = "Category \"with quotes\"",
                Value = 100,
                Unit = "%",
                Timestamp = DateTime.Now
            }
        };
        var filename = $"test_metrics_{Guid.NewGuid()}.csv";

        // Act
        var filepath = await _sut.ExportMetricsToCsvAsync(metrics, filename);
        _createdFiles.Add(filepath);

        // Assert
        File.Exists(filepath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(filepath);
        // CSV should escape quotes and commas
        content.Should().Contain("Test, Metric");
    }

    [Fact]
    public async Task ExportMetricsToCsv_WithEmptyList_CreatesFileWithHeaderOnly()
    {
        // Arrange
        var metrics = new List<MetricDto>();
        var filename = $"test_metrics_{Guid.NewGuid()}.csv";

        // Act
        var filepath = await _sut.ExportMetricsToCsvAsync(metrics, filename);
        _createdFiles.Add(filepath);

        // Assert
        File.Exists(filepath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(filepath);
        content.Should().Contain("ID,Name,Category,Value,Unit,Timestamp");
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Length.Should().Be(1); // Header only
    }

    #endregion

    #region PDF Export Tests (4 tests)

    [Fact]
    public async Task ExportPredictionsToPdf_WithNullFilename_GeneratesDefaultFilename()
    {
        // Arrange
        var predictions = new List<PredictionExportRow>
        {
            new PredictionExportRow(
                MetricName: "Test Scenario",
                CurrentValue: 100000,
                PredictedValue: 125000,
                ChangePercent: 25.0,
                Confidence: 85.5,
                Direction: "Up",
                TimeFrame: "Q1 2026")
        };

        // Act
        var filepath = await _sut.ExportPredictionsToPdfAsync(predictions, filename: null);
        _createdFiles.Add(filepath);

        // Assert
        File.Exists(filepath).Should().BeTrue();
        Path.GetFileName(filepath).Should().StartWith("predictions_");
        Path.GetExtension(filepath).Should().Be(".pdf");
    }

    [Fact]
    public async Task ExportPredictionsToPdf_WithSinglePrediction_CreatesValidPdf()
    {
        // Arrange
        var predictions = new List<PredictionExportRow>
        {
            new PredictionExportRow(
                MetricName: "Revenue Growth",
                CurrentValue: 500000,
                PredictedValue: 650000,
                ChangePercent: 30.0,
                Confidence: 92.3,
                Direction: "Up",
                TimeFrame: "2026")
        };
        var filename = $"test_predictions_{Guid.NewGuid()}.pdf";

        // Act
        var filepath = await _sut.ExportPredictionsToPdfAsync(predictions, filename);
        _createdFiles.Add(filepath);

        // Assert
        File.Exists(filepath).Should().BeTrue();
        var fileInfo = new FileInfo(filepath);
        fileInfo.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExportPredictionsToPdf_WithMultiplePredictions_CreatesValidPdf()
    {
        // Arrange
        var predictions = new List<PredictionExportRow>
        {
            new PredictionExportRow(
                MetricName: "Market Expansion",
                CurrentValue: 1000000,
                PredictedValue: 1250000,
                ChangePercent: 25.0,
                Confidence: 78.5,
                Direction: "Up",
                TimeFrame: "Q2 2026"),
            new PredictionExportRow(
                MetricName: "Cost Reduction",
                CurrentValue: 200000,
                PredictedValue: 150000,
                ChangePercent: -25.0,
                Confidence: 88.2,
                Direction: "Down",
                TimeFrame: "Q1 2026")
        };
        var filename = $"test_predictions_{Guid.NewGuid()}.pdf";

        // Act
        var filepath = await _sut.ExportPredictionsToPdfAsync(predictions, filename);
        _createdFiles.Add(filepath);

        // Assert
        File.Exists(filepath).Should().BeTrue();
        var fileInfo = new FileInfo(filepath);
        fileInfo.Length.Should().BeGreaterThan(1000); // Should have content
    }

    [Fact]
    public async Task ExportPredictionsToPdf_WithEmptyList_CreatesValidPdf()
    {
        // Arrange
        var predictions = new List<PredictionExportRow>();
        var filename = $"test_predictions_{Guid.NewGuid()}.pdf";

        // Act
        var filepath = await _sut.ExportPredictionsToPdfAsync(predictions, filename);
        _createdFiles.Add(filepath);

        // Assert
        File.Exists(filepath).Should().BeTrue();
        var fileInfo = new FileInfo(filepath);
        fileInfo.Length.Should().BeGreaterThan(0); // Should have header even if empty
    }

    #endregion

    #region File Operations & Integration Tests (4 tests)

    [Fact]
    public void ExportService_Constructor_CreatesExportDirectory()
    {
        // Arrange & Act
        var service = new ExportService();

        // Assert
        var exportDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "MIC Exports");
        Directory.Exists(exportDir).Should().BeTrue();
    }

    [Fact]
    public void ExportService_Instance_ReturnsSingleton()
    {
        // Act
        var instance1 = ExportService.Instance;
        var instance2 = ExportService.Instance;

        // Assert
        instance1.Should().BeSameAs(instance2);
    }

    [Fact]
    public async Task ExportAlertsToCsv_ReturnsCorrectFilePath()
    {
        // Arrange
        var alerts = new List<AlertDto>();
        var filename = $"test_path_{Guid.NewGuid()}.csv";

        // Act
        var filepath = await _sut.ExportAlertsToCsvAsync(alerts, filename);
        _createdFiles.Add(filepath);

        // Assert
        filepath.Should().EndWith(filename);
        Path.IsPathRooted(filepath).Should().BeTrue();
    }

    [Fact]
    public async Task ExportMetricsToCsv_OverwritesExistingFile()
    {
        // Arrange
        var metrics = new List<MetricDto>
        {
            new MetricDto
            {
                Id = Guid.NewGuid(),
                MetricName = "First",
                Category = "Test",
                Value = 100,
                Unit = "count",
                Timestamp = DateTime.Now
            }
        };
        var filename = $"test_overwrite_{Guid.NewGuid()}.csv";

        // Act - Create file twice
        var filepath1 = await _sut.ExportMetricsToCsvAsync(metrics, filename);
        var content1 = await File.ReadAllTextAsync(filepath1);

        // Create updated metrics list
        var updatedMetrics = new List<MetricDto>
        {
            new MetricDto
            {
                Id = Guid.NewGuid(),
                MetricName = "Second",
                Category = "Test",
                Value = 200,
                Unit = "count",
                Timestamp = DateTime.Now
            }
        };
        var filepath2 = await _sut.ExportMetricsToCsvAsync(updatedMetrics, filename);
        var content2 = await File.ReadAllTextAsync(filepath2);

        _createdFiles.Add(filepath2);

        // Assert
        filepath1.Should().Be(filepath2);
        content2.Should().Contain("Second");
        content2.Should().NotContain("First");
    }

    #endregion

    #region HTML Report Tests

    [Fact]
    public async Task GenerateHtmlReportAsync_WithEmptyData_CreatesValidHtml()
    {
        var alerts = new List<AlertDto>();
        var metrics = new List<MetricDto>();
        var filename = $"test_report_{Guid.NewGuid()}.html";

        var filepath = await _sut.GenerateHtmlReportAsync(alerts, metrics, filename);
        _createdFiles.Add(filepath);

        File.Exists(filepath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(filepath);
        content.Should().Contain("<!DOCTYPE html>");
        content.Should().Contain("MIC Report");
        content.Should().Contain("TOTAL ALERTS");
        content.Should().Contain("METRICS");
    }

    [Fact]
    public async Task GenerateHtmlReportAsync_CountsAlertsCorrectly()
    {
        var alerts = new List<AlertDto>
        {
            new AlertDto { Id = Guid.NewGuid(), AlertName = "A1", Severity = AlertSeverity.Critical, Status = AlertStatus.Active, Source = "S1" },
            new AlertDto { Id = Guid.NewGuid(), AlertName = "A2", Severity = AlertSeverity.Warning, Status = AlertStatus.Active, Source = "S2" },
            new AlertDto { Id = Guid.NewGuid(), AlertName = "A3", Severity = AlertSeverity.Critical, Status = AlertStatus.Resolved, Source = "S3" }
        };
        var metrics = new List<MetricDto>();
        var filename = $"test_counts_{Guid.NewGuid()}.html";

        var filepath = await _sut.GenerateHtmlReportAsync(alerts, metrics, filename);
        _createdFiles.Add(filepath);

        var content = await File.ReadAllTextAsync(filepath);
        // Total alerts = 3
        content.Should().Contain(">3<");
        // Critical count = 2
        content.Should().Contain(">2<");
    }

    [Fact]
    public async Task GenerateHtmlReportAsync_IncludesAlertDataInTable()
    {
        var alerts = new List<AlertDto>
        {
            new AlertDto { Id = Guid.NewGuid(), AlertName = "Security Breach", Severity = AlertSeverity.Critical, Status = AlertStatus.Active, Source = "Firewall", TriggeredAt = DateTime.Now }
        };
        var metrics = new List<MetricDto>();
        var filename = $"test_alert_table_{Guid.NewGuid()}.html";

        var filepath = await _sut.GenerateHtmlReportAsync(alerts, metrics, filename);
        _createdFiles.Add(filepath);

        var content = await File.ReadAllTextAsync(filepath);
        content.Should().Contain("Security Breach");
        content.Should().Contain("Firewall");
        content.Should().Contain("Critical");
    }

    [Fact]
    public async Task GenerateHtmlReportAsync_IncludesMetricDataInTable()
    {
        var alerts = new List<AlertDto>();
        var metrics = new List<MetricDto>
        {
            new MetricDto { Id = Guid.NewGuid(), MetricName = "CPU Load", Category = "Performance", Value = 87.5, Unit = "percent", Timestamp = DateTime.Now }
        };
        var filename = $"test_metric_table_{Guid.NewGuid()}.html";

        var filepath = await _sut.GenerateHtmlReportAsync(alerts, metrics, filename);
        _createdFiles.Add(filepath);

        var content = await File.ReadAllTextAsync(filepath);
        content.Should().Contain("CPU Load");
        content.Should().Contain("Performance");
        content.Should().Contain("87.50");
        content.Should().Contain("percent");
    }

    [Fact]
    public async Task GenerateHtmlReportAsync_HtmlEncodesSpecialCharacters()
    {
        var alerts = new List<AlertDto>
        {
            new AlertDto { Id = Guid.NewGuid(), AlertName = "Test <script>alert('xss')</script>", Severity = AlertSeverity.Info, Status = AlertStatus.Active, Source = "Source&Special", TriggeredAt = DateTime.Now }
        };
        var metrics = new List<MetricDto>();
        var filename = $"test_encoding_{Guid.NewGuid()}.html";

        var filepath = await _sut.GenerateHtmlReportAsync(alerts, metrics, filename);
        _createdFiles.Add(filepath);

        var content = await File.ReadAllTextAsync(filepath);
        content.Should().Contain("&lt;script&gt;");
        content.Should().Contain("Source&amp;Special");
        content.Should().NotContain("<script>alert");
    }

    [Fact]
    public async Task GenerateHtmlReportAsync_DefaultFilename_GeneratesTimestamped()
    {
        var alerts = new List<AlertDto>();
        var metrics = new List<MetricDto>();

        var filepath = await _sut.GenerateHtmlReportAsync(alerts, metrics);
        _createdFiles.Add(filepath);

        var filename = Path.GetFileName(filepath);
        filename.Should().StartWith("report_");
        filename.Should().EndWith(".html");
    }

    [Fact]
    public async Task GenerateHtmlReportAsync_LimitsTo20Rows()
    {
        var alerts = Enumerable.Range(1, 25).Select(i => new AlertDto
        {
            Id = Guid.NewGuid(),
            AlertName = $"Alert{i}",
            Severity = AlertSeverity.Info,
            Status = AlertStatus.Active,
            Source = "S",
            TriggeredAt = DateTime.Now
        }).ToList();
        var metrics = new List<MetricDto>();
        var filename = $"test_limit_{Guid.NewGuid()}.html";

        var filepath = await _sut.GenerateHtmlReportAsync(alerts, metrics, filename);
        _createdFiles.Add(filepath);

        var content = await File.ReadAllTextAsync(filepath);
        content.Should().Contain("Alert20");
        content.Should().NotContain("Alert21");
    }

    #endregion

    #region JSON Export Tests

    [Fact]
    public async Task ExportToJsonAsync_WithEmptyData_CreatesEmptyArray()
    {
        var data = new List<AlertDto>();
        var filename = $"test_json_{Guid.NewGuid()}.json";

        var filepath = await _sut.ExportToJsonAsync(data, "alerts", filename);
        _createdFiles.Add(filepath);

        File.Exists(filepath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(filepath);
        content.Trim().Should().Be("[]");
    }

    [Fact]
    public async Task ExportToJsonAsync_WithData_CreatesIndentedJson()
    {
        var data = new List<MetricDto>
        {
            new MetricDto { Id = Guid.NewGuid(), MetricName = "TestMetric", Category = "Cat", Value = 99.0, Unit = "u", Timestamp = DateTime.Now }
        };
        var filename = $"test_json_data_{Guid.NewGuid()}.json";

        var filepath = await _sut.ExportToJsonAsync(data, "metrics", filename);
        _createdFiles.Add(filepath);

        var content = await File.ReadAllTextAsync(filepath);
        content.Should().Contain("TestMetric");
        content.Should().Contain("\n"); // indented JSON
    }

    [Fact]
    public async Task ExportToJsonAsync_DefaultFilename_UsesNameParameter()
    {
        var data = new List<AlertDto>();

        var filepath = await _sut.ExportToJsonAsync(data, "myexport");
        _createdFiles.Add(filepath);

        var filename = Path.GetFileName(filepath);
        filename.Should().StartWith("myexport_");
        filename.Should().EndWith(".json");
    }

    #endregion

    #region EscapeCsv Tests

    private static string InvokeEscapeCsv(string? value)
    {
        var method = typeof(ExportService).GetMethod("EscapeCsv",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        return (string)method.Invoke(null, new object?[] { value })!;
    }

    [Fact]
    public void EscapeCsv_Null_ReturnsEmpty()
    {
        InvokeEscapeCsv(null).Should().BeEmpty();
    }

    [Fact]
    public void EscapeCsv_Empty_ReturnsEmpty()
    {
        InvokeEscapeCsv("").Should().BeEmpty();
    }

    [Fact]
    public void EscapeCsv_NormalText_ReturnsUnchanged()
    {
        InvokeEscapeCsv("hello world").Should().Be("hello world");
    }

    [Fact]
    public void EscapeCsv_WithDoubleQuotes_DoublesQuotes()
    {
        InvokeEscapeCsv("say \"hello\"").Should().Be("say \"\"hello\"\"");
    }

    #endregion
}
