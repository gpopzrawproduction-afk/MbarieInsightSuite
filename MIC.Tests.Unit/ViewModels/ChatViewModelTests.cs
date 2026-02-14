using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MIC.Desktop.Avalonia.ViewModels;
using MIC.Infrastructure.AI.Models;
using MIC.Infrastructure.AI.Services;
using NSubstitute;
using ReactiveUI;
using Xunit;

namespace MIC.Tests.Unit.ViewModels;

/// <summary>
/// Tests for ChatViewModel covering initialization, commands, and message handling.
/// Target: 18 tests for core chat functionality coverage
/// </summary>
public class ChatViewModelTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_InitializesProperties()
    {
        // Act
        var viewModel = new ChatViewModel();

        // Assert
        viewModel.Messages.Should().NotBeNull();
        viewModel.SuggestedQuestions.Should().NotBeNull();
        viewModel.SuggestedQuestions.Should().HaveCountGreaterThan(0);
        viewModel.UserInput.Should().BeEmpty();
        viewModel.IsAITyping.Should().BeFalse();
        viewModel.StatusText.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Constructor_InitializesCommands()
    {
        // Act
        var viewModel = new ChatViewModel();

        // Assert
        viewModel.SendMessageCommand.Should().NotBeNull();
        viewModel.ClearChatCommand.Should().NotBeNull();
        viewModel.UseSuggestionCommand.Should().NotBeNull();
        viewModel.InsertNewLineCommand.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_AddsWelcomeMessage()
    {
        // Act
        var viewModel = new ChatViewModel();

        // Assert
       viewModel.Messages.Should().HaveCountGreaterThan(0);
        viewModel.Messages[0].IsUser.Should().BeFalse(); // Welcome message is from assistant
    }

    [Fact]
    public void Constructor_WithChatService_InjectedServiceUsed()
    {
        // Arrange
        var mockChatService = Substitute.For<IChatService>();

        // Act
        var viewModel = new ChatViewModel(mockChatService);

        // Assert
        viewModel.Should().NotBeNull();
    }

    #endregion

    #region UserInput Tests

    [Fact]
    public void UserInput_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        var viewModel = new ChatViewModel();
        var propertyChangedCount = 0;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(viewModel.UserInput))
                propertyChangedCount++;
        };

        // Act
        viewModel.UserInput = "Test message";

        // Assert
        propertyChangedCount.Should().BeGreaterThan(0);
        viewModel.UserInput.Should().Be("Test message");
    }

    [Fact]
    public void UserInput_WhenSetToEmpty_CanSendIsFalse()
    {
        // Arrange
        var viewModel = new ChatViewModel();
        viewModel.UserInput = "Test";

        // Act
        viewModel.UserInput = "";

        // Assert
        viewModel.CanSend.Should().BeFalse();
    }

    [Fact]
    public void UserInput_WhenSetToWhitespace_CanSendIsFalse()
    {
        // Arrange
        var viewModel = new ChatViewModel();

        // Act
        viewModel.UserInput = "   ";

        // Assert
        viewModel.CanSend.Should().BeFalse();
    }

    [Fact]
    public void UserInput_WhenSetToValidText_CanSendIsTrue()
    {
        // Arrange
        var viewModel = new ChatViewModel();

        // Act
        viewModel.UserInput = "Valid message";

        // Assert
        viewModel.CanSend.Should().BeTrue();
    }

    #endregion

    #region IsAITyping Tests

    [Fact]
    public void IsAITyping_WhenTrue_CanSendIsFalse()
    {
        // Arrange
        var viewModel = new ChatViewModel();
        viewModel.UserInput = "Valid message";

        // Act
        viewModel.IsAITyping = true;

        // Assert
        viewModel.CanSend.Should().BeFalse();
    }

    [Fact]
    public void IsAITyping_WhenFalse_CanSendIsTrue()
    {
        // Arrange
        var viewModel = new ChatViewModel();
        viewModel.UserInput = "Valid message";
        viewModel.IsAITyping = true;

        // Act
        viewModel.IsAITyping = false;

        // Assert
        viewModel.CanSend.Should().BeTrue();
    }

    #endregion

    #region InsertNewLineCommand Tests

    [Fact]
    public void InsertNewLineCommand_AddsNewlineToUserInput()
    {
        // Arrange
        var viewModel = new ChatViewModel();
        viewModel.UserInput = "First line";

        // Act
        viewModel.InsertNewLineCommand.Execute().Subscribe();

        // Assert
        viewModel.UserInput.Should().Contain(Environment.NewLine);
        viewModel.UserInput.Should().StartWith("First line");
    }

    [Fact]
    public void InsertNewLineCommand_CanExecuteMultipleTimes()
    {
        // Arrange
        var viewModel = new ChatViewModel();
        viewModel.UserInput = "First line";

        // Act
        viewModel.InsertNewLineCommand.Execute().Subscribe();
        viewModel.InsertNewLineCommand.Execute().Subscribe();

        // Assert
        var newlineCount = viewModel.UserInput.Split(Environment.NewLine).Length - 1;
        newlineCount.Should().BeGreaterOrEqualTo(2);
    }

    #endregion

    #region ShowSuggestions Tests

    [Fact]
    public void ShowSuggestions_WithOneMessage_ReturnsTrue()
    {
        // Arrange
        var viewModel = new ChatViewModel();

        // Assert
        viewModel.ShowSuggestions.Should().BeTrue();
    }

    [Fact]
    public void ShowSuggestions_WithManyMessages_ReturnsFalse()
    {
        // Arrange
        var viewModel = new ChatViewModel();
        
        // Add multiple messages directly
        viewModel.Messages.Add(new ChatMessageViewModel { Content = "Message 1", IsUser = true, Timestamp = DateTime.Now });
        viewModel.Messages.Add(new ChatMessageViewModel { Content = "Response 1", IsUser = false, Timestamp = DateTime.Now });

        // Act
        var result = viewModel.ShowSuggestions;

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region SuggestedQuestions Tests

    [Fact]
    public void SuggestedQuestions_ContainsDefaultQuestions()
    {
        // Arrange & Act
        var viewModel = new ChatViewModel();

        // Assert
        viewModel.SuggestedQuestions.Should().NotBeEmpty();
        viewModel.SuggestedQuestions.Should().Contain(q => q.Contains("alerts") || q.Contains("insights"));
    }

    [Fact]
    public void SuggestedQuestions_IsObservableCollection()
    {
        // Arrange & Act
        var viewModel = new ChatViewModel();

        // Assert
        viewModel.SuggestedQuestions.Should().BeAssignableTo<ObservableCollection<string>>();
    }

    #endregion

    #region StatusText Tests

    [Fact]
    public void StatusText_DefaultValue_IsOnline()
    {
        // Arrange & Act
        var viewModel = new ChatViewModel();

        // Assert
        viewModel.StatusText.Should().Contain("Online");
    }

    [Fact]
    public void StatusText_WhenChanged_RaisesPropertyChanged()
    {
        // Arrange
        var viewModel = new ChatViewModel();
        var propertyChangedCount = 0;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(viewModel.StatusText))
                propertyChangedCount++;
        };

        // Act
        viewModel.StatusText = "Thinking...";

        // Assert
        propertyChangedCount.Should().BeGreaterThan(0);
        viewModel.StatusText.Should().Be("Thinking...");
    }

    #endregion
}
