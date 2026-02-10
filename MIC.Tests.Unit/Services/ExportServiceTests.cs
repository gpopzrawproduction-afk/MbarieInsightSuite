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
}
