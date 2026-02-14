using System;
using FluentAssertions;
using MIC.Core.Domain.Entities;
using Xunit;

namespace MIC.Tests.Unit.Domain;

/// <summary>
/// Tests for ChatHistory entity covering constructors and MarkAsFailed.
/// </summary>
public class ChatHistoryTests
{
    [Fact]
    public void DefaultConstructor_InitializesDefaults()
    {
        var chat = new ChatHistory();

        chat.SessionId.Should().BeEmpty();
        chat.Query.Should().BeEmpty();
        chat.Response.Should().BeEmpty();
        chat.IsSuccessful.Should().BeTrue();
        chat.ErrorMessage.Should().BeNull();
        chat.TokenCount.Should().Be(0);
    }

    [Fact]
    public void ParameterizedConstructor_SetsRequiredProperties()
    {
        var userId = Guid.NewGuid();

        var chat = new ChatHistory(userId, "session-1", "What is AI?", "AI is...");

        chat.UserId.Should().Be(userId);
        chat.SessionId.Should().Be("session-1");
        chat.Query.Should().Be("What is AI?");
        chat.Response.Should().Be("AI is...");
        chat.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ParameterizedConstructor_ThrowsOnNullSessionId()
    {
        var act = () => new ChatHistory(Guid.NewGuid(), null!, "q", "r");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ParameterizedConstructor_ThrowsOnNullQuery()
    {
        var act = () => new ChatHistory(Guid.NewGuid(), "s", null!, "r");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ParameterizedConstructor_ThrowsOnNullResponse()
    {
        var act = () => new ChatHistory(Guid.NewGuid(), "s", "q", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FullConstructor_SetsAIProviderDetails()
    {
        var userId = Guid.NewGuid();

        var chat = new ChatHistory(userId, "s1", "query", "response", "OpenAI", "gpt-4", 150);

        chat.AIProvider.Should().Be("OpenAI");
        chat.ModelUsed.Should().Be("gpt-4");
        chat.TokenCount.Should().Be(150);
    }

    [Fact]
    public void MarkAsFailed_SetsIsSuccessfulToFalse()
    {
        var chat = new ChatHistory(Guid.NewGuid(), "s1", "query", "response");
        chat.IsSuccessful.Should().BeTrue();

        chat.MarkAsFailed("Timeout error");

        chat.IsSuccessful.Should().BeFalse();
        chat.ErrorMessage.Should().Be("Timeout error");
    }

    [Fact]
    public void Properties_CanBeSetDirectly()
    {
        var chat = new ChatHistory
        {
            Context = "Analyzing email #123",
            Cost = 0.05m,
            Metadata = "{\"tokens\": 200}"
        };

        chat.Context.Should().Be("Analyzing email #123");
        chat.Cost.Should().Be(0.05m);
        chat.Metadata.Should().Contain("tokens");
    }
}
