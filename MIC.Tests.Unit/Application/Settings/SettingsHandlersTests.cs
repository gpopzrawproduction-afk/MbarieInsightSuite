using System;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Settings.Commands.SaveSettings;
using MIC.Core.Application.Settings.Queries.GetSettings;
using Moq;
using Xunit;

namespace MIC.Tests.Unit.Application.Settings;

/// <summary>
/// Comprehensive tests for Settings CQRS handlers.
/// Tests user settings persistence, retrieval, and default handling.
/// Target: 10 tests for settings handler coverage
/// </summary>
public class SettingsHandlersTests
{
    private readonly Mock<ISettingsService> _mockSettingsService;
    private readonly Mock<ILogger<SaveSettingsCommandHandler>> _mockSaveLogger;
    private readonly Mock<ILogger<GetSettingsQueryHandler>> _mockGetLogger;
    private readonly Guid _testUserId = Guid.NewGuid();

    public SettingsHandlersTests()
    {
        _mockSettingsService = new Mock<ISettingsService>();
        _mockSaveLogger = new Mock<ILogger<SaveSettingsCommandHandler>>();
        _mockGetLogger = new Mock<ILogger<GetSettingsQueryHandler>>();
    }

    #region SaveSettingsCommandHandler Tests (5 tests)

    [Fact]
    public async Task SaveSettings_WithValidSettings_ReturnsTrue()
    {
        // Arrange
        var settings = CreateTestSettings();
        var command = new SaveSettingsCommand
        {
            UserId = _testUserId,
            Settings = settings
        };

        _mockSettingsService.Setup(x => x.SaveUserSettingsAsync(_testUserId, settings, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockSettingsService.Setup(x => x.SaveSettingsAsync(settings, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new SaveSettingsCommandHandler(_mockSettingsService.Object, _mockSaveLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeTrue();
        _mockSettingsService.Verify(x => x.SaveUserSettingsAsync(_testUserId, settings, It.IsAny<CancellationToken>()), Times.Once);
        _mockSettingsService.Verify(x => x.SaveSettingsAsync(settings, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveSettings_SavesBothUserAndLocalSettings()
    {
        // Arrange
        var settings = CreateTestSettings();
        var command = new SaveSettingsCommand
        {
            UserId = _testUserId,
            Settings = settings
        };

        _mockSettingsService.Setup(x => x.SaveUserSettingsAsync(It.IsAny<Guid>(), It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockSettingsService.Setup(x => x.SaveSettingsAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new SaveSettingsCommandHandler(_mockSettingsService.Object, _mockSaveLogger.Object);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _mockSettingsService.Verify(x => x.SaveUserSettingsAsync(_testUserId, settings, It.IsAny<CancellationToken>()), Times.Once);
        _mockSettingsService.Verify(x => x.SaveSettingsAsync(settings, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveSettings_WithComplexSettings_PreservesAllProperties()
    {
        // Arrange
        var settings = new AppSettings
        {
            AI = new AISettings
            {
                Provider = "AzureOpenAI",
                ModelId = "gpt-4",
                Temperature = 0.8,
                MaxTokens = 8000,
                EnableEmailAnalysis = true,
                EnableChatAssistant = true
            },
            EmailSync = new EmailSyncSettings
            {
                SyncIntervalMinutes = 10,
                InitialSyncMonths = 6,
                MaxEmailsPerSync = 200,
                EnableAttachmentDownload = false
            },
            UI = new UISettings
            {
                Theme = "Light",
                Language = "es-ES",
                FontSize = 16,
                CompactMode = true
            }
        };

        var command = new SaveSettingsCommand
        {
            UserId = _testUserId,
            Settings = settings
        };

        AppSettings? capturedSettings = null;
        _mockSettingsService.Setup(x => x.SaveUserSettingsAsync(_testUserId, It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, AppSettings, CancellationToken>((_, s, _) => capturedSettings = s)
            .Returns(Task.CompletedTask);
        _mockSettingsService.Setup(x => x.SaveSettingsAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new SaveSettingsCommandHandler(_mockSettingsService.Object, _mockSaveLogger.Object);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        capturedSettings.Should().NotBeNull();
        capturedSettings!.AI.Provider.Should().Be("AzureOpenAI");
        capturedSettings.AI.Temperature.Should().Be(0.8);
        capturedSettings.EmailSync.SyncIntervalMinutes.Should().Be(10);
        capturedSettings.UI.Theme.Should().Be("Light");
    }

    [Fact]
    public async Task SaveSettings_WhenSaveUserSettingsFails_ReturnsFailureError()
    {
        // Arrange
        var command = new SaveSettingsCommand
        {
            UserId = _testUserId,
            Settings = CreateTestSettings()
        };

        _mockSettingsService.Setup(x => x.SaveUserSettingsAsync(It.IsAny<Guid>(), It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        var handler = new SaveSettingsCommandHandler(_mockSettingsService.Object, _mockSaveLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Failure);
        result.FirstError.Code.Should().Be("Settings.SaveFailed");
        result.FirstError.Description.Should().Contain("Database connection failed");
    }

    [Fact]
    public async Task SaveSettings_WhenSaveSettingsFails_ReturnsFailureError()
    {
        // Arrange
        var command = new SaveSettingsCommand
        {
            UserId = _testUserId,
            Settings = CreateTestSettings()
        };

        _mockSettingsService.Setup(x => x.SaveUserSettingsAsync(It.IsAny<Guid>(), It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockSettingsService.Setup(x => x.SaveSettingsAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("File system error"));

        var handler = new SaveSettingsCommandHandler(_mockSettingsService.Object, _mockSaveLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Failure);
        result.FirstError.Code.Should().Be("Settings.SaveFailed");
        result.FirstError.Description.Should().Contain("File system error");
    }

    #endregion

    #region GetSettingsQueryHandler Tests (5 tests)

    [Fact]
    public async Task GetSettings_WithValidUserId_ReturnsSettings()
    {
        // Arrange
        var query = new GetSettingsQuery { UserId = _testUserId };
        var expectedSettings = CreateTestSettings();

        _mockSettingsService.Setup(x => x.LoadUserSettingsAsync(_testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSettings);

        var handler = new GetSettingsQueryHandler(_mockSettingsService.Object, _mockGetLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.AI.Provider.Should().Be("OpenAI");
        result.Value.EmailSync.SyncIntervalMinutes.Should().Be(5);
        result.Value.UI.Theme.Should().Be("Dark");
        _mockSettingsService.Verify(x => x.LoadUserSettingsAsync(_testUserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSettings_WhenNoSettingsExist_ReturnsDefaultSettings()
    {
        // Arrange
        var query = new GetSettingsQuery { UserId = _testUserId };

        _mockSettingsService.Setup(x => x.LoadUserSettingsAsync(_testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppSettings)null!);

        var handler = new GetSettingsQueryHandler(_mockSettingsService.Object, _mockGetLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeOfType<AppSettings>();
        // Verify default values
        result.Value.AI.Should().NotBeNull();
        result.Value.EmailSync.Should().NotBeNull();
        result.Value.UI.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSettings_ReturnsCompleteSettingsStructure()
    {
        // Arrange
        var query = new GetSettingsQuery { UserId = _testUserId };
        var settings = new AppSettings
        {
            AI = new AISettings { Provider = "Custom", ModelId = "custom-model" },
            EmailSync = new EmailSyncSettings { SyncIntervalMinutes = 15 },
            UI = new UISettings { Theme = "Auto", Language = "fr-FR" },
            Notifications = new NotificationSettings { EnableSound = false },
            General = new GeneralSettings { AutoStart = true }
        };

        _mockSettingsService.Setup(x => x.LoadUserSettingsAsync(_testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var handler = new GetSettingsQueryHandler(_mockSettingsService.Object, _mockGetLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.AI.Provider.Should().Be("Custom");
        result.Value.EmailSync.SyncIntervalMinutes.Should().Be(15);
        result.Value.UI.Theme.Should().Be("Auto");
        result.Value.Notifications.EnableSound.Should().BeFalse();
        result.Value.General.AutoStart.Should().BeTrue();
    }

    [Fact]
    public async Task GetSettings_WhenLoadFails_ReturnsFailureError()
    {
        // Arrange
        var query = new GetSettingsQuery { UserId = _testUserId };

        _mockSettingsService.Setup(x => x.LoadUserSettingsAsync(_testUserId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        var handler = new GetSettingsQueryHandler(_mockSettingsService.Object, _mockGetLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Failure);
        result.FirstError.Code.Should().Be("Settings.LoadFailed");
        result.FirstError.Description.Should().Contain("Database error");
    }

    [Fact]
    public async Task GetSettings_WithDifferentUsers_LoadsUserSpecificSettings()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var settings1 = new AppSettings { UI = new UISettings { Theme = "Dark" } };
        var settings2 = new AppSettings { UI = new UISettings { Theme = "Light" } };

        _mockSettingsService.Setup(x => x.LoadUserSettingsAsync(userId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings1);
        _mockSettingsService.Setup(x => x.LoadUserSettingsAsync(userId2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings2);

        var handler = new GetSettingsQueryHandler(_mockSettingsService.Object, _mockGetLogger.Object);

        // Act
        var result1 = await handler.Handle(new GetSettingsQuery { UserId = userId1 }, CancellationToken.None);
        var result2 = await handler.Handle(new GetSettingsQuery { UserId = userId2 }, CancellationToken.None);

        // Assert
        result1.Value.UI.Theme.Should().Be("Dark");
        result2.Value.UI.Theme.Should().Be("Light");
        _mockSettingsService.Verify(x => x.LoadUserSettingsAsync(userId1, It.IsAny<CancellationToken>()), Times.Once);
        _mockSettingsService.Verify(x => x.LoadUserSettingsAsync(userId2, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Helper Methods

    private AppSettings CreateTestSettings()
    {
        return new AppSettings
        {
            AI = new AISettings
            {
                Provider = "OpenAI",
                ModelId = "gpt-4-turbo-preview",
                Temperature = 0.7,
                MaxTokens = 4000
            },
            EmailSync = new EmailSyncSettings
            {
                SyncIntervalMinutes = 5,
                InitialSyncMonths = 3,
                MaxEmailsPerSync = 100
            },
            UI = new UISettings
            {
                Theme = "Dark",
                Language = "en-US",
                FontSize = 14
            },
            Notifications = new NotificationSettings
            {
                EnableEmailNotifications = true,
                EnableDesktopNotifications = true
            },
            General = new GeneralSettings
            {
                AutoStart = false,
                CheckForUpdates = true
            }
        };
    }

    #endregion
}
