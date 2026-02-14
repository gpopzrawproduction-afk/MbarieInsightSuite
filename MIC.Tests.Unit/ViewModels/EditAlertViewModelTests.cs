using System;
using System.Reactive.Concurrency;
using System.Reflection;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using MIC.Desktop.Avalonia.ViewModels;
using NSubstitute;
using ReactiveUI;
using Xunit;

namespace MIC.Tests.Unit.ViewModels;

/// <summary>
/// Tests for EditAlertViewModel covering alert editing functionality.
/// Target: 10 tests for edit alert dialog
/// </summary>
public class EditAlertViewModelTests : IDisposable
{
    private readonly IServiceProvider? _originalProvider;

    static EditAlertViewModelTests()
    {
        RxApp.MainThreadScheduler = CurrentThreadScheduler.Instance;
        RxApp.TaskpoolScheduler = CurrentThreadScheduler.Instance;
    }

    public EditAlertViewModelTests()
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
        // Arrange
        var alertId = Guid.NewGuid();

        // Act
        var viewModel = new EditAlertViewModel(alertId);

        // Assert
        viewModel.AlertName.Should().NotBeNull();
        viewModel.Description.Should().NotBeNull();
        viewModel.Source.Should().NotBeNull();
        viewModel.SelectedSeverity.Should().Be(AlertSeverity.Info);
        viewModel.SelectedStatus.Should().Be(AlertStatus.Active);
        viewModel.ErrorMessage.Should().NotBeNull(); // May contain error from loading with mock mediator
    }

    [Fact]
    public void Constructor_InitializesCommands()
    {
        // Arrange
        var alertId = Guid.NewGuid();

        // Act
        var viewModel = new EditAlertViewModel(alertId);

        // Assert
        viewModel.UpdateCommand.Should().NotBeNull();
        viewModel.CancelCommand.Should().NotBeNull();
    }

    #endregion

    #region Property Tests

    [Fact]
    public void AlertName_CanBeSet()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var viewModel = new EditAlertViewModel(alertId);

        // Act
        viewModel.AlertName = "Updated Alert Name";

        // Assert
        viewModel.AlertName.Should().Be("Updated Alert Name");
    }

    [Fact]
    public void Description_CanBeSet()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var viewModel = new EditAlertViewModel(alertId);

        // Act
        viewModel.Description = "Updated description";

        // Assert
        viewModel.Description.Should().Be("Updated description");
    }

    [Fact]
    public void Source_CanBeSet()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var viewModel = new EditAlertViewModel(alertId);

        // Act
        viewModel.Source = "Updated Source";

        // Assert
        viewModel.Source.Should().Be("Updated Source");
    }

    [Fact]
    public void SelectedSeverity_CanBeChanged()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var viewModel = new EditAlertViewModel(alertId);

        // Act
        viewModel.SelectedSeverity = AlertSeverity.Emergency;

        // Assert
        viewModel.SelectedSeverity.Should().Be(AlertSeverity.Emergency);
    }

    [Fact]
    public void SelectedStatus_CanBeChanged()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var viewModel = new EditAlertViewModel(alertId);

        // Act
        viewModel.SelectedStatus = AlertStatus.Resolved;

        // Assert
        viewModel.SelectedStatus.Should().Be(AlertStatus.Resolved);
    }

    [Fact]
    public void ErrorMessage_CanBeSet()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var viewModel = new EditAlertViewModel(alertId);

        // Act
        viewModel.ErrorMessage = "Update failed";

        // Assert
        viewModel.ErrorMessage.Should().Be("Update failed");
    }

    #endregion

    #region Command Tests

    [Fact]
    public void CancelCommand_CanExecute()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var viewModel = new EditAlertViewModel(alertId);
        var cancelInvoked = false;
        viewModel.OnCancel += () => cancelInvoked = true;

        // Act
        viewModel.CancelCommand.Execute().Subscribe();

        // Assert
        cancelInvoked.Should().BeTrue();
    }

    [Fact]
    public void UpdateCommand_IsDisabled_WhenFieldsAreEmpty()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var viewModel = new EditAlertViewModel(alertId);

        // Clear all fields
        viewModel.AlertName = "";
        viewModel.Description = "";
        viewModel.Source = "";

        // Act & Assert
        viewModel.UpdateCommand.CanExecute.Subscribe(canExecute =>
        {
            canExecute.Should().BeFalse();
        });
    }

    #endregion
}
