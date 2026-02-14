using System;
using FluentAssertions;
using MIC.Desktop.Avalonia.Services;
using MIC.Desktop.Avalonia.ViewModels;
using Xunit;

namespace MIC.Tests.Unit.ViewModels;

/// <summary>
/// Tests for UserProfileViewModel covering properties and commands.
/// Target: 10 tests for user profile functionality
/// </summary>
[Collection("UserSessionServiceTests")]
public class UserProfileViewModelTests : IDisposable
{
    private readonly SessionStorageScope _sessionScope;

    public UserProfileViewModelTests()
    {
        _sessionScope = new SessionStorageScope();
    }

    public void Dispose()
    {
        _sessionScope.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_InitializesCommands()
    {
        // Act
        var viewModel = new UserProfileViewModel();

        // Assert
        viewModel.ViewProfileCommand.Should().NotBeNull();
        viewModel.PreferencesCommand.Should().NotBeNull();
        viewModel.KeyboardShortcutsCommand.Should().NotBeNull();
        viewModel.HelpCommand.Should().NotBeNull();
        viewModel.LogoutCommand.Should().NotBeNull();
    }

    #endregion

    #region Property Tests

    [Fact]
    public void DisplayName_WhenSessionSet_ReturnsUserName()
    {
        // Arrange
        UserSessionService.Instance.SetSession("user-id", "testuser", "test@example.com", "Test User", "token");
        var viewModel = new UserProfileViewModel();

        // Act
        var displayName = viewModel.DisplayName;

        // Assert
        displayName.Should().Be("Test User");
    }

    [Fact]
    public void Email_WhenSessionSet_ReturnsUserEmail()
    {
        // Arrange
        UserSessionService.Instance.SetSession("user-id", "testuser", "test@example.com", "Test User", "token");
        var viewModel = new UserProfileViewModel();

        // Act
        var email = viewModel.Email;

        // Assert
        email.Should().Be("test@example.com");
    }

    [Fact]
    public void UserInitials_WhenSessionSet_ReturnsInitials()
    {
        // Arrange
        UserSessionService.Instance.SetSession("user-id", "testuser", "test@example.com", "John Doe", "token");
        var viewModel = new UserProfileViewModel();

        // Act
        var initials = viewModel.UserInitials;

        // Assert
        initials.Should().Be("JD");
    }

    [Fact]
    public void RoleDisplay_WhenSessionSet_ReturnsRole()
    {
        // Arrange
        UserSessionService.Instance.SetSession("user-id", "testuser", "test@example.com", "Test User", "token");
        var viewModel = new UserProfileViewModel();

        // Act
        var role = viewModel.RoleDisplay;

        // Assert
        role.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void SessionInfo_WhenSessionActive_ReturnsFormattedDuration()
    {
        // Arrange
        UserSessionService.Instance.SetSession("user-id", "testuser", "test@example.com", "Test User", "token");
        var viewModel = new UserProfileViewModel();

        // Act
        var sessionInfo = viewModel.SessionInfo;

        // Assert
        sessionInfo.Should().Contain("Signed in");
        sessionInfo.Should().Contain("ago");
    }

    [Fact]
    public void SessionInfo_WhenNoSession_ReturnsNotSignedIn()
    {
        // Arrange - clear any existing session
        UserSessionService.Instance.Clear();
        var viewModel = new UserProfileViewModel();

        // Act
        var sessionInfo = viewModel.SessionInfo;

        // Assert
        sessionInfo.Should().Be("Not signed in");
    }

    #endregion

    #region Command Tests

    [Fact]
    public void ViewProfileCommand_CanExecute()
    {
        // Arrange
        var viewModel = new UserProfileViewModel();

        // Act & Assert
        viewModel.ViewProfileCommand.Execute().Subscribe();
        // No exception means success
    }

    [Fact]
    public void KeyboardShortcutsCommand_CanExecute()
    {
        // Arrange
        var viewModel = new UserProfileViewModel();

        // Act & Assert
        viewModel.KeyboardShortcutsCommand.Execute().Subscribe();
        // No exception means success
    }

    [Fact]
    public void HelpCommand_CanExecute()
    {
        // Arrange
        var viewModel = new UserProfileViewModel();

        // Act & Assert
        viewModel.HelpCommand.Execute().Subscribe();
        // No exception means success
    }

    #endregion

    private sealed class SessionStorageScope : IDisposable
    {
        private readonly string _sessionPath;
        private readonly string? _backupPath;
        private readonly bool _hadExisting;

        public SessionStorageScope()
        {
            var directory = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MIC");
            System.IO.Directory.CreateDirectory(directory);

            _sessionPath = System.IO.Path.Combine(directory, "session.json");

            if (System.IO.File.Exists(_sessionPath))
            {
                _backupPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"mic-session-backup-{Guid.NewGuid():N}.json");
                System.IO.File.Copy(_sessionPath, _backupPath, overwrite: true);
                _hadExisting = true;
            }
        }

        public void Dispose()
        {
            try
            {
                if (_hadExisting && _backupPath != null && System.IO.File.Exists(_backupPath))
                {
                    System.IO.File.Copy(_backupPath, _sessionPath, overwrite: true);
                    System.IO.File.Delete(_backupPath);
                }
                else if (System.IO.File.Exists(_sessionPath))
                {
                    System.IO.File.Delete(_sessionPath);
                }
            }
            catch
            {
                // Ignore cleanup failures
            }
        }
    }
}
