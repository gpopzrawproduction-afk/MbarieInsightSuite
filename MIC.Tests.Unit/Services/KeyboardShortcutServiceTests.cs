using System;
using System.Linq;
using Avalonia.Input;
using FluentAssertions;
using MIC.Desktop.Avalonia.Services;
using Xunit;

namespace MIC.Tests.Unit.Services;

/// <summary>
/// Comprehensive tests for KeyboardShortcutService.
/// Tests shortcut registration, event triggering, and navigation shortcuts.
/// Target: 12 tests for keyboard shortcut functionality
/// </summary>
public class KeyboardShortcutServiceTests
{
    #region Singleton Tests (1 test)

    [Fact]
    public void KeyboardShortcutService_Instance_ReturnsSingleton()
    {
        // Act
        var instance1 = KeyboardShortcutService.Instance;
        var instance2 = KeyboardShortcutService.Instance;

        // Assert
        instance1.Should().BeSameAs(instance2);
        instance1.Should().NotBeNull();
    }

    #endregion

    #region Registration Tests (2 tests)

    [Fact]
    public void Register_WithCustomAction_DoesNotThrow()
    {
        // Arrange
        var service = new KeyboardShortcutService();
        var actionCalled = 0;
        Action customAction = () => { actionCalled++; };

        // Act & Assert
        FluentActions.Invoking(() => service.Register(Key.Z, KeyModifiers.Control | KeyModifiers.Alt, customAction))
            .Should().NotThrow();
    }

    [Fact]
    public void Unregister_WithExistingKeyCombo_DoesNotThrow()
    {
        // Arrange
        var service = new KeyboardShortcutService();
        Action customAction = () => { };
        service.Register(Key.Y, KeyModifiers.Control, customAction);

        // Act & Assert
        FluentActions.Invoking(() => service.Unregister(Key.Y, KeyModifiers.Control))
            .Should().NotThrow();
    }

    #endregion

    #region Event Subscription Tests (6 tests)

    [Fact]
    public void OnOpenCommandPalette_CanBeSubscribed()
    {
        // Arrange
        var service = new KeyboardShortcutService();
        var eventInvoked = false;

        // Act & Assert
        FluentActions.Invoking(() => service.OnOpenCommandPalette += () => { eventInvoked = true; })
            .Should().NotThrow();
    }

    [Fact]
    public void OnRefresh_CanBeSubscribed()
    {
        // Arrange
        var service = new KeyboardShortcutService();
        var eventInvoked = false;

        // Act & Assert
        FluentActions.Invoking(() => service.OnRefresh += () => { eventInvoked = true; })
            .Should().NotThrow();
    }

    [Fact]
    public void OnCreateNew_CanBeSubscribed()
    {
        // Arrange
        var service = new KeyboardShortcutService();
        var eventInvoked = false;

        // Act & Assert
        FluentActions.Invoking(() => service.OnCreateNew += () => { eventInvoked = true; })
            .Should().NotThrow();
    }

    [Fact]
    public void OnToggleTheme_CanBeSubscribed()
    {
        // Arrange
        var service = new KeyboardShortcutService();
        var eventInvoked = false;

        // Act & Assert
        FluentActions.Invoking(() => service.OnToggleTheme += () => { eventInvoked = true; })
            .Should().NotThrow();
    }

    [Fact]
    public void OnExport_CanBeSubscribed()
    {
        // Arrange
        var service = new KeyboardShortcutService();
        var eventInvoked = false;

        // Act & Assert
        FluentActions.Invoking(() => service.OnExport += () => { eventInvoked = true; })
            .Should().NotThrow();
    }

    [Fact]
    public void OnSearch_CanBeSubscribed()
    {
        // Arrange
        var service = new KeyboardShortcutService();
        var eventInvoked = false;

        // Act & Assert
        FluentActions.Invoking(() => service.OnSearch += () => { eventInvoked = true; })
            .Should().NotThrow();
    }

    #endregion

    #region Navigation Tests (1 test)

    [Fact]
    public void InvokeNavigate_WithViewName_TriggersNavigateEvent()
    {
        // Arrange
        var service = new KeyboardShortcutService();
        var navigatedTo = string.Empty;
        service.OnNavigate += (view) => { navigatedTo = view; };

        // Act
        service.InvokeNavigate("Settings");

        // Assert
        navigatedTo.Should().Be("Settings");
    }

    #endregion

    #region Shortcut List Tests (2 tests)

    [Fact]
    public void GetShortcutList_ReturnsAllDefaultShortcuts()
    {
        // Arrange
        var service = new KeyboardShortcutService();

        // Act
        var shortcuts = service.GetShortcutList();

        // Assert
        shortcuts.Should().NotBeEmpty();
        shortcuts.Should().HaveCountGreaterThan(10);
        shortcuts.Should().Contain(s => s.Shortcut == "Ctrl+K");
        shortcuts.Should().Contain(s => s.Description == "Go to Dashboard");
        shortcuts.Should().Contain(s => s.Category == "Navigation");
        shortcuts.Should().Contain(s => s.Description == "Refresh Data");
        shortcuts.Should().Contain(s => s.Description == "Toggle Theme");
    }

    [Fact]
    public void GetShortcutList_IncludesAllCategories()
    {
        // Arrange
        var service = new KeyboardShortcutService();

        // Act
        var shortcuts = service.GetShortcutList();
        var categories = shortcuts.Select(s => s.Category).Distinct().ToList();

        // Assert
        categories.Should().Contain("Navigation");
        categories.Should().Contain("Actions");
        categories.Should().Contain("Appearance");
        categories.Should().Contain("General");
        categories.Count.Should().Be(4);
    }

    #endregion
}
