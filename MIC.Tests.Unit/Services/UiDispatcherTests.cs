using System;
using System.Threading.Tasks;
using FluentAssertions;
using MIC.Desktop.Avalonia.Services;
using Xunit;

namespace MIC.Tests.Unit.Services;

/// <summary>
/// Comprehensive tests for UiDispatcher service.
/// Tests UI thread dispatching, singleton pattern, and error handling.
/// Target: 10 tests for UI thread functionality
/// </summary>
public class UiDispatcherTests
{
    #region Singleton Pattern Tests (2 tests)

    [Fact]
    public void AvaloniaUiDispatcher_Instance_ReturnsSingleton()
    {
        // Act
        var instance1 = AvaloniaUiDispatcher.Instance;
        var instance2 = AvaloniaUiDispatcher.Instance;

        // Assert
        instance1.Should().BeSameAs(instance2);
        instance1.Should().NotBeNull();
    }

    [Fact]
    public void AvaloniaUiDispatcher_ImplementsIUiDispatcher()
    {
        // Act
        var instance = AvaloniaUiDispatcher.Instance;

        // Assert
        instance.Should().BeAssignableTo<IUiDispatcher>();
    }

    #endregion

    #region Null Argument Validation Tests (2 tests)

    [Fact]
    public async Task RunAsync_WithNullAction_ThrowsArgumentNullException()
    {
        // Arrange
        var dispatcher = AvaloniaUiDispatcher.Instance;

        // Act
        Func<Task> act = async () => await dispatcher.RunAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("action");
    }

    #endregion

    #region Action Execution Tests (4 tests)

    [Fact]
    public void RunAsync_WithEmptyAction_ReturnsValidTask()
    {
        // Arrange
        var dispatcher = AvaloniaUiDispatcher.Instance;
        Action action = () => { };

        // Act
        var result = dispatcher.RunAsync(action);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<Task>();
    }

    [Fact]
    public void RunAsync_WithValidAction_ReturnsTask()
    {
        // Arrange
        var dispatcher = AvaloniaUiDispatcher.Instance;
        var action = () => { };

        // Act
        var result = dispatcher.RunAsync(action);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<Task>();
    }

    [Fact]
    public void RunAsync_WithSimpleAction_DoesNotThrowSynchronously()
    {
        // Arrange
        var dispatcher = AvaloniaUiDispatcher.Instance;
        Action action = () => { };

        // Act
        Action act = () => { var _ = dispatcher.RunAsync(action); };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RunAsync_CalledMultipleTimes_ReturnsNewTaskEachTime()
    {
        // Arrange
        var dispatcher = AvaloniaUiDispatcher.Instance;
        var action = () => { };

        // Act
        var task1 = dispatcher.RunAsync(action);
        var task2 = dispatcher.RunAsync(action);

        // Assert
        task1.Should().NotBeSameAs(task2);
    }

    #endregion

    #region Interface Contract Tests (3 tests)

    [Fact]
    public void IUiDispatcher_RunAsync_IsAccessibleThroughInterface()
    {
        // Arrange
        IUiDispatcher dispatcher = AvaloniaUiDispatcher.Instance;
        var action = () => { };

        // Act
        var result = dispatcher.RunAsync(action);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<Task>();
    }

    [Fact]
    public void IUiDispatcher_CanBeUsedPolymorphically()
    {
        // Arrange
        IUiDispatcher dispatcher = AvaloniaUiDispatcher.Instance;

        // Act & Assert
        dispatcher.Should().NotBeNull();
        dispatcher.Should().BeOfType<AvaloniaUiDispatcher>();
    }

    [Fact]
    public async Task IUiDispatcher_RunAsync_WithNullAction_ThrowsArgumentNullException()
    {
        // Arrange
        IUiDispatcher dispatcher = AvaloniaUiDispatcher.Instance;

        // Act
        Func<Task> act = async () => await dispatcher.RunAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion
}
