using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Reports.Commands.GenerateReport;
using System.Text;

namespace MIC.Core.Application.Reports.Commands.GenerateReport;

public sealed class GenerateReportCommandHandler(
    IAlertRepository alertRepository,
    IEmailRepository emailRepository,
    IMetricsRepository metricsRepository,
    IReportRepository reportRepository,
    ILogger<GenerateReportCommandHandler> logger) : ICommandHandler<GenerateReportCommand, ReportGeneratedDto>
{
    public async Task<ErrorOr<ReportGeneratedDto>> Handle(GenerateReportCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation("Generating {ReportType} report from {FromDate} to {ToDate}",
            command.Type, command.FromDate, command.ToDate);

        try
        {
            // Fetch data based on report type
            var reportContent = new StringBuilder();
            reportContent.AppendLine($"???????????????????????????????????????????????????");
            reportContent.AppendLine($"  MBARIE INSIGHT SUITE - {command.Type.ToString().ToUpper()} REPORT");
            reportContent.AppendLine($"???????????????????????????????????????????????????");
            reportContent.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
            reportContent.AppendLine($"Date Range: {command.FromDate:yyyy-MM-dd} to {command.ToDate:yyyy-MM-dd}");
            reportContent.AppendLine($"Format: {command.Format}");
            reportContent.AppendLine();

            await PopulateReportContent(reportContent, command, cancellationToken);

            // Create report directory
            var reportDir = GetReportDirectory();
            Directory.CreateDirectory(reportDir);

            // Generate filename
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var extension = command.Format switch
            {
                ReportFormat.PDF => ".txt", // For now, plain text
                ReportFormat.XLSX => ".txt",
                ReportFormat.CSV => ".csv",
                _ => ".txt"
            };

            var filename = $"{command.Type}_{timestamp}{extension}";
            var filepath = Path.Combine(reportDir, filename);

            // Save file
            await File.WriteAllTextAsync(filepath, reportContent.ToString(), cancellationToken);
            var fileInfo = new FileInfo(filepath);

            // Create Report entity and save to DB
            var reportId = Guid.NewGuid();
            var reportEntity = new 
            {
                Id = reportId,
                Type = command.Type,
                Format = command.Format,
                GeneratedAt = DateTime.UtcNow,
                OutputFilePath = filepath,
                FileName = filename,
                FileSizeBytes = fileInfo.Length
            };

            await reportRepository.AddAsync(reportEntity, cancellationToken);

            logger.LogInformation("Report generated successfully: {FilePath}", filepath);

            return new ReportGeneratedDto
            {
                ReportId = reportId,
                OutputFilePath = filepath,
                FileName = filename,
                FileSizeBytes = fileInfo.Length,
                GeneratedAt = DateTime.UtcNow,
                Format = command.Format,
                Type = command.Type
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating report");
            return Error.Failure("report.generation_error", $"Failed to generate report: {ex.Message}");
        }
    }

    private async Task PopulateReportContent(
        StringBuilder content,
        GenerateReportCommand command,
        CancellationToken cancellationToken)
    {
        switch (command.Type)
        {
            case ReportType.AlertSummary:
                await AddAlertSummary(content, command, cancellationToken);
                break;

            case ReportType.EmailActivity:
                await AddEmailActivity(content, command, cancellationToken);
                break;

            case ReportType.MetricsTrend:
                await AddMetricsTrend(content, command, cancellationToken);
                break;

            case ReportType.AiChatSummary:
                await AddAiChatSummary(content, command, cancellationToken);
                break;

            case ReportType.FullDashboard:
                await AddAlertSummary(content, command, cancellationToken);
                content.AppendLine();
                await AddEmailActivity(content, command, cancellationToken);
                content.AppendLine();
                await AddMetricsTrend(content, command, cancellationToken);
                break;
        }
    }

    private async Task AddAlertSummary(
        StringBuilder content,
        GenerateReportCommand command,
        CancellationToken cancellationToken)
    {
        content.AppendLine("ALERT SUMMARY");
        content.AppendLine("?????????????????????????????????????????????????????");

        try
        {
            var alerts = await alertRepository.GetAllAsync(cancellationToken);
            var filteredAlerts = alerts
                .Where(a => a.CreatedAt >= command.FromDate && a.CreatedAt <= command.ToDate)
                .ToList();

            content.AppendLine($"Total Alerts: {filteredAlerts.Count}");
            content.AppendLine();

            if (filteredAlerts.Any())
            {
                content.AppendLine("ALERTS:");
                foreach (var alert in filteredAlerts.Take(20))
                {
                    content.AppendLine($"  - [{alert.CreatedAt:MM-dd HH:mm}] {alert.AlertName}");
                }

                if (filteredAlerts.Count > 20)
                    content.AppendLine($"  ... and {filteredAlerts.Count - 20} more");
            }
        }
        catch (Exception ex)
        {
            content.AppendLine($"Error fetching alerts: {ex.Message}");
        }

        content.AppendLine();
    }

    private async Task AddEmailActivity(
        StringBuilder content,
        GenerateReportCommand command,
        CancellationToken cancellationToken)
    {
        content.AppendLine("EMAIL ACTIVITY");
        content.AppendLine("?????????????????????????????????????????????????????");

        try
        {
            var emails = await emailRepository.GetAllAsync(cancellationToken);
            var filteredEmails = emails
                .Where(e => e.CreatedAt >= command.FromDate && e.CreatedAt <= command.ToDate)
                .ToList();

            content.AppendLine($"Total Emails: {filteredEmails.Count}");
            content.AppendLine($"Average per day: {(filteredEmails.Count > 0 ? (filteredEmails.Count / ((command.ToDate - command.FromDate).TotalDays + 1)) : 0):F1}");
            content.AppendLine();

            if (command.IncludeRawData && filteredEmails.Any())
            {
                content.AppendLine("RECENT EMAILS:");
                foreach (var email in filteredEmails.Take(10))
                {
                    content.AppendLine($"  - [{email.CreatedAt:MM-dd HH:mm}] From: {email.FromAddress}");
                }

                if (filteredEmails.Count > 10)
                    content.AppendLine($"  ... and {filteredEmails.Count - 10} more");
            }
        }
        catch (Exception ex)
        {
            content.AppendLine($"Error fetching emails: {ex.Message}");
        }

        content.AppendLine();
    }

    private async Task AddMetricsTrend(
        StringBuilder content,
        GenerateReportCommand command,
        CancellationToken cancellationToken)
    {
        content.AppendLine("METRICS TREND");
        content.AppendLine("?????????????????????????????????????????????????????");

        try
        {
            var metrics = await metricsRepository.GetFilteredMetricsAsync(
                null, null, command.FromDate, command.ToDate, 
                cancellationToken: cancellationToken);

            content.AppendLine($"Total Metric Data Points: {metrics.Count}");

            if (metrics.Count > 0)
            {
                var grouped = metrics.GroupBy(m => m.MetricName).ToList();
                content.AppendLine($"Unique Metrics: {grouped.Count}");
                content.AppendLine();
                content.AppendLine("METRICS BREAKDOWN:");

                foreach (var group in grouped.Take(10))
                {
                    var avg = group.Average(m => m.Value);
                    var max = group.Max(m => m.Value);
                    var min = group.Min(m => m.Value);
                    content.AppendLine($"  {group.Key}: Avg={avg:F2}, Max={max:F2}, Min={min:F2}");
                }
            }
        }
        catch (Exception ex)
        {
            content.AppendLine($"Error fetching metrics: {ex.Message}");
        }

        content.AppendLine();
    }

    private async Task AddAiChatSummary(
        StringBuilder content,
        GenerateReportCommand command,
        CancellationToken cancellationToken)
    {
        content.AppendLine("AI CHAT SUMMARY");
        content.AppendLine("?????????????????????????????????????????????????????");
        content.AppendLine("Chat session summary would be populated here in future versions.");
        content.AppendLine();

        await Task.CompletedTask;
    }

    private static string GetReportDirectory()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "MbarieInsightSuite", "Reports", DateTime.UtcNow.ToString("yyyy-MM"));
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
