using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Reports.Commands.GenerateReport;
using MIC.Core.Application.Reports.Commands.DeleteReport;
using MIC.Core.Application.Reports.Queries.GetReports;
using MIC.Desktop.Avalonia.Services;

namespace MIC.Desktop.Avalonia.ViewModels;

/// <summary>
/// ViewModel for Reports generation and management.
/// </summary>
public partial class ReportsViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ReportsViewModel> _logger;
    private CancellationTokenSource _cancellationTokenSource = new();

    // Report type selection
    [ObservableProperty]
    public ReportType selectedReportType = ReportType.AlertSummary;

    // Date range
    [ObservableProperty]
    public DateTimeOffset fromDate = DateTimeOffset.Now.AddDays(-30);

    [ObservableProperty]
    public DateTimeOffset toDate = DateTimeOffset.Now;

    // Format selection
    [ObservableProperty]
    public ReportFormat selectedFormat = ReportFormat.PDF;

    // Options
    [ObservableProperty]
    public bool includeCharts = true;

    [ObservableProperty]
    public bool includeRawData;

    [ObservableProperty]
    public bool emailToSelf;

    // State
    [ObservableProperty]
    public bool isGenerating;

    [ObservableProperty]
    public double generationProgress;

    [ObservableProperty]
    public string generationStatusText = string.Empty;

    // Report list
    public ObservableCollection<ReportSummaryDto> Reports { get; } = new();

    [ObservableProperty]
    public bool hasReports;

    [ObservableProperty]
    public bool isLoadingReports;

    // Selected report
    [ObservableProperty]
    public ReportSummaryDto? selectedReport;

    // Report type selection states
    [ObservableProperty]
    public bool isAlertSummarySelected = true;

    [ObservableProperty]
    public bool isEmailActivitySelected;

    [ObservableProperty]
    public bool isMetricsTrendSelected;

    [ObservableProperty]
    public bool isAiChatSelected;

    [ObservableProperty]
    public bool isFullDashboardSelected;

    // Report type cards data
    public List<ReportTypeCard> ReportTypeCards { get; } = new()
    {
        new ReportTypeCard
        {
            Type = ReportType.AlertSummary,
            Icon = "??",
            Name = "Alert Summary",
            Description = "Summary of all alerts in date range",
            Badge = "ALERTS"
        },
        new ReportTypeCard
        {
            Type = ReportType.EmailActivity,
            Icon = "??",
            Name = "Email Activity",
            Description = "Email send/receive patterns and trends",
            Badge = "EMAIL"
        },
        new ReportTypeCard
        {
            Type = ReportType.MetricsTrend,
            Icon = "??",
            Name = "Metrics Trend",
            Description = "Performance metrics and statistics",
            Badge = "METRICS"
        },
        new ReportTypeCard
        {
            Type = ReportType.AiChatSummary,
            Icon = "??",
            Name = "AI Chat Summary",
            Description = "AI assistant interactions and insights",
            Badge = "AI"
        },
        new ReportTypeCard
        {
            Type = ReportType.FullDashboard,
            Icon = "??",
            Name = "Full Dashboard",
            Description = "Complete system report with all data",
            Badge = "FULL"
        }
    };

    public ReportsViewModel(
        IMediator mediator,
        ILogger<ReportsViewModel> logger)
    {
        _mediator = mediator;
        _logger = logger;

        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing ReportsViewModel");
            await LoadReportsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing ReportsViewModel");
        }
    }

    [RelayCommand]
    public async Task GenerateReportAsync()
    {
        IsGenerating = true;
        GenerationProgress = 0;
        GenerationStatusText = "Initializing...";

        try
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;

            _logger.LogInformation("Generating {ReportType} report", SelectedReportType);

            // Simulate progress updates
            await SimulateProgress("Fetching data...", 0.2, 500);
            await SimulateProgress("Building report...", 0.6, 500);
            await SimulateProgress("Saving file...", 0.9, 500);

            var command = new GenerateReportCommand
            {
                Type = SelectedReportType,
                FromDate = FromDate.DateTime,
                ToDate = ToDate.DateTime,
                Format = SelectedFormat,
                IncludeCharts = IncludeCharts,
                IncludeRawData = IncludeRawData,
                EmailToSelf = EmailToSelf
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsError)
            {
                GenerationStatusText = $"Error: {result.FirstError.Description}";
                _logger.LogWarning("Report generation failed: {Error}", result.FirstError.Description);
                IsGenerating = false;
                return;
            }

            // Add to reports list
            var newReport = new ReportSummaryDto
            {
                Id = result.Value.ReportId,
                Name = result.Value.FileName,
                Type = result.Value.Type,
                Format = result.Value.Format,
                GeneratedAt = result.Value.GeneratedAt,
                FileSizeBytes = result.Value.FileSizeBytes,
                FileSizeFormatted = FormatFileSize(result.Value.FileSizeBytes),
                OutputFilePath = result.Value.OutputFilePath,
                FileExists = File.Exists(result.Value.OutputFilePath)
            };

            Reports.Insert(0, newReport);
            HasReports = Reports.Count > 0;

            await SimulateProgress("Complete!", 1.0, 300);

            _logger.LogInformation("Report generated successfully: {FileName}", result.Value.FileName);

            IsGenerating = false;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Report generation was cancelled");
            IsGenerating = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report");
            GenerationStatusText = $"Error: {ex.Message}";
            IsGenerating = false;
        }
    }

    [RelayCommand]
    public void SelectReportType(ReportType type)
    {
        SelectedReportType = type;

        IsAlertSummarySelected = type == ReportType.AlertSummary;
        IsEmailActivitySelected = type == ReportType.EmailActivity;
        IsMetricsTrendSelected = type == ReportType.MetricsTrend;
        IsAiChatSelected = type == ReportType.AiChatSummary;
        IsFullDashboardSelected = type == ReportType.FullDashboard;
    }

    [RelayCommand]
    public void SelectFormat(ReportFormat format)
    {
        SelectedFormat = format;
    }

    [RelayCommand]
    public async Task OpenReportAsync(ReportSummaryDto? report)
    {
        if (report == null || !File.Exists(report.OutputFilePath))
        {
            _logger.LogWarning("Report file not found: {Path}", report?.OutputFilePath);
            NotificationService.Instance.ShowWarning("Report file not found");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(report.OutputFilePath) { UseShellExecute = true });
            _logger.LogInformation("Opened report: {Path}", report.OutputFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening report");
            NotificationService.Instance.ShowError("Failed to open report");
        }
    }

    [RelayCommand]
    public async Task DeleteReportAsync(ReportSummaryDto? report)
    {
        if (report == null)
            return;

        try
        {
            var command = new DeleteReportCommand { ReportId = report.Id };
            var result = await _mediator.Send(command, CancellationToken.None);

            if (result.IsError)
            {
                _logger.LogWarning("Failed to delete report: {Error}", result.FirstError.Description);
                NotificationService.Instance.ShowWarning("Failed to delete report");
                return;
            }

            Reports.Remove(report);
            HasReports = Reports.Count > 0;

            NotificationService.Instance.ShowSuccess("Report deleted successfully");
            _logger.LogInformation("Report deleted: {ReportId}", report.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting report");
            NotificationService.Instance.ShowError("Error deleting report");
        }
    }

    [RelayCommand]
    public async Task RegenerateReportAsync(ReportSummaryDto? report)
    {
        if (report == null)
            return;

        FromDate = report.GeneratedAt.AddDays(-30);
        ToDate = report.GeneratedAt;
        SelectedReportType = report.Type;
        SelectedFormat = report.Format;

        await GenerateReportAsync();
    }

    [RelayCommand]
    public async Task LoadReportsAsync()
    {
        IsLoadingReports = true;

        try
        {
            var query = new GetReportsQuery { PageNumber = 1, PageSize = 50 };
            var result = await _mediator.Send(query, CancellationToken.None);

            if (result.IsError)
            {
                _logger.LogWarning("Failed to load reports: {Error}", result.FirstError.Description);
                IsLoadingReports = false;
                return;
            }

            Reports.Clear();
            foreach (var report in result.Value)
            {
                Reports.Add(report);
            }

            HasReports = Reports.Count > 0;

            _logger.LogInformation("Loaded {Count} reports", Reports.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading reports");
        }
        finally
        {
            IsLoadingReports = false;
        }
    }

    private async Task SimulateProgress(string status, double progress, int delayMs)
    {
        GenerationStatusText = status;
        GenerationProgress = progress;
        await Task.Delay(delayMs);
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:F2} {sizes[order]}";
    }
}

public class ReportTypeCard
{
    public ReportType Type { get; set; }
    public string Icon { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Badge { get; set; } = string.Empty;
}
