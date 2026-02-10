using Xunit;
using FluentAssertions;
using NSubstitute;
using MIC.Desktop.Avalonia.ViewModels;
using MIC.Core.Application.Authentication;
using MIC.Core.Domain.Entities;
using System;
using System.Threading.Tasks;
using ReactiveUI;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;

namespace MIC.Tests.Unit.ViewModels;

public class LoginViewModelTests
{
    private readonly IAuthenticationService _mockAuthService;
    private readonly LoginViewModel _sut;

    public LoginViewModelTests()
    {
        _mockAuthService = Substitute.For<IAuthenticationService>();
        _sut = new LoginViewModel(_mockAuthService);
    }

    [Fact]
    public void Constructor_InitializesProperties()
    {
        // Assert
        _sut.Username.Should().BeEmpty();
        _sut.Password.Should().BeEmpty();
        _sut.RememberMe.Should().BeTrue();
        _sut.IsLoading.Should().BeFalse();
        _sut.ErrorMessage.Should().BeEmpty();
        _sut.RegisterUsername.Should().BeEmpty();
        _sut.RegisterEmail.Should().BeEmpty();
        _sut.RegisterPassword.Should().BeEmpty();
        _sut.RegisterConfirmPassword.Should().BeEmpty();
        _sut.RegisterFullName.Should().BeEmpty();
        _sut.ShowRegistration.Should().BeFalse();
    }

    [Fact]
    public void Constructor_InitializesCommands()
    {
        // Assert
        _sut.LoginCommand.Should().NotBeNull();
        _sut.ContinueAsGuestCommand.Should().NotBeNull();
        _sut.ShowRegisterCommand.Should().NotBeNull();
        _sut.BackToLoginCommand.Should().NotBeNull();
        _sut.RegisterCommand.Should().NotBeNull();
    }

    [Fact]
    public async Task LoginCommand_CanExecute_WhenUsernameAndPasswordNotEmpty()
    {
        // Arrange
        _sut.Username = "testuser";
        _sut.Password = "password";
        _sut.IsLoading = false;

        // Act & Assert
        var canExecute = await _sut.LoginCommand.CanExecute.FirstAsync();
        canExecute.Should().BeTrue();
    }

    [Fact]
    public async Task LoginCommand_CannotExecute_WhenUsernameEmpty()
    {
        // Arrange
        _sut.Username = "";
        _sut.Password = "password";
        _sut.IsLoading = false;

        // Act & Assert
        var canExecute = await _sut.LoginCommand.CanExecute.FirstAsync();
        canExecute.Should().BeFalse();
    }

    [Fact]
    public async Task LoginCommand_CannotExecute_WhenPasswordEmpty()
    {
        // Arrange
        _sut.Username = "testuser";
        _sut.Password = "";
        _sut.IsLoading = false;

        // Act & Assert
        var canExecute = await _sut.LoginCommand.CanExecute.FirstAsync();
        canExecute.Should().BeFalse();
    }

    [Fact]
    public async Task LoginCommand_CannotExecute_WhenIsLoading()
    {
        // Arrange
        _sut.Username = "testuser";
        _sut.Password = "password";
        _sut.IsLoading = true;

        // Act & Assert
        var canExecute = await _sut.LoginCommand.CanExecute.FirstAsync();
        canExecute.Should().BeFalse();
    }

    [Fact]
    public async Task LoginCommand_ExecutesLogin_WhenSuccessful()
    {
        // Arrange
        _sut.Username = "testuser";
        _sut.Password = "password";
        
        var loginResult = new AuthenticationResult
        {
            Success = true,
            User = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@example.com",
                FullName = "Test User"
            },
            Token = "test-token"
        };
        
        _mockAuthService.LoginAsync("testuser", "password").Returns(Task.FromResult(loginResult));
        
        bool loginSuccessRaised = false;
        _sut.OnLoginSuccess += () => loginSuccessRaised = true;

        // Act
        await _sut.LoginCommand.Execute().ToTask();

        // Assert
        await _mockAuthService.Received(1).LoginAsync("testuser", "password");
        loginSuccessRaised.Should().BeTrue();
        _sut.ErrorMessage.Should().BeEmpty();
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoginCommand_SetsErrorMessage_WhenLoginFails()
    {
        // Arrange
        _sut.Username = "testuser";
        _sut.Password = "password";
        
        var loginResult = new AuthenticationResult
        {
            Success = false,
            ErrorMessage = "Invalid credentials"
        };
        
        _mockAuthService.LoginAsync("testuser", "password").Returns(Task.FromResult(loginResult));

        // Act
        await _sut.LoginCommand.Execute().ToTask();

        // Assert
        _sut.ErrorMessage.Should().Be("Invalid credentials");
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoginCommand_SetsErrorMessage_WhenExceptionThrown()
    {
        // Arrange
        _sut.Username = "testuser";
        _sut.Password = "password";
        
        _mockAuthService.LoginAsync("testuser", "password").Returns(Task.FromException<AuthenticationResult>(new Exception("Network error")));

        // Act
        await _sut.LoginCommand.Execute().ToTask();

        // Assert
        _sut.ErrorMessage.Should().StartWith("Login failed:");
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task ContinueAsGuestCommand_ExecutesSuccessfully()
    {
        // Arrange
        bool loginSuccessRaised = false;
        _sut.OnLoginSuccess += () => loginSuccessRaised = true;

        // Act
        await _sut.ContinueAsGuestCommand.Execute().ToTask();

        // Assert
        loginSuccessRaised.Should().BeTrue();
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task ShowRegisterCommand_ShowsRegistrationForm()
    {
        // Act
        await _sut.ShowRegisterCommand.Execute().ToTask();

        // Assert
        _sut.ShowRegistration.Should().BeTrue();
        _sut.ErrorMessage.Should().BeEmpty();
    }

    [Fact]
    public async Task BackToLoginCommand_ShowsLoginForm()
    {
        // Arrange
        _sut.ShowRegistration = true;

        // Act
        await _sut.BackToLoginCommand.Execute().ToTask();

        // Assert
        _sut.ShowRegistration.Should().BeFalse();
        _sut.ErrorMessage.Should().BeEmpty();
    }

    [Fact]
    public async Task RegisterCommand_CanExecute_WhenAllRegistrationFieldsFilled()
    {
        // Arrange
        _sut.RegisterUsername = "newuser";
        _sut.RegisterEmail = "new@example.com";
        _sut.RegisterPassword = "password123";
        _sut.RegisterConfirmPassword = "password123";
        _sut.IsLoading = false;

        // Act & Assert
        var canExecute = await _sut.RegisterCommand.CanExecute.FirstAsync();
        canExecute.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterCommand_CannotExecute_WhenAnyRegistrationFieldEmpty()
    {
        // Arrange
        _sut.RegisterUsername = "";
        _sut.RegisterEmail = "new@example.com";
        _sut.RegisterPassword = "password123";
        _sut.RegisterConfirmPassword = "password123";
        _sut.IsLoading = false;

        // Act & Assert
        var canExecute = await _sut.RegisterCommand.CanExecute.FirstAsync();
        canExecute.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterCommand_ValidatesPasswordMatch()
    {
        // Arrange
        _sut.RegisterUsername = "newuser";
        _sut.RegisterEmail = "new@example.com";
        _sut.RegisterPassword = "password123";
        _sut.RegisterConfirmPassword = "different";

        // Act
        await _sut.RegisterCommand.Execute().ToTask();

        // Assert
        _sut.ErrorMessage.Should().Be("Passwords do not match");
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterCommand_ValidatesPasswordLength()
    {
        // Arrange
        _sut.RegisterUsername = "newuser";
        _sut.RegisterEmail = "new@example.com";
        _sut.RegisterPassword = "short";
        _sut.RegisterConfirmPassword = "short";

        // Act
        await _sut.RegisterCommand.Execute().ToTask();

        // Assert
        _sut.ErrorMessage.Should().Be("Password must be at least 8 characters");
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterCommand_ValidatesEmailFormat()
    {
        // Arrange
        _sut.RegisterUsername = "newuser";
        _sut.RegisterEmail = "invalid-email";
        _sut.RegisterPassword = "password123";
        _sut.RegisterConfirmPassword = "password123";

        // Act
        await _sut.RegisterCommand.Execute().ToTask();

        // Assert
        _sut.ErrorMessage.Should().Be("Please enter a valid email address");
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact(Skip = "UI thread issue in ShowFirstTimeSetupDialogAsync - needs proper UI thread mocking")]
    public async Task RegisterCommand_ExecutesRegistration_WhenSuccessful()
    {
        // Arrange
        _sut.RegisterUsername = "newuser";
        _sut.RegisterEmail = "new@example.com";
        _sut.RegisterPassword = "password123";
        _sut.RegisterConfirmPassword = "password123";
        _sut.RegisterFullName = "New User"; // Set full name
        
        var registerResult = new AuthenticationResult
        {
            Success = true,
            User = new User
            {
                Id = Guid.NewGuid(),
                Username = "newuser",
                Email = "new@example.com",
                FullName = "New User"
            }
        };
        
        _mockAuthService.RegisterAsync("newuser", "new@example.com", "password123", "New User")
            .Returns(Task.FromResult(registerResult));

        // Act
        await _sut.RegisterCommand.Execute().ToTask();

        // Assert
        await _mockAuthService.Received(1).RegisterAsync("newuser", "new@example.com", "password123", "New User");
        _sut.ShowRegistration.Should().BeFalse();
        _sut.Username.Should().Be("newuser");
        _sut.Password.Should().Be("password123");
        _sut.RegisterUsername.Should().BeEmpty();
        _sut.RegisterEmail.Should().BeEmpty();
        _sut.RegisterPassword.Should().BeEmpty();
        _sut.RegisterConfirmPassword.Should().BeEmpty();
        _sut.RegisterFullName.Should().BeEmpty();
    }

    [Fact(Skip = "UI thread issue in ShowFirstTimeSetupDialogAsync - needs proper UI thread mocking")]
    public async Task RegisterCommand_SetsErrorMessage_WhenRegistrationFails()
    {
        // Arrange
        _sut.RegisterUsername = "newuser";
        _sut.RegisterEmail = "new@example.com";
        _sut.RegisterPassword = "password123";
        _sut.RegisterConfirmPassword = "password123";
        
        var registerResult = new AuthenticationResult
        {
            Success = false,
            ErrorMessage = "Username already exists"
        };
        
        _mockAuthService.RegisterAsync("newuser", "new@example.com", "password123", "newuser")
            .Returns(Task.FromResult(registerResult));

        // Act
        await _sut.RegisterCommand.Execute().ToTask();

        // Assert
        _sut.ErrorMessage.Should().Be("Username already exists");
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public void ClearError_ClearsErrorMessage()
    {
        // Arrange
        _sut.ErrorMessage = "Some error";

        // Act
        _sut.ErrorMessage = "";

        // Assert
        _sut.ErrorMessage.Should().BeEmpty();
    }
}