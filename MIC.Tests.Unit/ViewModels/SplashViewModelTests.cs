using System.Threading.Tasks;
using FluentAssertions;
using MIC.Desktop.Avalonia.ViewModels;
using Xunit;

namespace MIC.Tests.Unit.ViewModels;

/// <summary>
/// Tests for SplashViewModel covering initialization and startup sequence.
/// Target: 8 tests for splash screen functionality
/// </summary>
public class SplashViewModelTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_InitializesProperties()
    {
        // Act
        var viewModel = new SplashViewModel();

        // Assert
        viewModel.LoadingMessage.Should().Be("Initializing...");
    }

    #endregion

    #region LoadingMessage Tests

    [Fact]
    public void LoadingMessage_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        var viewModel = new SplashViewModel();
        var propertyChangedRaised = false;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(viewModel.LoadingMessage))
                propertyChangedRaised = true;
        };

        // Act
        viewModel.LoadingMessage = "Loading...";

        // Assert
        propertyChangedRaised.Should().BeTrue();
        viewModel.LoadingMessage.Should().Be("Loading...");
    }

    [Fact]
    public void LoadingMessage_DefaultValue_IsInitializing()
    {
        // Arrange & Act
        var viewModel = new SplashViewModel();

        // Assert
        viewModel.LoadingMessage.Should().Be("Initializing...");
    }

    #endregion

    #region RunStartupSequenceAsync Tests

    [Fact]
    public async Task RunStartupSequenceAsync_UpdatesLoadingMessage()
    {
        // Arrange
        var viewModel = new SplashViewModel();
        var messagesReceived = new System.Collections.Generic.List<string>();
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(viewModel.LoadingMessage))
                messagesReceived.Add(viewModel.LoadingMessage);
        };

        // Act
        await viewModel.RunStartupSequenceAsync();

        // Assert
        messagesReceived.Should().Contain("Initializing database...");
        messagesReceived.Should().Contain("Loading configuration...");
        messagesReceived.Should().Contain("Connecting services...");
        messagesReceived.Should().Contain("Almost ready...");
    }

    [Fact]
    public async Task RunStartupSequenceAsync_ProgressesThroughSteps()
    {
        // Arrange
        var viewModel = new SplashViewModel();

        // Act
        var task = viewModel.RunStartupSequenceAsync();
        await task;

        // Assert
        viewModel.LoadingMessage.Should().Be("Almost ready...");
    }

    [Fact]
    public async Task RunStartupSequenceAsync_Completes()
    {
        // Arrange
        var viewModel = new SplashViewModel();

        // Act
        var task = viewModel.RunStartupSequenceAsync();
        await task;

        // Assert
        task.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task RunStartupSequenceAsync_CanBeCalledMultipleTimes()
    {
        // Arrange
        var viewModel = new SplashViewModel();

        // Act
        await viewModel.RunStartupSequenceAsync();
        await viewModel.RunStartupSequenceAsync();

        // Assert
        viewModel.LoadingMessage.Should().Be("Almost ready...");
    }

    [Fact]
    public void LoadingMessage_CanBeSetManually()
    {
        // Arrange
        var viewModel = new SplashViewModel();

        // Act
        viewModel.LoadingMessage = "Custom message";

        // Assert
        viewModel.LoadingMessage.Should().Be("Custom message");
    }

    #endregion
}
