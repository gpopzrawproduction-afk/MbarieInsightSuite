using System;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using FluentAssertions;
using MIC.Core.Application.Common.Interfaces;
using MIC.Desktop.Avalonia.ViewModels;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ReactiveUI;
using Xunit;

using RxUnit = System.Reactive.Unit;

namespace MIC.Tests.Unit.ViewModels;

/// <summary>
/// Tests for FirstRunSetupViewModel covering initial setup functionality.
/// Target: 12 tests for first-run setup dialog
/// </summary>
public class FirstRunSetupViewModelTests
{
    private readonly IFirstRunSetupService _setupService;
    private bool _setupCompleted;

    static FirstRunSetupViewModelTests()
    {
        RxApp.MainThreadScheduler = CurrentThreadScheduler.Instance;
        RxApp.TaskpoolScheduler = CurrentThreadScheduler.Instance;
    }

    public FirstRunSetupViewModelTests()
    {
        _setupService = Substitute.For<IFirstRunSetupService>();
        _setupCompleted = false;
    }

    private Task OnSetupComplete()
    {
        _setupCompleted = true;
        return Task.CompletedTask;
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_InitializesProperties()
    {
        // Act
        var viewModel = new FirstRunSetupViewModel(_setupService, OnSetupComplete);

        // Assert
        viewModel.Email.Should().BeEmpty();
        viewModel.Password.Should().BeEmpty();
        viewModel.ConfirmPassword.Should().BeEmpty();
        viewModel.ErrorMessage.Should().BeEmpty();
        viewModel.IsBusy.Should().BeFalse();
    }

    [Fact]
    public void Constructor_InitializesCommand()
    {
        // Act
        var viewModel = new FirstRunSetupViewModel(_setupService, OnSetupComplete);

        // Assert
        viewModel.FinishSetupCommand.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_AcceptsAllParameters()
    {
        // Act
        var viewModel = new FirstRunSetupViewModel(_setupService, OnSetupComplete);

        // Assert
        viewModel.Should().NotBeNull();
        viewModel.Email.Should().BeEmpty();
        viewModel.Password.Should().BeEmpty();
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Email_CanBeSet()
    {
        // Arrange
        var viewModel = new FirstRunSetupViewModel(_setupService, OnSetupComplete);

        // Act
        viewModel.Email = "admin@example.com";

        // Assert
        viewModel.Email.Should().Be("admin@example.com");
    }

    [Fact]
    public void Password_CanBeSet()
    {
        // Arrange
        var viewModel = new FirstRunSetupViewModel(_setupService, OnSetupComplete);

        // Act
        viewModel.Password = "SecurePassword123!";

        // Assert
        viewModel.Password.Should().Be("SecurePassword123!");
    }

    [Fact]
    public void ConfirmPassword_CanBeSet()
    {
        // Arrange
        var viewModel = new FirstRunSetupViewModel(_setupService, OnSetupComplete);

        // Act
        viewModel.ConfirmPassword = "SecurePassword123!";

        // Assert
        viewModel.ConfirmPassword.Should().Be("SecurePassword123!");
    }

    [Fact]
    public void ErrorMessage_CanBeSet()
    {
        // Arrange
        var viewModel = new FirstRunSetupViewModel(_setupService, OnSetupComplete);

        // Act
        viewModel.ErrorMessage = "Setup failed";

        // Assert
        viewModel.ErrorMessage.Should().Be("Setup failed");
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void FinishSetupCommand_IsDisabled_WhenEmailIsEmpty()
    {
        // Arrange
        var viewModel = new FirstRunSetupViewModel(_setupService, OnSetupComplete);
        viewModel.Password = "ValidPassword123!";
        viewModel.ConfirmPassword = "ValidPassword123!";

        // Act & Assert - cast to ReactiveCommand to access CanExecute observable
        var reactiveCmd = (ReactiveCommand<RxUnit, RxUnit>)viewModel.FinishSetupCommand;
        bool? canExec = null;
        reactiveCmd.CanExecute.Subscribe(v => canExec = v);
        canExec.Should().BeFalse();
    }

    [Fact]
    public void FinishSetupCommand_IsDisabled_WhenPasswordTooShort()
    {
        // Arrange
        var viewModel = new FirstRunSetupViewModel(_setupService, OnSetupComplete);
        viewModel.Email = "admin@example.com";
        viewModel.Password = "Short1!";
        viewModel.ConfirmPassword = "Short1!";

        // Act & Assert
        var reactiveCmd = (ReactiveCommand<RxUnit, RxUnit>)viewModel.FinishSetupCommand;
        bool? canExec = null;
        reactiveCmd.CanExecute.Subscribe(v => canExec = v);
        canExec.Should().BeFalse();
    }

    [Fact]
    public void FinishSetupCommand_IsDisabled_WhenPasswordsDoNotMatch()
    {
        // Arrange
        var viewModel = new FirstRunSetupViewModel(_setupService, OnSetupComplete);
        viewModel.Email = "admin@example.com";
        viewModel.Password = "ValidPassword123!";
        viewModel.ConfirmPassword = "DifferentPassword123!";

        // Act & Assert
        var reactiveCmd = (ReactiveCommand<RxUnit, RxUnit>)viewModel.FinishSetupCommand;
        bool? canExec = null;
        reactiveCmd.CanExecute.Subscribe(v => canExec = v);
        canExec.Should().BeFalse();
    }

    [Fact]
    public void FinishSetupCommand_IsDisabled_WhenEmailInvalid()
    {
        // Arrange
        var viewModel = new FirstRunSetupViewModel(_setupService, OnSetupComplete);
        viewModel.Email = "invalid-email";
        viewModel.Password = "ValidPassword123!";
        viewModel.ConfirmPassword = "ValidPassword123!";

        // Act & Assert
        var reactiveCmd = (ReactiveCommand<RxUnit, RxUnit>)viewModel.FinishSetupCommand;
        bool? canExec = null;
        reactiveCmd.CanExecute.Subscribe(v => canExec = v);
        canExec.Should().BeFalse();
    }

    [Fact]
    public async Task FinishSetupAsync_SetsErrorMessage_WhenServiceThrows()
    {
        // Arrange
        _setupService.CompleteFirstRunSetupAsync(Arg.Any<string>(), Arg.Any<string>())
            .ThrowsAsync(new Exception("Database connection failed"));

        var viewModel = new FirstRunSetupViewModel(_setupService, OnSetupComplete);
        viewModel.Email = "admin@example.com";
        viewModel.Password = "ValidPassword123!Aa";
        viewModel.ConfirmPassword = "ValidPassword123!Aa";

        // Act - cast to ReactiveCommand to call Execute properly
        var reactiveCmd = (ReactiveCommand<RxUnit, RxUnit>)viewModel.FinishSetupCommand;
        try
        {
            reactiveCmd.Execute().Subscribe(_ => { }, _ => { });
            await Task.Delay(200);
        }
        catch { /* ReactiveCommand may rethrow */ }

        // Assert
        viewModel.ErrorMessage.Should().NotBeEmpty();
        viewModel.IsBusy.Should().BeFalse();
    }

    #endregion

    #region FinishSetupAsync Internal Validation (via reflection)

    [Fact]
    public async Task FinishSetupAsync_EmptyEmail_SetsErrorMessage()
    {
        var vm = new FirstRunSetupViewModel(_setupService, OnSetupComplete);
        vm.Email = "";
        vm.Password = "ValidP@ssw0rd1";
        vm.ConfirmPassword = "ValidP@ssw0rd1";

        await InvokeFinishSetupAsync(vm);

        vm.ErrorMessage.Should().Contain("Email is required");
    }

    [Fact]
    public async Task FinishSetupAsync_EmptyPassword_SetsErrorMessage()
    {
        var vm = new FirstRunSetupViewModel(_setupService, OnSetupComplete);
        vm.Email = "user@example.com";
        vm.Password = "";
        vm.ConfirmPassword = "";

        await InvokeFinishSetupAsync(vm);

        vm.ErrorMessage.Should().Contain("Password is required");
    }

    [Fact]
    public async Task FinishSetupAsync_ShortPassword_SetsErrorMessage()
    {
        var vm = new FirstRunSetupViewModel(_setupService, OnSetupComplete);
        vm.Email = "user@example.com";
        vm.Password = "Short1!";
        vm.ConfirmPassword = "Short1!";

        await InvokeFinishSetupAsync(vm);

        vm.ErrorMessage.Should().Contain("at least 12 characters");
    }

    [Fact]
    public async Task FinishSetupAsync_PasswordMismatch_SetsErrorMessage()
    {
        var vm = new FirstRunSetupViewModel(_setupService, OnSetupComplete);
        vm.Email = "user@example.com";
        vm.Password = "ValidP@ssw0rd1";
        vm.ConfirmPassword = "DifferentP@ss1";

        await InvokeFinishSetupAsync(vm);

        vm.ErrorMessage.Should().Contain("do not match");
    }

    [Fact]
    public async Task FinishSetupAsync_InvalidEmail_SetsErrorMessage()
    {
        var vm = new FirstRunSetupViewModel(_setupService, OnSetupComplete);
        vm.Email = "notanemail";
        vm.Password = "ValidP@ssw0rd1";
        vm.ConfirmPassword = "ValidP@ssw0rd1";

        await InvokeFinishSetupAsync(vm);

        vm.ErrorMessage.Should().Contain("valid email");
    }

    [Fact]
    public async Task FinishSetupAsync_WeakPassword_NoUppercase_SetsErrorMessage()
    {
        var vm = new FirstRunSetupViewModel(_setupService, OnSetupComplete);
        vm.Email = "user@example.com";
        vm.Password = "nouppercase1!aa";
        vm.ConfirmPassword = "nouppercase1!aa";

        await InvokeFinishSetupAsync(vm);

        vm.ErrorMessage.Should().Contain("uppercase");
    }

    [Fact]
    public async Task FinishSetupAsync_WeakPassword_NoLowercase_SetsErrorMessage()
    {
        var vm = new FirstRunSetupViewModel(_setupService, OnSetupComplete);
        vm.Email = "user@example.com";
        vm.Password = "NOLOWERCASE1!AA";
        vm.ConfirmPassword = "NOLOWERCASE1!AA";

        await InvokeFinishSetupAsync(vm);

        vm.ErrorMessage.Should().Contain("lowercase");
    }

    [Fact]
    public async Task FinishSetupAsync_WeakPassword_NoDigit_SetsErrorMessage()
    {
        var vm = new FirstRunSetupViewModel(_setupService, OnSetupComplete);
        vm.Email = "user@example.com";
        vm.Password = "NoDigitsHere!!a";
        vm.ConfirmPassword = "NoDigitsHere!!a";

        await InvokeFinishSetupAsync(vm);

        vm.ErrorMessage.Should().Contain("number");
    }

    [Fact]
    public async Task FinishSetupAsync_WeakPassword_NoSpecialChar_SetsErrorMessage()
    {
        var vm = new FirstRunSetupViewModel(_setupService, OnSetupComplete);
        vm.Email = "user@example.com";
        vm.Password = "NoSpecialChar1a";
        vm.ConfirmPassword = "NoSpecialChar1a";

        await InvokeFinishSetupAsync(vm);

        vm.ErrorMessage.Should().Contain("special character");
    }

    [Fact]
    public async Task FinishSetupAsync_ValidInput_CallsServiceAndCompletesSetup()
    {
        _setupService.CompleteFirstRunSetupAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);

        var vm = new FirstRunSetupViewModel(_setupService, OnSetupComplete);
        vm.Email = "admin@example.com";
        vm.Password = "ValidP@ssw0rd1";
        vm.ConfirmPassword = "ValidP@ssw0rd1";

        await InvokeFinishSetupAsync(vm);

        await _setupService.Received(1).CompleteFirstRunSetupAsync("admin@example.com", "ValidP@ssw0rd1");
        _setupCompleted.Should().BeTrue();
        vm.ErrorMessage.Should().BeEmpty();
    }

    [Fact]
    public async Task FinishSetupAsync_ServiceThrows_SetsErrorAndResetsBusy()
    {
        _setupService.CompleteFirstRunSetupAsync(Arg.Any<string>(), Arg.Any<string>())
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var vm = new FirstRunSetupViewModel(_setupService, OnSetupComplete);
        vm.Email = "admin@example.com";
        vm.Password = "ValidP@ssw0rd1";
        vm.ConfirmPassword = "ValidP@ssw0rd1";

        await InvokeFinishSetupAsync(vm);

        vm.ErrorMessage.Should().Contain("DB error");
        vm.IsBusy.Should().BeFalse();
        _setupCompleted.Should().BeFalse();
    }

    [Fact]
    public void UpdateCanExecute_DoesNotThrow()
    {
        var vm = new FirstRunSetupViewModel(_setupService, OnSetupComplete);
        var act = () => vm.UpdateCanExecute();
        act.Should().NotThrow();
    }

    #endregion

    #region Helpers

    private static async Task InvokeFinishSetupAsync(FirstRunSetupViewModel vm)
    {
        var method = typeof(FirstRunSetupViewModel).GetMethod(
            "FinishSetupAsync",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var task = (Task)method!.Invoke(vm, null)!;
        await task;
    }

    #endregion
}
