using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.Identity;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace MIC.Tests.Unit.Infrastructure.Identity;

/// <summary>
/// Tests for AuthenticationService covering login, registration, and error scenarios.
/// Target: 25 tests for comprehensive authentication coverage
/// </summary>
public class AuthenticationServiceTests
{
    private readonly IUserRepository _mockUserRepository;
    private readonly IPasswordHasher _mockPasswordHasher;
    private readonly IJwtTokenService _mockJwtTokenService;
    private readonly ILogger<AuthenticationService> _mockLogger;
    private readonly AuthenticationService _sut;

    public AuthenticationServiceTests()
    {
        _mockUserRepository = Substitute.For<IUserRepository>();
        _mockPasswordHasher = Substitute.For<IPasswordHasher>();
        _mockJwtTokenService = Substitute.For<IJwtTokenService>();
        _mockLogger = Substitute.For<ILogger<AuthenticationService>>();

        _sut = new AuthenticationService(
            _mockUserRepository,
            _mockPasswordHasher,
            _mockJwtTokenService,
            _mockLogger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullUserRepository_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new AuthenticationService(
            null!,
            _mockPasswordHasher,
            _mockJwtTokenService,
            _mockLogger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("userRepository");
    }

    [Fact]
    public void Constructor_WithNullPasswordHasher_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new AuthenticationService(
            _mockUserRepository,
            null!,
            _mockJwtTokenService,
            _mockLogger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("passwordHasher");
    }

    [Fact]
    public void Constructor_WithNullJwtTokenService_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new AuthenticationService(
            _mockUserRepository,
            _mockPasswordHasher,
            null!,
            _mockLogger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("jwtTokenService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new AuthenticationService(
            _mockUserRepository,
            _mockPasswordHasher,
            _mockJwtTokenService,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsSuccessWithToken()
    {
        // Arrange
        var username = "testuser";
        var password = "Password123!";
        var user = CreateTestUser(username, "test@example.com");
        var expectedToken = "test-jwt-token";

        _mockUserRepository.GetByUsernameAsync(username).Returns(user);
        _mockPasswordHasher.VerifyPassword(password, user.PasswordHash, user.Salt).Returns(true);
        _mockJwtTokenService.GenerateToken(user).Returns(expectedToken);

        // Act
        var result = await _sut.LoginAsync(username, password);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Token.Should().Be(expectedToken);
        result.User.Should().Be(user);
        result.ErrorMessage.Should().BeNull();

        await _mockUserRepository.Received(1).UpdateAsync(Arg.Is<User>(u => u.LastLoginAt.HasValue));
    }

    [Fact]
    public async Task LoginAsync_WithNullUsername_ReturnsFailure()
    {
        // Act
        var result = await _sut.LoginAsync(null!, "password");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Username and password are required.");
        result.Token.Should().BeNull();
        result.User.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WithEmptyUsername_ReturnsFailure()
    {
        // Act
        var result = await _sut.LoginAsync("", "password");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Username and password are required.");
    }

    [Fact]
    public async Task LoginAsync_WithWhitespaceUsername_ReturnsFailure()
    {
        // Act
        var result = await _sut.LoginAsync("   ", "password");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Username and password are required.");
    }

    [Fact]
    public async Task LoginAsync_WithNullPassword_ReturnsFailure()
    {
        // Act
        var result = await _sut.LoginAsync("username", null!);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Username and password are required.");
    }

    [Fact]
    public async Task LoginAsync_WithEmptyPassword_ReturnsFailure()
    {
        // Act
        var result = await _sut.LoginAsync("username", "");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Username and password are required.");
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        _mockUserRepository.GetByUsernameAsync(Arg.Any<string>()).Returns((User?)null);

        // Act
        var result = await _sut.LoginAsync("nonexistent", "password");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid username or password.");
        result.User.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WithInactiveUser_ReturnsFailure()
    {
        // Arrange
        var user = CreateTestUser("inactive", "inactive@example.com");
        user.Deactivate();

        _mockUserRepository.GetByUsernameAsync("inactive").Returns(user);

        // Act
        var result = await _sut.LoginAsync("inactive", "password");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid username or password.");
    }

    [Fact]
    public async Task LoginAsync_WithIncorrectPassword_ReturnsFailure()
    {
        // Arrange
        var user = CreateTestUser("testuser", "test@example.com");
        _mockUserRepository.GetByUsernameAsync("testuser").Returns(user);
        _mockPasswordHasher.VerifyPassword(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(false);

        // Act
        var result = await _sut.LoginAsync("testuser", "wrongpassword");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid username or password.");
        result.User.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_UpdatesLastLoginTimestamp()
    {
        // Arrange
        var user = CreateTestUser("testuser", "test@example.com");
        var beforeLogin = DateTimeOffset.UtcNow;

        _mockUserRepository.GetByUsernameAsync("testuser").Returns(user);
        _mockPasswordHasher.VerifyPassword(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(true);
        _mockJwtTokenService.GenerateToken(user).Returns("token");

        // Act
        await _sut.LoginAsync("testuser", "password");

        // Assert
        await _mockUserRepository.Received(1).UpdateAsync(Arg.Is<User>(u =>
            u.LastLoginAt.HasValue &&
            u.LastLoginAt.Value >= beforeLogin));
    }

    #endregion

    #region RegisterAsync Tests

    [Fact]
    public async Task RegisterAsync_WithValidCredentials_ReturnsSuccessWithToken()
    {
        // Arrange
        var username = "newuser";
        var email = "new@example.com";
        var password = "SecurePass123!";
        var displayName = "New User";
        var hashedPassword = "hashed_password";
        var salt = "random_salt";
        var expectedToken = "registration-jwt-token";

        _mockUserRepository.GetByUsernameAsync(username).Returns((User?)null);
        _mockUserRepository.GetByEmailAsync(email).Returns((User?)null);
        _mockUserRepository.GetByUsernameAsync("admin").Returns((User?)null); // First user
        _mockPasswordHasher.HashPassword(password).Returns((hashedPassword, salt));
        _mockUserRepository.CreateAsync(Arg.Any<User>()).Returns(callInfo => callInfo.Arg<User>());
        _mockJwtTokenService.GenerateToken(Arg.Any<User>()).Returns(expectedToken);

        // Act
        var result = await _sut.RegisterAsync(username, email, password, displayName);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Token.Should().Be(expectedToken);
        result.User.Should().NotBeNull();
        result.User!.Username.Should().Be(username);
        result.User.Email.Should().Be(email);
        result.ErrorMessage.Should().BeNull();

        await _mockUserRepository.Received(1).CreateAsync(Arg.Any<User>());
    }

    [Fact]
    public async Task RegisterAsync_FirstUser_AssignsAdminRole()
    {
        // Arrange
        var username = "firstuser";
        var email = "first@example.com";
        var password = "Password123!";

        _mockUserRepository.GetByUsernameAsync(username).Returns((User?)null);
        _mockUserRepository.GetByEmailAsync(email).Returns((User?)null);
        _mockUserRepository.GetByUsernameAsync("admin").Returns((User?)null); // No admin exists
        _mockPasswordHasher.HashPassword(password).Returns(("hash", "salt"));
        _mockJwtTokenService.GenerateToken(Arg.Any<User>()).Returns("token");

        User? capturedUser = null;
        _mockUserRepository.CreateAsync(Arg.Do<User>(u => capturedUser = u))
            .Returns(callInfo => callInfo.Arg<User>());

        // Act
        await _sut.RegisterAsync(username, email, password, "First User");

        // Assert
        capturedUser.Should().NotBeNull();
        capturedUser!.Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public async Task RegisterAsync_SubsequentUser_AssignsUserRole()
    {
        // Arrange
        var existingAdmin = CreateTestUser("admin", "admin@example.com");
        var username = "seconduser";
        var email = "second@example.com";
        var password = "Password123!";

        _mockUserRepository.GetByUsernameAsync(username).Returns((User?)null);
        _mockUserRepository.GetByEmailAsync(email).Returns((User?)null);
        _mockUserRepository.GetByUsernameAsync("admin").Returns(existingAdmin); // Admin exists
        _mockPasswordHasher.HashPassword(password).Returns(("hash", "salt"));
        _mockJwtTokenService.GenerateToken(Arg.Any<User>()).Returns("token");

        User? capturedUser = null;
        _mockUserRepository.CreateAsync(Arg.Do<User>(u => capturedUser = u))
            .Returns(callInfo => callInfo.Arg<User>());

        // Act
        await _sut.RegisterAsync(username, email, password, "Second User");

        // Assert
        capturedUser.Should().NotBeNull();
        capturedUser!.Role.Should().Be(UserRole.User);
    }

    [Fact]
    public async Task RegisterAsync_WithNullUsername_ReturnsFailure()
    {
        // Act
        var result = await _sut.RegisterAsync(null!, "email@test.com", "password", "Name");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Username, email, and password are required.");
    }

    [Fact]
    public async Task RegisterAsync_WithEmptyEmail_ReturnsFailure()
    {
        // Act
        var result = await _sut.RegisterAsync("username", "", "password", "Name");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Username, email, and password are required.");
    }

    [Fact]
    public async Task RegisterAsync_WithWhitespacePassword_ReturnsFailure()
    {
        // Act
        var result = await _sut.RegisterAsync("username", "email@test.com", "   ", "Name");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Username, email, and password are required.");
    }

    [Fact]
    public async Task RegisterAsync_WithInvalidEmailFormat_ReturnsFailure()
    {
        // Act
        var result = await _sut.RegisterAsync("username", "invalidemail", "password", "Name");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Email address is not in a valid format.");
    }

    [Fact]
    public async Task RegisterAsync_WithExistingUsername_ReturnsFailure()
    {
        // Arrange
        var existingUser = CreateTestUser("existinguser", "existing@example.com");
        _mockUserRepository.GetByUsernameAsync("existinguser").Returns(existingUser);

        // Act
        var result = await _sut.RegisterAsync("existinguser", "new@example.com", "password", "Name");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("A user with this username already exists.");
        result.User.Should().BeNull();

        await _mockUserRepository.DidNotReceive().CreateAsync(Arg.Any<User>());
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ReturnsFailure()
    {
        // Arrange
        var existingUser = CreateTestUser("user1", "taken@example.com");
        _mockUserRepository.GetByUsernameAsync("newuser").Returns((User?)null);
        _mockUserRepository.GetByEmailAsync("taken@example.com").Returns(existingUser);

        // Act
        var result = await _sut.RegisterAsync("newuser", "taken@example.com", "password", "Name");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("A user with this email address already exists.");
        result.User.Should().BeNull();

        await _mockUserRepository.DidNotReceive().CreateAsync(Arg.Any<User>());
    }

    [Fact]
    public async Task RegisterAsync_TrimsUsernameAndEmail()
    {
        // Arrange
        var username = "  spaceduser  ";
        var email = "  spaced@example.com  ";
        var password = "Password123!";

        _mockUserRepository.GetByUsernameAsync(Arg.Any<string>()).Returns((User?)null);
        _mockUserRepository.GetByEmailAsync(Arg.Any<string>()).Returns((User?)null);
        _mockUserRepository.GetByUsernameAsync("admin").Returns((User?)null);
        _mockPasswordHasher.HashPassword(password).Returns(("hash", "salt"));
        _mockJwtTokenService.GenerateToken(Arg.Any<User>()).Returns("token");

        User? capturedUser = null;
        _mockUserRepository.CreateAsync(Arg.Do<User>(u => capturedUser = u))
            .Returns(callInfo => callInfo.Arg<User>());

        // Act
        await _sut.RegisterAsync(username, email, password, "Display Name");

        // Assert
        capturedUser.Should().NotBeNull();
        capturedUser!.Username.Should().Be("spaceduser");
        capturedUser.Email.Should().Be("spaced@example.com");
    }

    [Fact]
    public async Task RegisterAsync_WithNullDisplayName_UsesUsernameTrimmed()
    {
        // Arrange
        var username = "  testuser  ";
        var email = "test@example.com";
        var password = "Password123!";

        _mockUserRepository.GetByUsernameAsync(Arg.Any<string>()).Returns((User?)null);
        _mockUserRepository.GetByEmailAsync(Arg.Any<string>()).Returns((User?)null);
        _mockUserRepository.GetByUsernameAsync("admin").Returns((User?)null);
        _mockPasswordHasher.HashPassword(password).Returns(("hash", "salt"));
        _mockJwtTokenService.GenerateToken(Arg.Any<User>()).Returns("token");

        User? capturedUser = null;
        _mockUserRepository.CreateAsync(Arg.Do<User>(u => capturedUser = u))
            .Returns(callInfo => callInfo.Arg<User>());

        // Act
        await _sut.RegisterAsync(username, email, password, null!);

        // Assert
        capturedUser.Should().NotBeNull();
        capturedUser!.FullName.Should().Be("testuser");
    }

    [Fact]
    public async Task RegisterAsync_WithEmptyDisplayName_UsesUsername()
    {
        // Arrange
        var username = "testuser";
        var email = "test@example.com";
        var password = "Password123!";

        _mockUserRepository.GetByUsernameAsync(Arg.Any<string>()).Returns((User?)null);
        _mockUserRepository.GetByEmailAsync(Arg.Any<string>()).Returns((User?)null);
        _mockUserRepository.GetByUsernameAsync("admin").Returns((User?)null);
        _mockPasswordHasher.HashPassword(password).Returns(("hash", "salt"));
        _mockJwtTokenService.GenerateToken(Arg.Any<User>()).Returns("token");

        User? capturedUser = null;
        _mockUserRepository.CreateAsync(Arg.Do<User>(u => capturedUser = u))
            .Returns(callInfo => callInfo.Arg<User>());

        // Act
        await _sut.RegisterAsync(username, email, password, "");

        // Assert
        capturedUser.Should().NotBeNull();
        capturedUser!.FullName.Should().Be("testuser");
    }

    #endregion

    #region LogoutAsync Tests

    [Fact]
    public async Task LogoutAsync_Completes()
    {
        // Act
        var act = async () => await _sut.LogoutAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region GetCurrentUserAsync Tests

    [Fact]
    public async Task GetCurrentUserAsync_ReturnsNull()
    {
        // Act
        var result = await _sut.GetCurrentUserAsync();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Exception Propagation Tests

    [Fact]
    public async Task LoginAsync_WhenUpdateAsyncThrows_PropagatesException()
    {
        var user = CreateTestUser("testuser", "test@example.com");
        _mockUserRepository.GetByUsernameAsync("testuser").Returns(user);
        _mockPasswordHasher.VerifyPassword("password", Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _mockUserRepository.UpdateAsync(Arg.Any<User>())
            .Returns(Task.FromException(new InvalidOperationException("DB error")));

        var act = () => _sut.LoginAsync("testuser", "password");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("DB error");
    }

    [Fact]
    public async Task LoginAsync_WhenGenerateTokenThrows_PropagatesException()
    {
        var user = CreateTestUser("testuser", "test@example.com");
        _mockUserRepository.GetByUsernameAsync("testuser").Returns(user);
        _mockPasswordHasher.VerifyPassword("password", Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _mockJwtTokenService.GenerateToken(Arg.Any<User>())
            .Throws(new InvalidOperationException("Token error"));

        var act = () => _sut.LoginAsync("testuser", "password");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Token error");
    }

    [Fact]
    public async Task RegisterAsync_WhenHashPasswordThrows_PropagatesException()
    {
        _mockUserRepository.GetByUsernameAsync(Arg.Any<string>()).Returns((User?)null);
        _mockUserRepository.GetByEmailAsync(Arg.Any<string>()).Returns((User?)null);
        _mockPasswordHasher.HashPassword("password")
            .Throws(new InvalidOperationException("Hash error"));

        var act = () => _sut.RegisterAsync("user", "user@test.com", "password", "User");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Hash error");
    }

    [Fact]
    public async Task RegisterAsync_WhenCreateAsyncThrows_PropagatesException()
    {
        _mockUserRepository.GetByUsernameAsync(Arg.Any<string>()).Returns((User?)null);
        _mockUserRepository.GetByEmailAsync(Arg.Any<string>()).Returns((User?)null);
        _mockPasswordHasher.HashPassword("password").Returns(("hash", "salt"));
        _mockUserRepository.CreateAsync(Arg.Any<User>())
            .ThrowsAsync(new InvalidOperationException("Create error"));

        var act = () => _sut.RegisterAsync("user", "user@test.com", "password", "User");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Create error");
    }

    [Fact]
    public async Task RegisterAsync_WhenGenerateTokenThrows_PropagatesException()
    {
        _mockUserRepository.GetByUsernameAsync(Arg.Any<string>()).Returns((User?)null);
        _mockUserRepository.GetByEmailAsync(Arg.Any<string>()).Returns((User?)null);
        _mockPasswordHasher.HashPassword("password").Returns(("hash", "salt"));
        _mockUserRepository.CreateAsync(Arg.Any<User>()).Returns(callInfo => callInfo.Arg<User>());
        _mockJwtTokenService.GenerateToken(Arg.Any<User>())
            .Throws(new InvalidOperationException("Token gen error"));

        var act = () => _sut.RegisterAsync("user", "user@test.com", "password", "User");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Token gen error");
    }

    #endregion

    #region Helper Methods

    private static User CreateTestUser(string username, string email)
    {
        var user = new User();
        user.SetCredentials(username, email);
        user.SetPasswordHash("hashed_password", "salt");
        user.UpdateProfile(username);
        user.SetRole(UserRole.User);
        user.Activate();

        // Set Id using reflection
        var idProperty = typeof(User).GetProperty("Id");
        idProperty?.SetValue(user, Guid.NewGuid());

        return user;
    }

    #endregion
}
