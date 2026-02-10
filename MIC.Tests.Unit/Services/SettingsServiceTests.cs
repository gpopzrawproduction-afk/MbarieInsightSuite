using System;
using FluentAssertions;
using MIC.Desktop.Avalonia.Services;
using Xunit;

namespace MIC.Tests.Unit.Services;

/// <summary>
/// Tests for SettingsService.
/// Tests settings persistence, theme management, and event handling.
/// Target: 5 additional tests for settings functionality
/// </summary>
public class SettingsServiceTests
{
    #region Singleton & Settings Property Tests (2 tests)

    [Fact]
    public void SettingsService_Instance_ReturnsSingleton()
    {
        // Act
        var instance1 = SettingsService.Instance;
        var instance2 = SettingsService.Instance;

        // Assert
        instance1.Should().BeSameAs(instance2);
        instance1.Should().NotBeNull();
    }

    [Fact]
    public void Settings_Property_ReturnsUserSettings()
    {
        // Arrange
        var service = SettingsService.Instance;

        // Act
        var settings = service.Settings;

        // Assert
        settings.Should().NotBeNull();
        settings.Should().BeOfType<UserSettings>();
    }

    #endregion

    #region Theme Management Tests (3 tests)

    [Fact]
    public void CurrentTheme_SetToNewValue_RaisesThemeChangedEvent()
    {
        // Arrange
        var service = SettingsService.Instance;
        var initialTheme = service.CurrentTheme;
        var newTheme = initialTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
        
        ThemeChangedEventArgs? eventArgs = null;
        service.ThemeChanged += (sender, args) => { eventArgs = args; };

        // Act
        service.CurrentTheme = newTheme;

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.OldTheme.Should().Be(initialTheme);
        eventArgs.NewTheme.Should().Be(newTheme);
        service.CurrentTheme.Should().Be(newTheme);
    }

    [Fact]
    public void ToggleTheme_FromDark_SwitchesToLight()
    {
        // Arrange
        var service = SettingsService.Instance;
        service.CurrentTheme = AppTheme.Dark;

        // Act
        service.ToggleTheme();

        // Assert
        service.CurrentTheme.Should().Be(AppTheme.Light);
    }

    [Fact]
    public void ToggleTheme_FromLight_SwitchesToDark()
    {
        // Arrange
        var service = SettingsService.Instance;
        service.CurrentTheme = AppTheme.Light;

        // Act
        service.ToggleTheme();

        // Assert
        service.CurrentTheme.Should().Be(AppTheme.Dark);
    }

    #endregion
}
