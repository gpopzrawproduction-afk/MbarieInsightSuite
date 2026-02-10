using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using FluentAssertions;
using MIC.Core.Application.Chat.Commands.ClearChatSession;
using MIC.Core.Application.Chat.Commands.SaveChatInteraction;
using MIC.Core.Application.Chat.Queries.GetChatHistory;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using Moq;
using Xunit;

namespace MIC.Tests.Unit.Application.Chat;

/// <summary>
/// Comprehensive tests for Chat CQRS handlers.
/// Tests chat history persistence, session management, and retrieval.
/// Target: 15 tests for chat handler coverage
/// </summary>
public class ChatHandlersTests
{
    private readonly Mock<IChatHistoryRepository> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly string _testSessionId = "session_123";

    public ChatHandlersTests()
    {
        _mockRepository = new Mock<IChatHistoryRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
    }

    #region SaveChatInteractionCommandHandler Tests (6 tests)

    [Fact]
    public async Task SaveChatInteraction_WithValidData_SavesAndReturnsId()
    {
        // Arrange
        var command = new SaveChatInteractionCommand(
            UserId: _testUserId,
            SessionId: _testSessionId,
            Query: "What is the weather?",
            Response: "The weather is sunny.",
            Timestamp: DateTimeOffset.UtcNow,
            AiProvider: "OpenAI",
            ModelUsed: "gpt-4",
            TokenCount: 150,
            IsSuccessful: true);

        _mockRepository.Setup(x => x.AddAsync(It.IsAny<ChatHistory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new SaveChatInteractionCommandHandler(_mockRepository.Object, _mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBe(Guid.Empty);
        _mockRepository.Verify(x => x.AddAsync(It.IsAny<ChatHistory>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveChatInteraction_CreatesEntityWithCorrectProperties()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.AddMinutes(-5);
        var command = new SaveChatInteractionCommand(
            UserId: _testUserId,
            SessionId: _testSessionId,
            Query: "Test query",
            Response: "Test response",
            Timestamp: timestamp,
            AiProvider: "AzureOpenAI",
            ModelUsed: "gpt-4-turbo",
            TokenCount: 200,
            IsSuccessful: true,
            ErrorMessage: null,
            Context: "Email analysis",
            Metadata: "{\"confidence\": 0.95}");

        ChatHistory? capturedEntity = null;
        _mockRepository.Setup(x => x.AddAsync(It.IsAny<ChatHistory>(), It.IsAny<CancellationToken>()))
            .Callback<ChatHistory, CancellationToken>((entity, _) => capturedEntity = entity)
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new SaveChatInteractionCommandHandler(_mockRepository.Object, _mockUnitOfWork.Object);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        capturedEntity.Should().NotBeNull();
        capturedEntity!.UserId.Should().Be(_testUserId);
        capturedEntity.SessionId.Should().Be(_testSessionId);
        capturedEntity.Query.Should().Be("Test query");
        capturedEntity.Response.Should().Be("Test response");
        capturedEntity.Timestamp.Should().Be(timestamp);
        capturedEntity.AIProvider.Should().Be("AzureOpenAI");
        capturedEntity.ModelUsed.Should().Be("gpt-4-turbo");
        capturedEntity.TokenCount.Should().Be(200);
        capturedEntity.IsSuccessful.Should().BeTrue();
        capturedEntity.Context.Should().Be("Email analysis");
        capturedEntity.Metadata.Should().Be("{\"confidence\": 0.95}");
    }

    [Fact]
    public async Task SaveChatInteraction_WithNullTimestamp_UsesUtcNow()
    {
        // Arrange
        var command = new SaveChatInteractionCommand(
            UserId: _testUserId,
            SessionId: _testSessionId,
            Query: "Question",
            Response: "Answer",
            Timestamp: null);

        ChatHistory? capturedEntity = null;
        _mockRepository.Setup(x => x.AddAsync(It.IsAny<ChatHistory>(), It.IsAny<CancellationToken>()))
            .Callback<ChatHistory, CancellationToken>((entity, _) => capturedEntity = entity)
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new SaveChatInteractionCommandHandler(_mockRepository.Object, _mockUnitOfWork.Object);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        capturedEntity.Should().NotBeNull();
        capturedEntity!.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task SaveChatInteraction_WithFailedInteraction_PreservesErrorDetails()
    {
        // Arrange
        var command = new SaveChatInteractionCommand(
            UserId: _testUserId,
            SessionId: _testSessionId,
            Query: "Invalid query",
            Response: "",
            IsSuccessful: false,
            ErrorMessage: "API timeout");

        ChatHistory? capturedEntity = null;
        _mockRepository.Setup(x => x.AddAsync(It.IsAny<ChatHistory>(), It.IsAny<CancellationToken>()))
            .Callback<ChatHistory, CancellationToken>((entity, _) => capturedEntity = entity)
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new SaveChatInteractionCommandHandler(_mockRepository.Object, _mockUnitOfWork.Object);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        capturedEntity.Should().NotBeNull();
        capturedEntity!.IsSuccessful.Should().BeFalse();
        capturedEntity.ErrorMessage.Should().Be("API timeout");
    }

    [Fact]
    public async Task SaveChatInteraction_WithNullTokenCount_DefaultsToZero()
    {
        // Arrange
        var command = new SaveChatInteractionCommand(
            UserId: _testUserId,
            SessionId: _testSessionId,
            Query: "Query",
            Response: "Response",
            TokenCount: null);

        ChatHistory? capturedEntity = null;
        _mockRepository.Setup(x => x.AddAsync(It.IsAny<ChatHistory>(), It.IsAny<CancellationToken>()))
            .Callback<ChatHistory, CancellationToken>((entity, _) => capturedEntity = entity)
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new SaveChatInteractionCommandHandler(_mockRepository.Object, _mockUnitOfWork.Object);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        capturedEntity.Should().NotBeNull();
        capturedEntity!.TokenCount.Should().Be(0);
    }

    [Fact]
    public async Task SaveChatInteraction_WhenRepositoryThrows_ReturnsFailureError()
    {
        // Arrange
        var command = new SaveChatInteractionCommand(
            UserId: _testUserId,
            SessionId: _testSessionId,
            Query: "Query",
            Response: "Response");

        _mockRepository.Setup(x => x.AddAsync(It.IsAny<ChatHistory>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        var handler = new SaveChatInteractionCommandHandler(_mockRepository.Object, _mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Failure);
        result.FirstError.Code.Should().Be("ChatHistory.SaveFailed");
        result.FirstError.Description.Should().Contain("Database error");
    }

    #endregion

    #region ClearChatSessionCommandHandler Tests (4 tests)

    [Fact]
    public async Task ClearChatSession_WithValidSessionId_ReturnsTrue()
    {
        // Arrange
        var command = new ClearChatSessionCommand(_testUserId, _testSessionId);

        _mockRepository.Setup(x => x.DeleteBySessionAsync(_testUserId, _testSessionId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new ClearChatSessionCommandHandler(_mockRepository.Object, _mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeTrue();
        _mockRepository.Verify(x => x.DeleteBySessionAsync(_testUserId, _testSessionId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ClearChatSession_WithNullSessionId_ReturnsValidationError()
    {
        // Arrange
        var command = new ClearChatSessionCommand(_testUserId, null!);
        var handler = new ClearChatSessionCommandHandler(_mockRepository.Object, _mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Code.Should().Be("ChatHistory.SessionIdRequired");
        result.FirstError.Description.Should().Contain("SessionId is required");
        _mockRepository.Verify(x => x.DeleteBySessionAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ClearChatSession_WithEmptySessionId_ReturnsValidationError()
    {
        // Arrange
        var command = new ClearChatSessionCommand(_testUserId, "");
        var handler = new ClearChatSessionCommandHandler(_mockRepository.Object, _mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Code.Should().Be("ChatHistory.SessionIdRequired");
    }

    [Fact]
    public async Task ClearChatSession_WhenRepositoryThrows_ReturnsFailureError()
    {
        // Arrange
        var command = new ClearChatSessionCommand(_testUserId, _testSessionId);

        _mockRepository.Setup(x => x.DeleteBySessionAsync(_testUserId, _testSessionId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Delete failed"));

        var handler = new ClearChatSessionCommandHandler(_mockRepository.Object, _mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Failure);
        result.FirstError.Code.Should().Be("ChatHistory.ClearFailed");
        result.FirstError.Description.Should().Contain("Delete failed");
    }

    #endregion

    #region GetChatHistoryQueryHandler Tests (5 tests)

    [Fact]
    public async Task GetChatHistory_WithValidSessionId_ReturnsChatMessages()
    {
        // Arrange
        var query = new GetChatHistoryQuery(_testUserId, _testSessionId, 10);
        var chatEntries = new List<ChatHistory>
        {
            CreateChatHistory("What is AI?", "AI is artificial intelligence."),
            CreateChatHistory("Tell me more", "AI involves machine learning."),
            CreateChatHistory("Examples?", "ChatGPT, image recognition, etc.")
        };

        _mockRepository.Setup(x => x.GetBySessionAsync(_testUserId, _testSessionId, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatEntries);

        var handler = new GetChatHistoryQueryHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(3);
        result.Value[0].Query.Should().Be("What is AI?");
        result.Value[1].Query.Should().Be("Tell me more");
        result.Value[2].Query.Should().Be("Examples?");
    }

    [Fact]
    public async Task GetChatHistory_WithNullSessionId_ReturnsValidationError()
    {
        // Arrange
        var query = new GetChatHistoryQuery(_testUserId, null!, 10);
        var handler = new GetChatHistoryQueryHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Code.Should().Be("ChatHistory.SessionIdRequired");
        _mockRepository.Verify(x => x.GetBySessionAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetChatHistory_WithZeroLimit_Uses100AsDefault()
    {
        // Arrange
        var query = new GetChatHistoryQuery(_testUserId, _testSessionId, 0);
        _mockRepository.Setup(x => x.GetBySessionAsync(_testUserId, _testSessionId, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatHistory>());

        var handler = new GetChatHistoryQueryHandler(_mockRepository.Object);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        _mockRepository.Verify(x => x.GetBySessionAsync(_testUserId, _testSessionId, 100, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetChatHistory_WithLimitOver500_CapsAt500()
    {
        // Arrange
        var query = new GetChatHistoryQuery(_testUserId, _testSessionId, 1000);
        _mockRepository.Setup(x => x.GetBySessionAsync(_testUserId, _testSessionId, 500, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatHistory>());

        var handler = new GetChatHistoryQueryHandler(_mockRepository.Object);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        _mockRepository.Verify(x => x.GetBySessionAsync(_testUserId, _testSessionId, 500, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetChatHistory_WhenRepositoryThrows_ReturnsFailureError()
    {
        // Arrange
        var query = new GetChatHistoryQuery(_testUserId, _testSessionId, 10);
        _mockRepository.Setup(x => x.GetBySessionAsync(_testUserId, _testSessionId, 10, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Load failed"));

        var handler = new GetChatHistoryQueryHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Failure);
        result.FirstError.Code.Should().Be("ChatHistory.LoadFailed");
        result.FirstError.Description.Should().Contain("Load failed");
    }

    #endregion

    #region Helper Methods

    private ChatHistory CreateChatHistory(string query, string response, bool isSuccessful = true)
    {
        return new ChatHistory(_testUserId, _testSessionId, query, response)
        {
            IsSuccessful = isSuccessful,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    #endregion
}
