using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using FluentAssertions;
using MediatR;
using MIC.Core.Application.Alerts.Common;
using MIC.Core.Application.Alerts.Queries.GetAlertById;
using MIC.Core.Domain.Entities;
using MIC.Desktop.Avalonia.ViewModels;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ReactiveUI;
using Xunit;

namespace MIC.Tests.Unit.ViewModels;

/// <summary>
/// Tests for AlertDetailsViewModel covering alert viewing, creation, and editing.
/// </summary>
public class AlertDetailsViewModelTests
{
    private readonly IMediator _mediator;

    static AlertDetailsViewModelTests()
    {
        RxApp.MainThreadScheduler = CurrentThreadScheduler.Instance;
        RxApp.TaskpoolScheduler = CurrentThreadScheduler.Instance;
    }

    public AlertDetailsViewModelTests()
    {
        _mediator = Substitute.For<IMediator>();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_InitializesDefaultProperties()
    {
        var vm = new AlertDetailsViewModel(_mediator);

        vm.AlertName.Should().BeEmpty();
        vm.Description.Should().BeEmpty();
        vm.Source.Should().BeEmpty();
        vm.Severity.Should().Be(AlertSeverity.Info);
        vm.Status.Should().Be(AlertStatus.Active);
        vm.IsEditMode.Should().BeFalse();
        vm.IsLoading.Should().BeFalse();
        vm.IsNewAlert.Should().BeFalse();
        vm.ErrorMessage.Should().BeEmpty();
        vm.WindowTitle.Should().Be("Alert Details");
    }

    [Fact]
    public void Constructor_InitializesCommands()
    {
        var vm = new AlertDetailsViewModel(_mediator);

        vm.SaveCommand.Should().NotBeNull();
        vm.CancelCommand.Should().NotBeNull();
        vm.ToggleEditModeCommand.Should().NotBeNull();
        vm.AcknowledgeCommand.Should().NotBeNull();
        vm.ResolveCommand.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_InitializesSeverityOptions()
    {
        var vm = new AlertDetailsViewModel(_mediator);

        vm.SeverityOptions.Should().HaveCount(4);
        vm.SeverityOptions.Should().Contain(AlertSeverity.Info);
        vm.SeverityOptions.Should().Contain(AlertSeverity.Warning);
        vm.SeverityOptions.Should().Contain(AlertSeverity.Critical);
        vm.SeverityOptions.Should().Contain(AlertSeverity.Emergency);
    }

    [Fact]
    public void Constructor_InitializesStatusOptions()
    {
        var vm = new AlertDetailsViewModel(_mediator);

        vm.StatusOptions.Should().HaveCount(4);
        vm.StatusOptions.Should().Contain(AlertStatus.Active);
        vm.StatusOptions.Should().Contain(AlertStatus.Acknowledged);
        vm.StatusOptions.Should().Contain(AlertStatus.Resolved);
        vm.StatusOptions.Should().Contain(AlertStatus.Escalated);
    }

    [Fact]
    public void Constructor_ThrowsOnNullMediator()
    {
        var act = () => new AlertDetailsViewModel(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region InitializeForNew Tests

    [Fact]
    public void InitializeForNew_SetsNewAlertProperties()
    {
        var vm = new AlertDetailsViewModel(_mediator);

        vm.InitializeForNew();

        vm.IsNewAlert.Should().BeTrue();
        vm.IsEditMode.Should().BeTrue();
        vm.WindowTitle.Should().Be("Create New Alert");
        vm.AlertId.Should().BeNull();
        vm.AlertName.Should().BeEmpty();
        vm.Description.Should().BeEmpty();
        vm.Severity.Should().Be(AlertSeverity.Warning);
        vm.Status.Should().Be(AlertStatus.Active);
        vm.Source.Should().BeEmpty();
        vm.ErrorMessage.Should().BeEmpty();
    }

    #endregion

    #region LoadFromDto Tests

    [Fact]
    public void LoadFromDto_CorrectlyMapsAllProperties()
    {
        var vm = new AlertDetailsViewModel(_mediator);
        var alertId = Guid.NewGuid();
        var dto = new AlertDto
        {
            Id = alertId,
            AlertName = "CPU Alert",
            Description = "CPU usage exceeded 90%",
            Severity = AlertSeverity.Critical,
            Status = AlertStatus.Active,
            Source = "Server Monitor",
            TriggeredAt = new DateTime(2026, 1, 15),
            AcknowledgedAt = new DateTime(2026, 1, 15, 1, 0, 0),
            AcknowledgedBy = "admin",
            ResolvedAt = null,
            ResolvedBy = null,
            Resolution = null
        };

        vm.LoadFromDto(dto);

        vm.AlertId.Should().Be(alertId);
        vm.AlertName.Should().Be("CPU Alert");
        vm.Description.Should().Be("CPU usage exceeded 90%");
        vm.Severity.Should().Be(AlertSeverity.Critical);
        vm.Status.Should().Be(AlertStatus.Active);
        vm.Source.Should().Be("Server Monitor");
        vm.TriggeredAt.Should().Be(new DateTime(2026, 1, 15));
        vm.AcknowledgedBy.Should().Be("admin");
        vm.IsEditMode.Should().BeFalse();
        vm.IsNewAlert.Should().BeFalse();
    }

    [Fact]
    public void LoadFromDto_SetsCorrectWindowTitle()
    {
        var vm = new AlertDetailsViewModel(_mediator);

        vm.LoadFromDto(new AlertDto { AlertName = "Test" });

        vm.WindowTitle.Should().Be("Alert Details");
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var vm = new AlertDetailsViewModel(_mediator);

        vm.AlertName = "Test Alert";
        vm.Description = "Test Description";
        vm.Source = "Test Source";
        vm.Severity = AlertSeverity.Emergency;
        vm.Status = AlertStatus.Escalated;
        vm.Notes = "Important note";
        vm.ResolutionNotes = "Fixed it";

        vm.AlertName.Should().Be("Test Alert");
        vm.Description.Should().Be("Test Description");
        vm.Source.Should().Be("Test Source");
        vm.Severity.Should().Be(AlertSeverity.Emergency);
        vm.Status.Should().Be(AlertStatus.Escalated);
        vm.Notes.Should().Be("Important note");
        vm.ResolutionNotes.Should().Be("Fixed it");
    }

    [Fact]
    public void CanAcknowledge_IsTrueWhenActive()
    {
        var vm = new AlertDetailsViewModel(_mediator);
        vm.Status = AlertStatus.Active;

        vm.CanAcknowledge.Should().BeTrue();
    }

    [Fact]
    public void CanAcknowledge_IsFalseWhenNotActive()
    {
        var vm = new AlertDetailsViewModel(_mediator);
        vm.Status = AlertStatus.Acknowledged;

        vm.CanAcknowledge.Should().BeFalse();
    }

    [Fact]
    public void CanResolve_IsTrueWhenNotResolved()
    {
        var vm = new AlertDetailsViewModel(_mediator);
        vm.Status = AlertStatus.Active;

        vm.CanResolve.Should().BeTrue();
    }

    [Fact]
    public void CanResolve_IsFalseWhenResolved()
    {
        var vm = new AlertDetailsViewModel(_mediator);
        vm.Status = AlertStatus.Resolved;

        vm.CanResolve.Should().BeFalse();
    }

    [Fact]
    public void HasResolution_IsTrueWhenResolutionSet()
    {
        var vm = new AlertDetailsViewModel(_mediator);

        vm.LoadFromDto(new AlertDto { Resolution = "Issue resolved" });

        vm.HasResolution.Should().BeTrue();
    }

    [Fact]
    public void HasResolution_IsFalseWhenNoResolution()
    {
        var vm = new AlertDetailsViewModel(_mediator);

        vm.LoadFromDto(new AlertDto { Resolution = null });

        vm.HasResolution.Should().BeFalse();
    }

    [Fact]
    public void IsViewMode_IsOppositeOfEditMode()
    {
        var vm = new AlertDetailsViewModel(_mediator);

        vm.IsEditMode = false;
        vm.IsViewMode.Should().BeTrue();

        vm.IsEditMode = true;
        vm.IsViewMode.Should().BeFalse();
    }

    #endregion

    #region InitializeForEditAsync Tests

    [Fact]
    public async Task InitializeForEditAsync_LoadsAlertFromMediator()
    {
        var alertId = Guid.NewGuid();
        var dto = new AlertDto
        {
            Id = alertId,
            AlertName = "Loaded Alert",
            Description = "From DB",
            Severity = AlertSeverity.Warning,
            Status = AlertStatus.Active,
            Source = "System"
        };

        ErrorOr<AlertDto> successResult = dto;
        _mediator.Send(Arg.Any<GetAlertByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(successResult);

        var vm = new AlertDetailsViewModel(_mediator);

        await vm.InitializeForEditAsync(alertId);

        vm.AlertName.Should().Be("Loaded Alert");
        vm.Description.Should().Be("From DB");
        vm.IsNewAlert.Should().BeFalse();
        vm.IsEditMode.Should().BeFalse();
    }

    [Fact]
    public async Task InitializeForEditAsync_SetsErrorMessage_WhenMediatorReturnsError()
    {
        ErrorOr<AlertDto> errorResult = Error.NotFound("Alert.NotFound", "Alert was not found");
        _mediator.Send(Arg.Any<GetAlertByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(errorResult);

        var vm = new AlertDetailsViewModel(_mediator);

        await vm.InitializeForEditAsync(Guid.NewGuid());

        vm.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task InitializeForEditAsync_SetsErrorMessage_WhenExceptionThrown()
    {
        _mediator.Send(Arg.Any<GetAlertByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns<ErrorOr<AlertDto>>(_ => throw new InvalidOperationException("DB connection failed"));

        var vm = new AlertDetailsViewModel(_mediator);

        await vm.InitializeForEditAsync(Guid.NewGuid());

        vm.ErrorMessage.Should().Contain("DB connection failed");
        vm.IsLoading.Should().BeFalse();
    }

    #endregion

    #region CloseRequested Event Tests

    [Fact]
    public void CancelCommand_RaisesCloseRequestedEvent()
    {
        var vm = new AlertDetailsViewModel(_mediator);
        bool? closeResult = null;
        vm.CloseRequested += (_, result) => closeResult = result;

        vm.CancelCommand.Execute().Subscribe();

        closeResult.Should().BeFalse();
    }

    #endregion

    #region ToggleEditMode Tests

    [Fact]
    public void ToggleEditModeCommand_TogglesEditMode()
    {
        var vm = new AlertDetailsViewModel(_mediator);
        vm.IsEditMode.Should().BeFalse();

        vm.ToggleEditModeCommand.Execute().Subscribe();

        vm.IsEditMode.Should().BeTrue();
    }

    [Fact]
    public void ToggleEditModeCommand_TogglesBackToViewMode()
    {
        var vm = new AlertDetailsViewModel(_mediator);
        vm.IsEditMode = true;

        vm.ToggleEditModeCommand.Execute().Subscribe();

        vm.IsEditMode.Should().BeFalse();
        vm.IsViewMode.Should().BeTrue();
    }

    #endregion

    #region SaveCommand Validation Tests

    [Fact]
    public async Task SaveCommand_EmptyAlertName_SetsErrorMessage()
    {
        var vm = new AlertDetailsViewModel(_mediator);
        vm.InitializeForNew();
        vm.AlertName = "";
        vm.Description = "Valid";
        vm.Source = "Valid";

        await vm.SaveCommand.Execute().ToTask();

        vm.ErrorMessage.Should().Contain("Alert name is required");
    }

    [Fact]
    public async Task SaveCommand_EmptyDescription_SetsErrorMessage()
    {
        var vm = new AlertDetailsViewModel(_mediator);
        vm.InitializeForNew();
        vm.AlertName = "Valid";
        vm.Description = "";
        vm.Source = "Valid";

        await vm.SaveCommand.Execute().ToTask();

        vm.ErrorMessage.Should().Contain("Description is required");
    }

    [Fact]
    public async Task SaveCommand_EmptySource_SetsErrorMessage()
    {
        var vm = new AlertDetailsViewModel(_mediator);
        vm.InitializeForNew();
        vm.AlertName = "Valid";
        vm.Description = "Valid";
        vm.Source = "";

        await vm.SaveCommand.Execute().ToTask();

        vm.ErrorMessage.Should().Contain("Source is required");
    }

    [Fact]
    public async Task SaveCommand_NewAlert_SendsCreateCommand()
    {
        var newId = Guid.NewGuid();
        ErrorOr<Guid> successResult = newId;
        _mediator.Send(Arg.Any<MIC.Core.Application.Alerts.Commands.CreateAlert.CreateAlertCommand>(), Arg.Any<CancellationToken>())
            .Returns(successResult);

        var vm = new AlertDetailsViewModel(_mediator);
        vm.InitializeForNew();
        vm.AlertName = "Test Alert";
        vm.Description = "Test Description";
        vm.Source = "Test Source";
        vm.Severity = AlertSeverity.Critical;

        bool closedWithSave = false;
        vm.CloseRequested += (_, result) => closedWithSave = result;

        await vm.SaveCommand.Execute().ToTask();

        vm.AlertId.Should().Be(newId);
        closedWithSave.Should().BeTrue();
    }

    [Fact]
    public async Task SaveCommand_NewAlert_Error_SetsErrorMessage()
    {
        ErrorOr<Guid> errorResult = Error.Failure("Alert.CreateFailed", "Failed to create alert");
        _mediator.Send(Arg.Any<MIC.Core.Application.Alerts.Commands.CreateAlert.CreateAlertCommand>(), Arg.Any<CancellationToken>())
            .Returns(errorResult);

        var vm = new AlertDetailsViewModel(_mediator);
        vm.InitializeForNew();
        vm.AlertName = "Test Alert";
        vm.Description = "Test Description";
        vm.Source = "Test Source";

        await vm.SaveCommand.Execute().ToTask();

        vm.ErrorMessage.Should().Contain("Failed to create alert");
    }

    [Fact]
    public async Task SaveCommand_ExistingAlert_SendsUpdateCommand()
    {
        var alertId = Guid.NewGuid();
        var dto = new AlertDto
        {
            Id = alertId,
            AlertName = "Existing",
            Description = "Desc",
            Source = "Source",
            Status = AlertStatus.Active
        };
        var updateResult = ErrorOrFactory.From(dto);
        _mediator.Send(Arg.Any<MIC.Core.Application.Alerts.Commands.UpdateAlert.UpdateAlertCommand>(), Arg.Any<CancellationToken>())
            .Returns(updateResult);

        var vm = new AlertDetailsViewModel(_mediator);
        vm.LoadFromDto(dto);
        // Make editable
        vm.IsEditMode = true;
        vm.Notes = "Updated notes";

        bool closedWithSave = false;
        vm.CloseRequested += (_, result) => closedWithSave = result;

        await vm.SaveCommand.Execute().ToTask();

        closedWithSave.Should().BeTrue();
    }

    [Fact]
    public async Task SaveCommand_Exception_SetsErrorMessage()
    {
        _mediator.Send(Arg.Any<MIC.Core.Application.Alerts.Commands.CreateAlert.CreateAlertCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Connection lost"));

        var vm = new AlertDetailsViewModel(_mediator);
        vm.InitializeForNew();
        vm.AlertName = "Test";
        vm.Description = "Test";
        vm.Source = "Test";

        await vm.SaveCommand.Execute().ToTask();

        vm.ErrorMessage.Should().Contain("Connection lost");
        vm.IsLoading.Should().BeFalse();
    }

    #endregion

    #region ResolveCommand Tests

    [Fact]
    public async Task ResolveCommand_WithoutResolutionNotes_SetsError()
    {
        var alertId = Guid.NewGuid();
        var vm = new AlertDetailsViewModel(_mediator);
        vm.LoadFromDto(new AlertDto { Id = alertId, Status = AlertStatus.Active });
        vm.ResolutionNotes = "";

        await vm.ResolveCommand.Execute().ToTask();

        vm.ErrorMessage.Should().Contain("Resolution notes are required");
    }

    [Fact]
    public async Task ResolveCommand_WithoutAlertId_DoesNothing()
    {
        var vm = new AlertDetailsViewModel(_mediator);
        vm.ResolutionNotes = "Fixed it";

        await vm.ResolveCommand.Execute().ToTask();

        await _mediator.DidNotReceive().Send(
            Arg.Any<MIC.Core.Application.Alerts.Commands.UpdateAlert.UpdateAlertCommand>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region AcknowledgeCommand Tests

    [Fact]
    public async Task AcknowledgeCommand_WithoutAlertId_DoesNothing()
    {
        var vm = new AlertDetailsViewModel(_mediator);

        await vm.AcknowledgeCommand.Execute().ToTask();

        await _mediator.DidNotReceive().Send(
            Arg.Any<MIC.Core.Application.Alerts.Commands.UpdateAlert.UpdateAlertCommand>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region IsLoading State Tests

    [Fact]
    public async Task InitializeForEditAsync_SetsIsLoading_DuringLoad()
    {
        var alertId = Guid.NewGuid();
        var tcs = new TaskCompletionSource<ErrorOr<AlertDto>>();

        _mediator.Send(Arg.Any<GetAlertByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        var vm = new AlertDetailsViewModel(_mediator);
        var task = vm.InitializeForEditAsync(alertId);

        // During load, IsLoading should be true
        vm.IsLoading.Should().BeTrue();

        // Complete the task
        tcs.SetResult(new AlertDto { AlertName = "Test" });
        await task;

        vm.IsLoading.Should().BeFalse();
    }

    #endregion
}
