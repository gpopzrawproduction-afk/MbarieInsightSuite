using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MIC.Core.Application.Alerts.Commands.CreateAlert;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using MIC.Desktop.Avalonia.Services;
using MIC.Desktop.Avalonia.ViewModels;
using NSubstitute;
using ReactiveUI;
using Xunit;

namespace MIC.Tests.Unit.ViewModels;

/// <summary>
/// Tests for CreateAlertViewModel covering alert creation functionality.
/// Target: 10 tests for create alert dialog
/// </summary>
public class CreateAlertViewModelTests : IDisposable
{
    private readonly IServiceProvider? _originalProvider;

    static CreateAlertViewModelTests()
    {
        RxApp.MainThreadScheduler = CurrentThreadScheduler.Instance;
        RxApp.TaskpoolScheduler = CurrentThreadScheduler.Instance;
    }

    public CreateAlertViewModelTests()
    {
        _originalProvider = GetProgramServiceProvider();
        SetProgramServiceProvider(BuildTestServiceProvider());
    }

    public void Dispose()
    {
        SetProgramServiceProvider(_originalProvider);
    }

    private static IServiceProvider BuildTestServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IErrorHandlingService>());
        services.AddSingleton(Substitute.For<IMediator>());
        return services.BuildServiceProvider();
    }

    private static void SetProgramServiceProvider(IServiceProvider? provider)
    {
        var programType = Type.GetType("MIC.Desktop.Avalonia.Program, MIC.Desktop.Avalonia");
        if (programType == null) return;
        var property = programType.GetProperty("ServiceProvider", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        property?.SetValue(null, provider);
    }

    private static IServiceProvider? GetProgramServiceProvider()
    {
        var programType = Type.GetType("MIC.Desktop.Avalonia.Program, MIC.Desktop.Avalonia");
        var property = programType?.GetProperty("ServiceProvider", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        return property?.GetValue(null) as IServiceProvider;
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_InitializesProperties()
    {
        // Arrange & Act
        var viewModel = new CreateAlertViewModel();

        // Assert
        viewModel.AlertName.Should().BeEmpty();
        viewModel.Description.Should().BeEmpty();
        viewModel.Source.Should().BeEmpty();
        viewModel.SelectedSeverity.Should().Be(AlertSeverity.Info);
        viewModel.IsLoading.Should().BeFalse();
        viewModel.ErrorMessage.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_InitializesSeveritiesCollection()
    {
        // Arrange & Act
        var viewModel = new CreateAlertViewModel();

        // Assert
        viewModel.Severities.Should().HaveCount(4);
        viewModel.Severities.Should().Contain(AlertSeverity.Info);
        viewModel.Severities.Should().Contain(AlertSeverity.Warning);
        viewModel.Severities.Should().Contain(AlertSeverity.Critical);
        viewModel.Severities.Should().Contain(AlertSeverity.Emergency);
    }

    [Fact]
    public void Constructor_InitializesCommands()
    {
        // Arrange & Act
        var viewModel = new CreateAlertViewModel();

        // Assert
        viewModel.CreateCommand.Should().NotBeNull();
        viewModel.CancelCommand.Should().NotBeNull();
    }

    #endregion

    #region Property Tests

    [Fact]
    public void AlertName_CanBeSet()
    {
        // Arrange
        var viewModel = new CreateAlertViewModel();

        // Act
        viewModel.AlertName = "Critical System Alert";

        // Assert
        viewModel.AlertName.Should().Be("Critical System Alert");
    }

    [Fact]
    public void Description_CanBeSet()
    {
        // Arrange
        var viewModel = new CreateAlertViewModel();

        // Act
        viewModel.Description = "System resources critically low";

        // Assert
        viewModel.Description.Should().Be("System resources critically low");
    }

    [Fact]
    public void Source_CanBeSet()
    {
        // Arrange
        var viewModel = new CreateAlertViewModel();

        // Act
        viewModel.Source = "Monitoring System";

        // Assert
        viewModel.Source.Should().Be("Monitoring System");
    }

    [Fact]
    public void SelectedSeverity_CanBeChanged()
    {
        // Arrange
        var viewModel = new CreateAlertViewModel();

        // Act
        viewModel.SelectedSeverity = AlertSeverity.Critical;

        // Assert
        viewModel.SelectedSeverity.Should().Be(AlertSeverity.Critical);
    }

    [Fact]
    public void ErrorMessage_CanBeSet()
    {
        // Arrange
        var viewModel = new CreateAlertViewModel();

        // Act
        viewModel.ErrorMessage = "Failed to create alert";

        // Assert
        viewModel.ErrorMessage.Should().Be("Failed to create alert");
    }

    #endregion

    #region Command Validation Tests

    [Fact]
    public void CreateCommand_IsDisabled_WhenFieldsAreEmpty()
    {
        // Arrange
        var viewModel = new CreateAlertViewModel();

        // Act & Assert
        viewModel.CreateCommand.CanExecute.Subscribe(canExecute =>
        {
            canExecute.Should().BeFalse();
        });
    }

    [Fact]
    public void CancelCommand_CanExecute()
    {
        // Arrange
        var viewModel = new CreateAlertViewModel();
        var cancelInvoked = false;
        viewModel.OnCancel += () => cancelInvoked = true;

        // Act
        viewModel.CancelCommand.Execute().Subscribe();

        // Assert
        cancelInvoked.Should().BeTrue();
    }

    #endregion

    #region CreateCommand Execution Tests

    [Fact]
    public async Task CreateCommand_Success_InvokesOnCreated()
    {
        var mediator = GetProgramServiceProvider()!.GetRequiredService<IMediator>();
        ErrorOr.ErrorOr<Guid> successResult = Guid.NewGuid();
        mediator.Send(Arg.Any<CreateAlertCommand>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(successResult);

        var viewModel = new CreateAlertViewModel();
        var created = false;
        viewModel.OnCreated += () => created = true;
        viewModel.AlertName = "Test Alert";
        viewModel.Description = "Test Description";
        viewModel.Source = "Test Source";

        await viewModel.CreateCommand.Execute().ToTask();

        created.Should().BeTrue();
        viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task CreateCommand_Error_SetsErrorMessage()
    {
        var mediator = GetProgramServiceProvider()!.GetRequiredService<IMediator>();
        ErrorOr.ErrorOr<Guid> errorResult = ErrorOr.Error.Failure("Alert.Failed", "Validation failed");
        mediator.Send(Arg.Any<CreateAlertCommand>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(errorResult);

        var viewModel = new CreateAlertViewModel();
        viewModel.AlertName = "Test Alert";
        viewModel.Description = "Test Description";
        viewModel.Source = "Test Source";

        await viewModel.CreateCommand.Execute().ToTask();

        viewModel.ErrorMessage.Should().Contain("Validation failed");
        viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task CreateCommand_Exception_SetsErrorMessage()
    {
        var mediator = GetProgramServiceProvider()!.GetRequiredService<IMediator>();
        mediator.Send(Arg.Any<CreateAlertCommand>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns<ErrorOr.ErrorOr<Guid>>(_ => throw new InvalidOperationException("DB error"));

        var viewModel = new CreateAlertViewModel();
        viewModel.AlertName = "Test Alert";
        viewModel.Description = "Test Description";
        viewModel.Source = "Test Source";

        await viewModel.CreateCommand.Execute().ToTask();

        viewModel.ErrorMessage.Should().Contain("DB error");
        viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public void CreateCommand_IsEnabled_WhenAllFieldsFilled()
    {
        var viewModel = new CreateAlertViewModel();
        viewModel.AlertName = "Alert";
        viewModel.Description = "Desc";
        viewModel.Source = "Source";

        bool? canExec = null;
        viewModel.CreateCommand.CanExecute.Subscribe(v => canExec = v);
        canExec.Should().BeTrue();
    }

    [Fact]
    public void IsLoading_CanBeSet()
    {
        var viewModel = new CreateAlertViewModel();
        viewModel.IsLoading = true;
        viewModel.IsLoading.Should().BeTrue();
    }

    #endregion
}
