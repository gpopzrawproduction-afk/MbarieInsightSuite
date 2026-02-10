using System;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Authentication;
using MIC.Core.Application.Authentication.Commands.LoginCommand;
using MIC.Core.Application.Authentication.Commands.RegisterUserCommand;
using MIC.Core.Application.Authentication.Common;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using Moq;
using Xunit;

namespace MIC.Tests.Unit.Application.Authentication;

/// <summary>
/// Comprehensive tests for Authentication CQRS handlers.
/// Tests login and registration command handlers for security and validation.
/// Target: 18 tests for authentication handler coverage
/// </summary>
public class AuthenticationHandlersTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<IJwtTokenService> _mockJwtTokenService;
    private readonly Mock<IAuthenticationService> _mockAuthService;
    private readonly Mock<ILogger<LoginCommandHandler>> _mockLoginLogger;
    private readonly Mock<ILogger<RegisterUserCommandHandler>> _mockRegisterLogger;

    public AuthenticationHandlersTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockJwtTokenService = new Mock<IJwtTokenService>();
        _mockAuthService = new Mock<IAuthenticationService>();
        _mockLoginLogger = new Mock<ILogger<LoginCommandHandler>>();
        _mockRegisterLogger = new Mock<ILogger<RegisterUserCommandHandler>>();
    }

    #region LoginCommandHandler Tests (9 tests)

    [Fact]
    public async Task LoginCommand_WithValidCredentials_ReturnsSuccessWithToken()
    {
        // Arrange
        var command = new LoginCommand("testuser", "password123");
        var user = CreateTestUser("testuser", "test@example.com");
        var expectedToken = "jwt_token_here";

        _mockUserRepository.Setup(x => x.GetByUsernameAsync("testuser")).ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.VerifyPassword("password123", user.PasswordHash, user.Salt)).Returns(true);
        _mockUserRepository.Setup(x => x.GetTrackedByIdAsync(user.Id)).ReturnsAsync(user);
        _mockUserRepository.Setup(x => x.UpdateAsync(user)).Returns(Task.CompletedTask);
        _mockJwtTokenService.Setup(x => x.GenerateToken(user)).Returns(expectedToken);

        var handler = new LoginCommandHandler(_mockUserRepository.Object, _mockPasswordHasher.Object,
            _mockJwtTokenService.Object, _mockLoginLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Success.Should().BeTrue();
        result.Value.Token.Should().Be(expectedToken);
        result.Value.User.Username.Should().Be("testuser");
        _mockJwtTokenService.Verify(x => x.GenerateToken(user), Times.Once);
    }

    [Fact]
    public async Task LoginCommand_WithNonExistentUser_ReturnsValidationError()
    {
        // Arrange
        var command = new LoginCommand("unknownuser", "password123");
        _mockUserRepository.Setup(x => x.GetByUsernameAsync("unknownuser")).ReturnsAsync((User?)null);

        var handler = new LoginCommandHandler(_mockUserRepository.Object, _mockPasswordHasher.Object,
            _mockJwtTokenService.Object, _mockLoginLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Code.Should().Be("Login.InvalidCredentials");
        result.FirstError.Description.Should().Contain("Invalid username or password");
    }

    [Fact]
    public async Task LoginCommand_WithIncorrectPassword_ReturnsValidationError()
    {
        // Arrange
        var command = new LoginCommand("testuser", "wrongpassword");
        var user = CreateTestUser("testuser", "test@example.com");

        _mockUserRepository.Setup(x => x.GetByUsernameAsync("testuser")).ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.VerifyPassword("wrongpassword", user.PasswordHash, user.Salt)).Returns(false);

        var handler = new LoginCommandHandler(_mockUserRepository.Object, _mockPasswordHasher.Object,
            _mockJwtTokenService.Object, _mockLoginLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Login.InvalidCredentials");
        _mockJwtTokenService.Verify(x => x.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginCommand_WithInactiveUser_ReturnsValidationError()
    {
        // Arrange
        var command = new LoginCommand("inactiveuser", "password123");
        var user = CreateTestUser("inactiveuser", "inactive@example.com");
        user.IsActive = false;

        _mockUserRepository.Setup(x => x.GetByUsernameAsync("inactiveuser")).ReturnsAsync(user);

        var handler = new LoginCommandHandler(_mockUserRepository.Object, _mockPasswordHasher.Object,
            _mockJwtTokenService.Object, _mockLoginLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Login.InvalidCredentials");
        _mockPasswordHasher.Verify(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginCommand_WithNullUsername_ReturnsValidationError()
    {
        // Arrange
        var command = new LoginCommand(null!, "password123");
        var handler = new LoginCommandHandler(_mockUserRepository.Object, _mockPasswordHasher.Object,
            _mockJwtTokenService.Object, _mockLoginLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Code.Should().Be("Login.Validation");
        result.FirstError.Description.Should().Contain("Username and password are required");
    }

    [Fact]
    public async Task LoginCommand_WithEmptyPassword_ReturnsValidationError()
    {
        // Arrange
        var command = new LoginCommand("testuser", "");
        var handler = new LoginCommandHandler(_mockUserRepository.Object, _mockPasswordHasher.Object,
            _mockJwtTokenService.Object, _mockLoginLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Login.Validation");
    }

    [Fact]
    public async Task LoginCommand_OnSuccessfulLogin_UpdatesLastLoginTimestamp()
    {
        // Arrange
        var command = new LoginCommand("testuser", "password123");
        var user = CreateTestUser("testuser", "test@example.com");
        var trackedUser = CreateTestUser("testuser", "test@example.com");

        _mockUserRepository.Setup(x => x.GetByUsernameAsync("testuser")).ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.VerifyPassword("password123", user.PasswordHash, user.Salt)).Returns(true);
        _mockUserRepository.Setup(x => x.GetTrackedByIdAsync(user.Id)).ReturnsAsync(trackedUser);
        _mockUserRepository.Setup(x => x.UpdateAsync(trackedUser)).Returns(Task.CompletedTask);
        _mockJwtTokenService.Setup(x => x.GenerateToken(user)).Returns("token");

        var handler = new LoginCommandHandler(_mockUserRepository.Object, _mockPasswordHasher.Object,
            _mockJwtTokenService.Object, _mockLoginLogger.Object);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _mockUserRepository.Verify(x => x.GetTrackedByIdAsync(user.Id), Times.Once);
        _mockUserRepository.Verify(x => x.UpdateAsync(trackedUser), Times.Once);
        trackedUser.LastLoginAt.Should().NotBeNull();
    }

    [Fact]
    public async Task LoginCommand_WhenRepositoryThrows_ReturnsFailureError()
    {
        // Arrange
        var command = new LoginCommand("testuser", "password123");
        _mockUserRepository.Setup(x => x.GetByUsernameAsync("testuser")).ThrowsAsync(new Exception("Database error"));

        var handler = new LoginCommandHandler(_mockUserRepository.Object, _mockPasswordHasher.Object,
            _mockJwtTokenService.Object, _mockLoginLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Failure);
        result.FirstError.Code.Should().Be("Login.Failure");
        result.FirstError.Description.Should().Contain("Database error");
    }

    [Fact]
    public async Task LoginCommand_GeneratesLoginResultWithUserDto()
    {
        // Arrange
        var command = new LoginCommand("testuser", "password123");
        var user = CreateTestUser("testuser", "test@example.com");
        user.FullName = "Test User";
        user.Department = "Engineering";

        _mockUserRepository.Setup(x => x.GetByUsernameAsync("testuser")).ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.VerifyPassword("password123", user.PasswordHash, user.Salt)).Returns(true);
        _mockUserRepository.Setup(x => x.GetTrackedByIdAsync(user.Id)).ReturnsAsync(user);
        _mockUserRepository.Setup(x => x.UpdateAsync(user)).Returns(Task.CompletedTask);
        _mockJwtTokenService.Setup(x => x.GenerateToken(user)).Returns("token");

        var handler = new LoginCommandHandler(_mockUserRepository.Object, _mockPasswordHasher.Object,
            _mockJwtTokenService.Object, _mockLoginLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        var userDto = result.Value.User;
        userDto.Username.Should().Be("testuser");
        userDto.Email.Should().Be("test@example.com");
        userDto.FullName.Should().Be("Test User");
        userDto.Department.Should().Be("Engineering");
        userDto.IsActive.Should().BeTrue();
    }

    #endregion

    #region RegisterUserCommandHandler Tests (9 tests)

    [Fact]
    public async Task RegisterCommand_WithValidData_ReturnsSuccessResult()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Username = "newuser",
            Email = "new@example.com",
            Password = "password123",
            ConfirmPassword = "password123",
            FullName = "New User"
        };

        var authResult = new AuthenticationResult
        {
            Success = true,
            Token = "jwt_token",
            User = CreateTestUser("newuser", "new@example.com")
        };

        _mockAuthService.Setup(x => x.RegisterAsync("newuser", "new@example.com", "password123", "New User"))
            .ReturnsAsync(authResult);

        var handler = new RegisterUserCommandHandler(_mockAuthService.Object, _mockRegisterLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Success.Should().BeTrue();
        result.Value.Token.Should().Be("jwt_token");
        _mockAuthService.Verify(x => x.RegisterAsync("newuser", "new@example.com", "password123", "New User"), Times.Once);
    }

    [Fact]
    public async Task RegisterCommand_WithMissingUsername_ReturnsValidationError()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Username = "",
            Email = "test@example.com",
            Password = "password123",
            ConfirmPassword = "password123"
        };

        var handler = new RegisterUserCommandHandler(_mockAuthService.Object, _mockRegisterLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Registration.Validation.Username");
        result.FirstError.Description.Should().Contain("Username is required");
    }

    [Fact]
    public async Task RegisterCommand_WithMissingEmail_ReturnsValidationError()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Username = "testuser",
            Email = "",
            Password = "password123",
            ConfirmPassword = "password123"
        };

        var handler = new RegisterUserCommandHandler(_mockAuthService.Object, _mockRegisterLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Registration.Validation.Email");
        result.FirstError.Description.Should().Contain("Email is required");
    }

    [Fact]
    public async Task RegisterCommand_WithMissingPassword_ReturnsValidationError()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "",
            ConfirmPassword = "password123"
        };

        var handler = new RegisterUserCommandHandler(_mockAuthService.Object, _mockRegisterLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Registration.Validation.Password");
    }

    [Fact]
    public async Task RegisterCommand_WithPasswordMismatch_ReturnsValidationError()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "password123",
            ConfirmPassword = "differentpassword"
        };

        var handler = new RegisterUserCommandHandler(_mockAuthService.Object, _mockRegisterLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Registration.Validation.PasswordMismatch");
        result.FirstError.Description.Should().Contain("Passwords do not match");
    }

    [Fact]
    public async Task RegisterCommand_WithShortPassword_ReturnsValidationError()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "short",
            ConfirmPassword = "short"
        };

        var handler = new RegisterUserCommandHandler(_mockAuthService.Object, _mockRegisterLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Registration.Validation.PasswordTooShort");
        result.FirstError.Description.Should().Contain("Password must be at least 8 characters");
    }

    [Fact]
    public async Task RegisterCommand_WithInvalidEmail_ReturnsValidationError()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Username = "testuser",
            Email = "invalid-email",
            Password = "password123",
            ConfirmPassword = "password123"
        };

        var handler = new RegisterUserCommandHandler(_mockAuthService.Object, _mockRegisterLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Registration.Validation.InvalidEmail");
        result.FirstError.Description.Should().Contain("Invalid email format");
    }

    [Fact]
    public async Task RegisterCommand_WhenServiceReturnsFailure_ReturnsValidationError()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Username = "existinguser",
            Email = "existing@example.com",
            Password = "password123",
            ConfirmPassword = "password123"
        };

        var authResult = new AuthenticationResult
        {
            Success = false,
            ErrorMessage = "Username already exists"
        };

        _mockAuthService.Setup(x => x.RegisterAsync("existinguser", "existing@example.com", "password123", string.Empty))
            .ReturnsAsync(authResult);

        var handler = new RegisterUserCommandHandler(_mockAuthService.Object, _mockRegisterLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Registration.Failed");
        result.FirstError.Description.Should().Contain("Username already exists");
    }

    [Fact]
    public async Task RegisterCommand_WhenServiceThrows_ReturnsFailureError()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "password123",
            ConfirmPassword = "password123"
        };

        _mockAuthService.Setup(x => x.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        var handler = new RegisterUserCommandHandler(_mockAuthService.Object, _mockRegisterLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Failure);
        result.FirstError.Code.Should().Be("Registration.Failure");
        result.FirstError.Description.Should().Contain("Database connection failed");
    }

    #endregion

    #region Helper Methods

    private User CreateTestUser(string username, string email)
    {
        return new User
        {
            Username = username,
            Email = email,
            PasswordHash = "hashed_password",
            Salt = "salt_value",
            Role = UserRole.User,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion
}
