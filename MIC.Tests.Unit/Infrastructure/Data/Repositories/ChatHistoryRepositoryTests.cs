using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.Data.Persistence;
using MIC.Infrastructure.Data.Repositories;
using Xunit;

namespace MIC.Tests.Unit.Infrastructure.Data.Repositories;

/// <summary>
/// Tests for ChatHistoryRepository covering chat session management.
/// Target: 8 tests for repository coverage
/// </summary>
public class ChatHistoryRepositoryTests : IDisposable
{
    private readonly MicDbContext _context;
    private readonly ChatHistoryRepository _repository;
    private readonly Guid _testUserId;

    public ChatHistoryRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<MicDbContext>()
            .UseInMemoryDatabase(databaseName: $"ChatHistoryTest_{Guid.NewGuid()}")
            .Options;

        _context = new MicDbContext(options);
        _repository = new ChatHistoryRepository(_context);
        _testUserId = Guid.NewGuid();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task AddAsync_ValidEntry_AddsToDatabase()
    {
        // Arrange
        var entry = CreateChatHistory("session1", "Hello", "Hi there");

        // Act
        await _repository.AddAsync(entry);
        await _context.SaveChangesAsync();

        // Assert
        var saved = await _context.ChatHistories.FirstOrDefaultAsync(x => x.Id == entry.Id);
        saved.Should().NotBeNull();
        saved!.Query.Should().Be("Hello");
        saved.Response.Should().Be("Hi there");
    }

    [Fact]
    public async Task GetBySessionAsync_WithMultipleMessages_ReturnsInChronologicalOrder()
    {
        // Arrange
        var sessionId = "session1";
        var entry1 = CreateChatHistory(sessionId, "First message", "First response", DateTimeOffset.UtcNow.AddMinutes(-10));
        var entry2 = CreateChatHistory(sessionId, "Second message", "Second response", DateTimeOffset.UtcNow.AddMinutes(-5));
        var entry3 = CreateChatHistory(sessionId, "Third message", "Third response", DateTimeOffset.UtcNow);

        await _repository.AddAsync(entry1);
        await _repository.AddAsync(entry2);
        await _repository.AddAsync(entry3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetBySessionAsync(_testUserId, sessionId, 10);

        // Assert
        result.Should().HaveCount(3);
        result[0].Query.Should().Be("First message");
        result[1].Query.Should().Be("Second message");
        result[2].Query.Should().Be("Third message");
    }

    [Fact]
    public async Task GetBySessionAsync_WithLimit_ReturnsOnlyRecentMessages()
    {
       // Arrange
        var sessionId = "session1";
        for (int i = 1; i <= 10; i++)
        {
            var entry = CreateChatHistory(sessionId, $"Message {i}", $"Response {i}", DateTimeOffset.UtcNow.AddMinutes(-10 + i));
            await _repository.AddAsync(entry);
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetBySessionAsync(_testUserId, sessionId, 5);

        // Assert
        result.Should().HaveCount(5);
        result[0].Query.Should().Be("Message 6");  // Most recent 5
        result[4].Query.Should().Be("Message 10");
    }

    [Fact]
    public async Task GetBySessionAsync_DifferentUsers_ReturnsOnlyUserMessages()
    {
        // Arrange
        var sessionId = "shared-session";
        var otherUserId = Guid.NewGuid();

        var userEntry = CreateChatHistory(sessionId, "User message", "Response");
        var otherUserEntry = CreateChatHistory(sessionId, "Other user message", "Response", DateTime.UtcNow, otherUserId);

        await _repository.AddAsync(userEntry);
        await _repository.AddAsync(otherUserEntry);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetBySessionAsync(_testUserId, sessionId, 10);

        // Assert
        result.Should().ContainSingle();
        result[0].Query.Should().Be("User message");
    }

    [Fact]
    public async Task GetBySessionAsync_DifferentSessions_ReturnsOnlySessionMessages()
    {
        // Arrange
        var entry1 = CreateChatHistory("session1", "Session 1 message", "Response 1");
        var entry2 = CreateChatHistory("session2", "Session 2 message", "Response 2");

        await _repository.AddAsync(entry1);
        await _repository.AddAsync(entry2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetBySessionAsync(_testUserId, "session1", 10);

        // Assert
        result.Should().ContainSingle();
        result[0].Query.Should().Be("Session 1 message");
    }

    [Fact]
    public async Task GetBySessionAsync_NoMessages_ReturnsEmptyList()
    {
        // Act
        var result = await _repository.GetBySessionAsync(_testUserId, "non-existent-session", 10);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteBySessionAsync_RemovesAllSessionMessages()
    {
        // Arrange
        var sessionId = "session-to-delete";
        var entry1 = CreateChatHistory(sessionId, "Message 1", "Response 1");
        var entry2 = CreateChatHistory(sessionId, "Message 2", "Response 2");
        var otherSessionEntry = CreateChatHistory("other-session", "Keep this", "Response");

        await _repository.AddAsync(entry1);
        await _repository.AddAsync(entry2);
        await _repository.AddAsync(otherSessionEntry);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteBySessionAsync(_testUserId, sessionId);
        await _context.SaveChangesAsync();

        // Assert
        var deletedSession = await _context.ChatHistories
            .Where(x => x.UserId == _testUserId && x.SessionId == sessionId)
            .ToListAsync();
        deletedSession.Should().BeEmpty();

        var otherSession = await _context.ChatHistories
            .Where(x => x.SessionId == "other-session")
            .ToListAsync();
        otherSession.Should().ContainSingle();
    }

    [Fact]
    public async Task DeleteBySessionAsync_OnlyDeletesUserMessages()
    {
        // Arrange
        var sessionId = "shared-session";
        var otherUserId = Guid.NewGuid();

        var userEntry = CreateChatHistory(sessionId, "Delete this", "Response");
        var otherUserEntry = CreateChatHistory(sessionId, "Keep this", "Response", DateTime.UtcNow, otherUserId);

        await _repository.AddAsync(userEntry);
        await _repository.AddAsync(otherUserEntry);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteBySessionAsync(_testUserId, sessionId);
        await _context.SaveChangesAsync();

        // Assert
        var remaining = await _context.ChatHistories.ToListAsync();
        remaining.Should().ContainSingle();
        remaining[0].UserId.Should().Be(otherUserId);
    }

    #region Helper Methods

    private ChatHistory CreateChatHistory(
        string sessionId,
        string query,
        string response,
        DateTimeOffset? timestamp = null,
        Guid? userId = null)
    {
        return new ChatHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId ?? _testUserId,
            SessionId = sessionId,
            Query = query ?? throw new ArgumentNullException(nameof(query)),
            Response = response ?? throw new ArgumentNullException(nameof(response)),
            Timestamp = timestamp ?? DateTimeOffset.UtcNow,
            ModelUsed = "gpt-4",
            TokenCount = 100
        };
    }

    #endregion
}
