using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using MIC.Core.Application.Chat.Queries.GetChatHistory;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;

namespace MIC.Tests.Unit.Application.Chat;

public class GetChatHistoryQueryHandlerTests
{
    private readonly Mock<IChatHistoryRepository> _chatHistoryRepository = new();
    private readonly GetChatHistoryQueryHandler _handler;

    public GetChatHistoryQueryHandlerTests()
    {
        _handler = new GetChatHistoryQueryHandler(_chatHistoryRepository.Object);
    }

    [Fact]
    public async Task Handle_WhenSessionIdMissing_ReturnsValidationError()
    {
        var query = new GetChatHistoryQuery(Guid.NewGuid(), string.Empty);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("ChatHistory.SessionIdRequired");
        _chatHistoryRepository.Verify(repo => repo.GetBySessionAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenLimitNotProvided_UsesDefaultOfOneHundred()
    {
        var userId = Guid.NewGuid();
        var query = new GetChatHistoryQuery(userId, "session", 0);
        _chatHistoryRepository
            .Setup(repo => repo.GetBySessionAsync(userId, "session", 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatHistory>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().BeEmpty();
        _chatHistoryRepository.Verify(repo => repo.GetBySessionAsync(userId, "session", 100, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenLimitExceedsMaximum_CapsAtFiveHundred()
    {
        var userId = Guid.NewGuid();
        var query = new GetChatHistoryQuery(userId, "session", 1000);
        _chatHistoryRepository
            .Setup(repo => repo.GetBySessionAsync(userId, "session", 500, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatHistory>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().BeEmpty();
        _chatHistoryRepository.Verify(repo => repo.GetBySessionAsync(userId, "session", 500, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRepositoryReturnsEntries_MapsToDtoList()
    {
        var userId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;
        var entries = new List<ChatHistory>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SessionId = "session",
                Query = "hello",
                Response = "world",
                Timestamp = timestamp,
                IsSuccessful = false,
                ErrorMessage = "oops"
            }
        };

        _chatHistoryRepository
            .Setup(repo => repo.GetBySessionAsync(userId, "session", 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);

        var result = await _handler.Handle(new GetChatHistoryQuery(userId, "session", 10), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Id = entries[0].Id,
            Query = "hello",
            Response = "world",
            Timestamp = timestamp,
            IsSuccessful = false,
            ErrorMessage = "oops"
        });
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrows_ReturnsFailureError()
    {
        var userId = Guid.NewGuid();
        _chatHistoryRepository
            .Setup(repo => repo.GetBySessionAsync(userId, "session", 100, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("broken"));

        var result = await _handler.Handle(new GetChatHistoryQuery(userId, "session"), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("ChatHistory.LoadFailed");
    }
}
