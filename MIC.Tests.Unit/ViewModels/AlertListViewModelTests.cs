using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using FluentAssertions;
using MediatR;
using MIC.Core.Application.Alerts.Commands.DeleteAlert;
using MIC.Core.Application.Alerts.Commands.UpdateAlert;
using MIC.Core.Application.Alerts.Common;
using MIC.Core.Application.Alerts.Queries.GetAllAlerts;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using MIC.Desktop.Avalonia.Services;
using MIC.Desktop.Avalonia.ViewModels;
using NSubstitute;
using Xunit;

namespace MIC.Tests.Unit.ViewModels;

/// <summary>
/// Tests for AlertListViewModel covering initialization, commands, and alert management.
/// Target: 20 tests for core alert list functionality coverage
/// </summary>
[Collection("UserSessionServiceTests")]
public class AlertListViewModelTests : IDisposable
{
    private readonly IMediator _mediator;
    private readonly IErrorHandlingService _errorHandlingService;
    private readonly SessionStorageScope _sessionScope;

    public AlertListViewModelTests()
    {
        _mediator = Substitute.For<IMediator>();
        _errorHandlingService = Substitute.For<IErrorHandlingService>();
        
        // Set up session for UserSessionService
        _sessionScope = new SessionStorageScope();
        UserSessionService.Instance.SetSession("test-user-id", "testuser", "test@example.com", "Test User", "test-token", "Developer", "Engineering");
    }

    public void Dispose()
    {
        _sessionScope.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_InitializesProperties()
    {
        // Act
        var viewModel = new AlertListViewModel(_mediator, _errorHandlingService);

        // Assert
        viewModel.Alerts.Should().NotBeNull().And.BeEmpty();
        viewModel.SeverityOptions.Should().NotBeNull().And.HaveCount(5);
        viewModel.StatusOptions.Should().NotBeNull().And.HaveCount(5);
        viewModel.IsLoading.Should().BeFalse();
        viewModel.SearchText.Should().BeEmpty();
        viewModel.SelectedSeverity.Should().BeNull();
        viewModel.SelectedStatus.Should().BeNull();
        viewModel.SelectedAlert.Should().BeNull();
    }

    [Fact]
    public void Constructor_InitializesCommands()
    {
        // Act
        var viewModel = new AlertListViewModel(_mediator, _errorHandlingService);

        // Assert
        viewModel.RefreshCommand.Should().NotBeNull();
        viewModel.CreateAlertCommand.Should().NotBeNull();
        viewModel.EditAlertCommand.Should().NotBeNull();
        viewModel.DeleteAlertCommand.Should().NotBeNull();
        viewModel.ViewDetailsCommand.Should().NotBeNull();
        viewModel.AcknowledgeAlertCommand.Should().NotBeNull();
        viewModel.ExportCommand.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ThrowsOnNullMediator()
    {
        // Act
        var act = () => new AlertListViewModel(null!, _errorHandlingService);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("mediator");
    }

    [Fact]
    public void Constructor_ThrowsOnNullErrorHandlingService()
    {
        // Act
        var act = () => new AlertListViewModel(_mediator, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("errorHandlingService");
    }

    [Fact]
    public void Constructor_InitializesSeverityOptions()
    {
        // Act
        var viewModel = new AlertListViewModel(_mediator, _errorHandlingService);

        // Assert
        viewModel.SeverityOptions[0].Display.Should().Be("All");
        viewModel.SeverityOptions[0].Value.Should().BeNull();
        viewModel.SeverityOptions[1].Display.Should().Be("Info");
        viewModel.SeverityOptions[1].Value.Should().Be(AlertSeverity.Info);
        viewModel.SeverityOptions[2].Display.Should().Be("Warning");
        viewModel.SeverityOptions[2].Value.Should().Be(AlertSeverity.Warning);
        viewModel.SeverityOptions[3].Display.Should().Be("Critical");
        viewModel.SeverityOptions[3].Value.Should().Be(AlertSeverity.Critical);
        viewModel.SeverityOptions[4].Display.Should().Be("Emergency");
        viewModel.SeverityOptions[4].Value.Should().Be(AlertSeverity.Emergency);
    }

    [Fact]
    public void Constructor_InitializesStatusOptions()
    {
        // Act
        var viewModel = new AlertListViewModel(_mediator, _errorHandlingService);

        // Assert
        viewModel.StatusOptions[0].Display.Should().Be("All");
        viewModel.StatusOptions[0].Value.Should().BeNull();
        viewModel.StatusOptions[1].Display.Should().Be("Active");
        viewModel.StatusOptions[1].Value.Should().Be(AlertStatus.Active);
        viewModel.StatusOptions[2].Display.Should().Be("Acknowledged");
        viewModel.StatusOptions[2].Value.Should().Be(AlertStatus.Acknowledged);
        viewModel.StatusOptions[3].Display.Should().Be("Resolved");
        viewModel.StatusOptions[3].Value.Should().Be(AlertStatus.Resolved);
        viewModel.StatusOptions[4].Display.Should().Be("Escalated");
        viewModel.StatusOptions[4].Value.Should().Be(AlertStatus.Escalated);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void IsLoading_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        var viewModel = new AlertListViewModel(_mediator, _errorHandlingService);
        var propertyChangedCount = 0;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(viewModel.IsLoading))
                propertyChangedCount++;
        };

        // Act
        viewModel.IsLoading = true;

        // Assert
        propertyChangedCount.Should().BeGreaterThan(0);
        viewModel.IsLoading.Should().BeTrue();
    }

    [Fact]
    public void SearchText_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        var viewModel = new AlertListViewModel(_mediator, _errorHandlingService);
        var propertyChangedCount = 0;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(viewModel.SearchText))
                propertyChangedCount++;
        };

        // Act
        viewModel.SearchText = "test search";

        // Assert
        propertyChangedCount.Should().BeGreaterThan(0);
        viewModel.SearchText.Should().Be("test search");
    }

    [Fact]
    public void SelectedSeverity_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        var viewModel = new AlertListViewModel(_mediator, _errorHandlingService);
        var propertyChangedCount = 0;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(viewModel.SelectedSeverity))
                propertyChangedCount++;
        };

        // Act
        viewModel.SelectedSeverity = AlertSeverity.Critical;

        // Assert
        propertyChangedCount.Should().BeGreaterThan(0);
        viewModel.SelectedSeverity.Should().Be(AlertSeverity.Critical);
    }

    [Fact]
    public void SelectedStatus_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        var viewModel = new AlertListViewModel(_mediator, _errorHandlingService);
        var propertyChangedCount = 0;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(viewModel.SelectedStatus))
                propertyChangedCount++;
        };

        // Act
        viewModel.SelectedStatus = AlertStatus.Acknowledged;

        // Assert
        propertyChangedCount.Should().BeGreaterThan(0);
        viewModel.SelectedStatus.Should().Be(AlertStatus.Acknowledged);
    }

    [Fact]
    public void SelectedAlert_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        var viewModel = new AlertListViewModel(_mediator, _errorHandlingService);
        var alert = CreateSampleAlertDto();
        var propertyChangedCount = 0;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(viewModel.SelectedAlert))
                propertyChangedCount++;
        };

        // Act
        viewModel.SelectedAlert = alert;

        // Assert
        propertyChangedCount.Should().BeGreaterThan(0);
        viewModel.SelectedAlert.Should().Be(alert);
    }

    [Fact]
    public void StatusMessage_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        var viewModel = new AlertListViewModel(_mediator, _errorHandlingService);
        var propertyChangedCount = 0;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(viewModel.StatusMessage))
                propertyChangedCount++;
        };

        // Act
        viewModel.StatusMessage = "Test status";

        // Assert
        propertyChangedCount.Should().BeGreaterThan(0);
        viewModel.StatusMessage.Should().Be("Test status");
    }

    #endregion

    #region Command Tests

    [Fact]
    public void CreateAlertCommand_RaisesCreateAlertRequestedEvent()
    {
        // Arrange
        var viewModel = new AlertListViewModel(_mediator, _errorHandlingService);
        var eventRaised = false;
        viewModel.CreateAlertRequested += (sender, e) => eventRaised = true;

        // Act
        viewModel.CreateAlertCommand.Execute().Subscribe();

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void EditAlertCommand_WithValidAlert_RaisesEditAlertRequestedEvent()
    {
        // Arrange
        var viewModel = new AlertListViewModel(_mediator, _errorHandlingService);
        var alert = CreateSampleAlertDto();
        AlertDto? receivedAlert = null;
        viewModel.EditAlertRequested += (sender, e) => receivedAlert = e;

        // Act
        viewModel.EditAlertCommand.Execute(alert).Subscribe();

        // Assert
        receivedAlert.Should().Be(alert);
    }

    [Fact]
    public void EditAlertCommand_WithNullAlert_DoesNotRaiseEvent()
    {
        // Arrange
        var viewModel = new AlertListViewModel(_mediator, _errorHandlingService);
        var eventRaised = false;
        viewModel.EditAlertRequested += (sender, e) => eventRaised = true;

        // Act
        viewModel.EditAlertCommand.Execute(null!).Subscribe();

        // Assert
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public void ViewDetailsCommand_WithValidAlert_RaisesViewDetailsRequestedEvent()
    {
        // Arrange
        var viewModel = new AlertListViewModel(_mediator, _errorHandlingService);
        var alert = CreateSampleAlertDto();
        AlertDto? receivedAlert = null;
        viewModel.ViewDetailsRequested += (sender, e) => receivedAlert = e;

        // Act
        viewModel.ViewDetailsCommand.Execute(alert).Subscribe();

        // Assert
        receivedAlert.Should().Be(alert);
        viewModel.SelectedAlert.Should().Be(alert);
    }

    [Fact]
    public void ViewDetailsCommand_WithNullAlert_DoesNotRaiseEvent()
    {
        // Arrange
        var viewModel = new AlertListViewModel(_mediator, _errorHandlingService);
        var eventRaised = false;
        viewModel.ViewDetailsRequested += (sender, e) => eventRaised = true;

        // Act
        viewModel.ViewDetailsCommand.Execute(null!).Subscribe();

        // Assert
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public void DeleteAlertCommand_WithNullAlert_DoesNothing()
    {
        // Arrange
        var viewModel = new AlertListViewModel(_mediator, _errorHandlingService);

        // Act
        viewModel.DeleteAlertCommand.Execute(null!).Subscribe();

        // Assert
        // No exception thrown
    }

    [Fact]
    public void AcknowledgeAlertCommand_WithNullAlert_DoesNothing()
    {
        // Arrange
        var viewModel = new AlertListViewModel(_mediator, _errorHandlingService);

        // Act
        viewModel.AcknowledgeAlertCommand.Execute(null!).Subscribe();

        // Assert
        // No exception thrown
    }

    #endregion

    #region Helper Methods

    private static AlertDto CreateSampleAlertDto()
    {
        return new AlertDto
        {
            Id = Guid.NewGuid(),
            AlertName = "Test Alert",
            Description = "Test message",
            Severity = AlertSeverity.Warning,
            Status = AlertStatus.Active,
            Source = "Test",
            TriggeredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion

    /// <summary>
    /// Helper class to manage UserSessionService state isolation in tests.
    /// </summary>
    private sealed class SessionStorageScope : IDisposable
    {
        private readonly string _sessionPath;
        private readonly string? _backupPath;
        private readonly bool _hadExisting;

        public SessionStorageScope()
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MIC");
            Directory.CreateDirectory(directory);

            _sessionPath = Path.Combine(directory, "session.json");

            if (File.Exists(_sessionPath))
            {
                _backupPath = Path.Combine(Path.GetTempPath(), $"mic-session-backup-{Guid.NewGuid():N}.json");
                File.Copy(_sessionPath, _backupPath, overwrite: true);
                _hadExisting = true;
            }

            // Nothing to do - session is already backed up if it exists
        }

        public void Dispose()
        {
            try
            {
                if (_hadExisting && _backupPath != null && File.Exists(_backupPath))
                {
                    File.Copy(_backupPath, _sessionPath, overwrite: true);
                    File.Delete(_backupPath);
                }
                else if (File.Exists(_sessionPath))
                {
                    File.Delete(_sessionPath);
                }
            }
            catch
            {
                // Ignore cleanup failures
            }
        }
    }
}
