using FluentAssertions;
using Moq;
using Xunit;
using MIC.Core.Application.Reports.Commands.GenerateReport;
using MIC.Core.Application.Reports.Commands.DeleteReport;
using MIC.Core.Application.Reports.Queries.GetReports;
using MIC.Core.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace MIC.Tests.Unit.Features.Reports;

public sealed class GenerateReportCommandHandlerTests
{
    private readonly Mock<IAlertRepository> _alertRepositoryMock;
    private readonly Mock<IEmailRepository> _emailRepositoryMock;
    private readonly Mock<IMetricsRepository> _metricsRepositoryMock;
    private readonly Mock<IReportRepository> _reportRepositoryMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<ILogger<GenerateReportCommandHandler>> _loggerMock;
    private readonly GenerateReportCommandHandler _handler;

    public GenerateReportCommandHandlerTests()
    {
        _alertRepositoryMock = new Mock<IAlertRepository>();
        _emailRepositoryMock = new Mock<IEmailRepository>();
        _metricsRepositoryMock = new Mock<IMetricsRepository>();
        _reportRepositoryMock = new Mock<IReportRepository>();
        _notificationServiceMock = new Mock<INotificationService>();
        _loggerMock = new Mock<ILogger<GenerateReportCommandHandler>>();

        _handler = new GenerateReportCommandHandler(
            _alertRepositoryMock.Object,
            _emailRepositoryMock.Object,
            _metricsRepositoryMock.Object,
            _reportRepositoryMock.Object,
            _notificationServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_AlertSummary_CreatesFileAndReturnsDto()
    {
        // Arrange
        var command = new GenerateReportCommand
        {
            Type = ReportType.AlertSummary,
            FromDate = DateTime.UtcNow.AddDays(-7),
            ToDate = DateTime.UtcNow,
            Format = ReportFormat.PDF
        };

        _alertRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<dynamic>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.ReportId.Should().NotBe(Guid.Empty);
        result.Value.FileName.Should().Contain("AlertSummary");
        result.Value.Type.Should().Be(ReportType.AlertSummary);
        result.Value.FileSizeBytes.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Handle_InvalidDateRange_ReturnsError()
    {
        // Arrange - ToDate before FromDate
        var command = new GenerateReportCommand
        {
            Type = ReportType.MetricsTrend,
            FromDate = DateTime.UtcNow,
            ToDate = DateTime.UtcNow.AddDays(-7),
            Format = ReportFormat.CSV
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_EmailActivity_FetchesEmailsInRange()
    {
        // Arrange
        var command = new GenerateReportCommand
        {
            Type = ReportType.EmailActivity,
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            Format = ReportFormat.XLSX
        };

        _emailRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<dynamic>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Type.Should().Be(ReportType.EmailActivity);
        _emailRepositoryMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MetricsTrend_FetchesMetricsData()
    {
        // Arrange
        var command = new GenerateReportCommand
        {
            Type = ReportType.MetricsTrend,
            FromDate = DateTime.UtcNow.AddDays(-7),
            ToDate = DateTime.UtcNow,
            Format = ReportFormat.CSV
        };

        _metricsRepositoryMock.Setup(r => r.GetFilteredMetricsAsync(
                null, null, command.FromDate, command.ToDate,
                null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<dynamic>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Type.Should().Be(ReportType.MetricsTrend);
    }

    [Fact]
    public async Task Handle_NotifiesUserOnSuccess()
    {
        // Arrange
        var command = new GenerateReportCommand
        {
            Type = ReportType.AlertSummary,
            FromDate = DateTime.UtcNow.AddDays(-7),
            ToDate = DateTime.UtcNow
        };

        _alertRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<dynamic>());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _notificationServiceMock.Verify(
            n => n.ShowSuccess(It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_SavesReportToRepository()
    {
        // Arrange
        var command = new GenerateReportCommand
        {
            Type = ReportType.AlertSummary,
            FromDate = DateTime.UtcNow.AddDays(-7),
            ToDate = DateTime.UtcNow
        };

        _alertRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<dynamic>());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _reportRepositoryMock.Verify(r => r.AddAsync(It.IsAny<dynamic>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

public sealed class DeleteReportCommandHandlerTests
{
    [Fact]
    public async Task Handle_ExistingReport_DeletesSuccessfully()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var command = new DeleteReportCommand { ReportId = reportId };

        var reportRepositoryMock = new Mock<IReportRepository>();
        var loggerMock = new Mock<ILogger<DeleteReportCommandHandler>>();

        var mockReport = new { Id = reportId, OutputFilePath = "" };
        reportRepositoryMock.Setup(r => r.GetByIdAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockReport);

        var handler = new DeleteReportCommandHandler(reportRepositoryMock.Object, loggerMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        reportRepositoryMock.Verify(r => r.DeleteAsync(reportId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentReport_ReturnsNotFoundError()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var command = new DeleteReportCommand { ReportId = reportId };

        var reportRepositoryMock = new Mock<IReportRepository>();
        var loggerMock = new Mock<ILogger<DeleteReportCommandHandler>>();

        reportRepositoryMock.Setup(r => r.GetByIdAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((dynamic?)null);

        var handler = new DeleteReportCommandHandler(reportRepositoryMock.Object, loggerMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Contain("not_found");
    }
}

public sealed class GetReportsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsPaginatedResults()
    {
        // Arrange
        var query = new GetReportsQuery { PageNumber = 1, PageSize = 10 };
        var reportRepositoryMock = new Mock<IReportRepository>();
        var loggerMock = new Mock<ILogger<GetReportsQueryHandler>>();

        reportRepositoryMock.Setup(r => r.GetPagedAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<dynamic>());

        var handler = new GetReportsQueryHandler(reportRepositoryMock.Object, loggerMock.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeOfType<List<ReportSummaryDto>>();
    }
}
