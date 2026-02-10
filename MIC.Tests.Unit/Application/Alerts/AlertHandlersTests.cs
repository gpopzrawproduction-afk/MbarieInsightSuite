using System;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using FluentAssertions;
using MIC.Core.Application.Alerts.Commands.CreateAlert;
using MIC.Core.Application.Alerts.Commands.DeleteAlert;
using MIC.Core.Application.Alerts.Commands.UpdateAlert;
using MIC.Core.Application.Alerts.Queries.GetAlertById;
using MIC.Core.Application.Alerts.Queries.GetAllAlerts;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using Moq;
using Xunit;

namespace MIC.Tests.Unit.Application.Alerts;

/// <summary>
/// Comprehensive tests for Alert CQRS handlers.
/// Tests command and query handlers for business logic validation.
/// Target: 17 tests for alert handler coverage
/// </summary>
public class AlertHandlersTests
{
    private readonly Mock<IAlertRepository> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;

    public AlertHandlersTests()
    {
        _mockRepository = new Mock<IAlertRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
    }

    #region CreateAlertCommandHandler Tests (4 tests)

    [Fact]
    public async Task CreateAlertCommand_WithValidData_ReturnsAlertId()
    {
        // Arrange
        var command = new CreateAlertCommand(
            "Test Alert",
            "Test Description",
            AlertSeverity.Critical,
            "TestSource");

        var handler = new CreateAlertCommandHandler(_mockRepository.Object, _mockUnitOfWork.Object);

        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<IntelligenceAlert>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBe(Guid.Empty);
        _mockRepository.Verify(x => x.AddAsync(It.IsAny<IntelligenceAlert>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAlertCommand_CallsAddAsyncWithCorrectAlert()
    {
        // Arrange
        var command = new CreateAlertCommand(
            "Specific Alert",
            "Specific Description",
            AlertSeverity.Warning,
            "SpecificSource");

        var handler = new CreateAlertCommandHandler(_mockRepository.Object, _mockUnitOfWork.Object);

        IntelligenceAlert? capturedAlert = null;
        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<IntelligenceAlert>(), It.IsAny<CancellationToken>()))
            .Callback<IntelligenceAlert, CancellationToken>((alert, _) => capturedAlert = alert)
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        capturedAlert.Should().NotBeNull();
        capturedAlert!.AlertName.Should().Be("Specific Alert");
        capturedAlert.Description.Should().Be("Specific Description");
        capturedAlert.Severity.Should().Be(AlertSeverity.Warning);
        capturedAlert.Source.Should().Be("SpecificSource");
    }

    [Fact]
    public async Task CreateAlertCommand_WhenRepositoryThrows_ReturnsError()
    {
        // Arrange
        var command = new CreateAlertCommand("Alert", "Desc", AlertSeverity.Info, "Source");
        var handler = new CreateAlertCommandHandler(_mockRepository.Object, _mockUnitOfWork.Object);

        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<IntelligenceAlert>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CreateAlert.Failed");
        result.FirstError.Description.Should().Contain("Database error");
    }

    [Fact]
    public async Task CreateAlertCommand_WhenUnitOfWorkThrows_ReturnsError()
    {
        // Arrange
        var command = new CreateAlertCommand("Alert", "Desc", AlertSeverity.Info, "Source");
        var handler = new CreateAlertCommandHandler(_mockRepository.Object, _mockUnitOfWork.Object);

        _mockRepository.Setup(x => x.AddAsync(It.IsAny<IntelligenceAlert>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Save failed"));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CreateAlert.Failed");
    }

    #endregion

    #region DeleteAlertCommandHandler Tests (4 tests)

    [Fact]
    public async Task DeleteAlertCommand_WithExistingAlert_ReturnsSuccess()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var command = new DeleteAlertCommand(alertId, "TestUser");
        var existingAlert = new IntelligenceAlert("Alert", "Desc", AlertSeverity.Info, "Source");

        _mockRepository
            .Setup(x => x.GetByIdAsync(alertId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAlert);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new DeleteAlertCommandHandler(_mockRepository.Object, _mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeTrue();
        existingAlert.IsDeleted.Should().BeTrue();
        existingAlert.LastModifiedBy.Should().Be("TestUser");
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAlertCommand_WithNonExistentAlert_ReturnsNotFoundError()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var command = new DeleteAlertCommand(alertId, "TestUser");

        _mockRepository
            .Setup(x => x.GetByIdAsync(alertId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IntelligenceAlert?)null);

        var handler = new DeleteAlertCommandHandler(_mockRepository.Object, _mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
        result.FirstError.Code.Should().Be("Alert.NotFound");
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAlertCommand_WithAlreadyDeletedAlert_ReturnsConflictError()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var command = new DeleteAlertCommand(alertId, "TestUser");
        var existingAlert = new IntelligenceAlert("Alert", "Desc", AlertSeverity.Info, "Source");
        existingAlert.MarkAsDeleted("PreviousUser");

        _mockRepository
            .Setup(x => x.GetByIdAsync(alertId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAlert);

        var handler = new DeleteAlertCommandHandler(_mockRepository.Object, _mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Conflict);
        result.FirstError.Code.Should().Be("Alert.AlreadyDeleted");
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAlertCommand_MarkAsDeleted_SetsCorrectProperties()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var command = new DeleteAlertCommand(alertId, "TestUser");
        var existingAlert = new IntelligenceAlert("Alert", "Desc", AlertSeverity.Info, "Source");

        _mockRepository.Setup(x => x.GetByIdAsync(alertId, It.IsAny<CancellationToken>())).ReturnsAsync(existingAlert);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new DeleteAlertCommandHandler(_mockRepository.Object, _mockUnitOfWork.Object);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _mockRepository.Verify(x => x.GetByIdAsync(alertId, It.IsAny<CancellationToken>()), Times.Once);
        existingAlert.IsDeleted.Should().BeTrue();
        existingAlert.LastModifiedBy.Should().Be("TestUser");
        existingAlert.ModifiedAt.Should().NotBeNull();
    }

    #endregion

    #region UpdateAlertCommandHandler Tests (4 tests)

    [Fact]
    public async Task UpdateAlertCommand_WithMetadataUpdate_ReturnsUpdatedDto()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var command = new UpdateAlertCommand
        {
            AlertId = alertId,
            AlertName = "Updated Alert",
            Description = "Updated Desc",
            Severity = AlertSeverity.Critical,
            Source = "UpdatedSource",
            UpdatedBy = "TestUser"
        };
        var existingAlert = new IntelligenceAlert("Original", "Original Desc", AlertSeverity.Info, "OriginalSource");

        _mockRepository.Setup(x => x.GetByIdAsync(alertId, It.IsAny<CancellationToken>())).ReturnsAsync(existingAlert);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new UpdateAlertCommandHandler(_mockRepository.Object, _mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.AlertName.Should().Be("Updated Alert");
        result.Value.Severity.Should().Be(AlertSeverity.Critical);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAlertCommand_WithNonExistentAlert_ReturnsNotFoundError()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var command = new UpdateAlertCommand
        {
            AlertId = alertId,
            AlertName = "Updated",
            UpdatedBy = "TestUser"
        };

        _mockRepository.Setup(x => x.GetByIdAsync(alertId, It.IsAny<CancellationToken>())).ReturnsAsync((IntelligenceAlert?)null);

        var handler = new UpdateAlertCommandHandler(_mockRepository.Object, _mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
        result.FirstError.Code.Should().Be("Alert.NotFound");
    }

    [Fact]
    public async Task UpdateAlertCommand_WithAcknowledgeStatus_AcknowledgesAlert()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var command = new UpdateAlertCommand
        {
            AlertId = alertId,
            NewStatus = AlertStatus.Acknowledged,
            UpdatedBy = "TestUser"
        };
        var existingAlert = new IntelligenceAlert("Alert", "Desc", AlertSeverity.Info, "Source");

        _mockRepository.Setup(x => x.GetByIdAsync(alertId, It.IsAny<CancellationToken>())).ReturnsAsync(existingAlert);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new UpdateAlertCommandHandler(_mockRepository.Object, _mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Status.Should().Be(AlertStatus.Acknowledged);
        result.Value.AcknowledgedBy.Should().Be("TestUser");
    }

    [Fact]
    public async Task UpdateAlertCommand_WithResolveStatus_ResolvesAlert()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var command = new UpdateAlertCommand
        {
            AlertId = alertId,
            NewStatus = AlertStatus.Resolved,
            ResolutionNotes = "Issue fixed",
            UpdatedBy = "TestUser"
        };
        var existingAlert = new IntelligenceAlert("Alert", "Desc", AlertSeverity.Critical, "Source");

        _mockRepository.Setup(x => x.GetByIdAsync(alertId, It.IsAny<CancellationToken>())).ReturnsAsync(existingAlert);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new UpdateAlertCommandHandler(_mockRepository.Object, _mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Status.Should().Be(AlertStatus.Resolved);
        result.Value.ResolvedBy.Should().Be("TestUser");
        result.Value.Resolution.Should().Be("Issue fixed");
    }

    #endregion

    #region GetAlertByIdQueryHandler Tests (3 tests)

    [Fact]
    public async Task GetAlertByIdQuery_WithExistingAlert_ReturnsAlertDto()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var query = new GetAlertByIdQuery(alertId);
        var alert = new IntelligenceAlert("Test Alert", "Description", AlertSeverity.Critical, "TestSource");

        _mockRepository.Setup(x => x.GetByIdAsync(alertId, It.IsAny<CancellationToken>())).ReturnsAsync(alert);

        var handler = new GetAlertByIdQueryHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.AlertName.Should().Be("Test Alert");
        result.Value.Severity.Should().Be(AlertSeverity.Critical);
    }

    [Fact]
    public async Task GetAlertByIdQuery_WithNonExistentAlert_ReturnsNotFoundError()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var query = new GetAlertByIdQuery(alertId);

        _mockRepository.Setup(x => x.GetByIdAsync(alertId, It.IsAny<CancellationToken>())).ReturnsAsync((IntelligenceAlert?)null);

        var handler = new GetAlertByIdQueryHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
        result.FirstError.Code.Should().Be("Alert.NotFound");
        result.FirstError.Description.Should().Contain(alertId.ToString());
    }

    [Fact]
    public async Task GetAlertByIdQuery_ReturnsMappedDto()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var query = new GetAlertByIdQuery(alertId);
        var alert = new IntelligenceAlert("Mapped Alert", "Mapped Description", AlertSeverity.Warning, "MappedSource");

        _mockRepository.Setup(x => x.GetByIdAsync(alertId, It.IsAny<CancellationToken>())).ReturnsAsync(alert);

        var handler = new GetAlertByIdQueryHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        var dto = result.Value;
        dto.AlertName.Should().Be("Mapped Alert");
        dto.Description.Should().Be("Mapped Description");
        dto.Severity.Should().Be(AlertSeverity.Warning);
        dto.Source.Should().Be("MappedSource");
        dto.Status.Should().Be(AlertStatus.Active);
    }

    #endregion

    #region GetAllAlertsQueryHandler Tests (2 tests)

    [Fact]
    public async Task GetAllAlertsQuery_WithMultipleAlerts_ReturnsAllAlertDtos()
    {
        // Arrange
        var query = new GetAllAlertsQuery();
        var alerts = new List<IntelligenceAlert>
        {
            new IntelligenceAlert("Alert 1", "Desc 1", AlertSeverity.Info, "Source1"),
            new IntelligenceAlert("Alert 2", "Desc 2", AlertSeverity.Warning, "Source2"),
            new IntelligenceAlert("Alert 3", "Desc 3", AlertSeverity.Critical, "Source3")
        };

        _mockRepository.Setup(x => x.GetFilteredAlertsAsync(
            It.IsAny<AlertSeverity?>(),
            It.IsAny<AlertStatus?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<string?>(),
            It.IsAny<int?>(),
            It.IsAny<int?>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(alerts);

        var handler = new GetAllAlertsQueryHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(3);
        result.Value.Should().Contain(dto => dto.AlertName == "Alert 1");
        result.Value.Should().Contain(dto => dto.AlertName == "Alert 2");
        result.Value.Should().Contain(dto => dto.AlertName == "Alert 3");
    }

    [Fact]
    public async Task GetAllAlertsQuery_WithNoAlerts_ReturnsEmptyList()
    {
        // Arrange
        var query = new GetAllAlertsQuery();
        _mockRepository.Setup(x => x.GetFilteredAlertsAsync(
            It.IsAny<AlertSeverity?>(),
            It.IsAny<AlertStatus?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<string?>(),
            It.IsAny<int?>(),
            It.IsAny<int?>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(new List<IntelligenceAlert>());

        var handler = new GetAllAlertsQueryHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeEmpty();
    }

    #endregion
}
