using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using FluentAssertions;
using MIC.Core.Application.Chat.Commands.ClearChatSession;
using MIC.Core.Application.Chat.Commands.SaveChatInteraction;
using MIC.Core.Application.Chat.Queries.GetChatHistory;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace MIC.Tests.Unit.Application.Chat;

public class ChatHandlersExtendedTests
{
    #region SaveChatInteractionCommandHandler

    [Fact]
    public async Task SaveChat_ValidCommand_ReturnsGuid()
    {
        var repo = Substitute.For<IChatHistoryRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        var handler = new SaveChatInteractionCommandHandler(repo, uow);

        var cmd = new SaveChatInteractionCommand(
            UserId: Guid.NewGuid(),
            SessionId: "sess-1",
            Query: "Hello",
            Response: "Hi!");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().NotBe(Guid.Empty);
        await repo.Received(1).AddAsync(Arg.Any<ChatHistory>(), Arg.Any<CancellationToken>());
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveChat_WithOptionalFields_SetsAllProperties()
    {
        var repo = Substitute.For<IChatHistoryRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        ChatHistory? captured = null;
        await repo.AddAsync(Arg.Do<ChatHistory>(h => captured = h), Arg.Any<CancellationToken>());
        var handler = new SaveChatInteractionCommandHandler(repo, uow);

        var ts = DateTimeOffset.UtcNow.AddHours(-1);
        var cmd = new SaveChatInteractionCommand(
            UserId: Guid.NewGuid(),
            SessionId: "sess-1",
            Query: "Q",
            Response: "R",
            IsSuccessful: false,
            ErrorMessage: "err",
            AiProvider: "OpenAI",
            ModelUsed: "gpt-4",
            TokenCount: 500,
            Timestamp: ts,
            Context: "ctx",
            Metadata: "{\"k\":1}");

        await handler.Handle(cmd, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.IsSuccessful.Should().BeFalse();
        captured.ErrorMessage.Should().Be("err");
        captured.AIProvider.Should().Be("OpenAI");
        captured.ModelUsed.Should().Be("gpt-4");
        captured.TokenCount.Should().Be(500);
        captured.Timestamp.Should().Be(ts);
        captured.Context.Should().Be("ctx");
        captured.Metadata.Should().Be("{\"k\":1}");
    }

    [Fact]
    public async Task SaveChat_RepositoryThrows_ReturnsFailure()
    {
        var repo = Substitute.For<IChatHistoryRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        repo.AddAsync(Arg.Any<ChatHistory>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("db down"));
        var handler = new SaveChatInteractionCommandHandler(repo, uow);

        var cmd = new SaveChatInteractionCommand(
            UserId: Guid.NewGuid(), SessionId: "s", Query: "q", Response: "r");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("ChatHistory.SaveFailed");
    }

    #endregion

    #region GetChatHistoryQueryHandler

    [Fact]
    public async Task GetHistory_ValidSession_ReturnsMappedDtos()
    {
        var repo = Substitute.For<IChatHistoryRepository>();
        var userId = Guid.NewGuid();
        var entries = new List<ChatHistory>
        {
            new ChatHistory(userId, "sess", "Q1", "R1") { IsSuccessful = true },
            new ChatHistory(userId, "sess", "Q2", "R2") { IsSuccessful = false, ErrorMessage = "e" }
        };
        repo.GetBySessionAsync(userId, "sess", 100, Arg.Any<CancellationToken>())
            .Returns(entries);
        var handler = new GetChatHistoryQueryHandler(repo);

        var result = await handler.Handle(
            new GetChatHistoryQuery(userId, "sess", 100), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(2);
        result.Value[0].Query.Should().Be("Q1");
        result.Value[1].ErrorMessage.Should().Be("e");
    }

    [Fact]
    public async Task GetHistory_EmptySessionId_ReturnsValidationError()
    {
        var repo = Substitute.For<IChatHistoryRepository>();
        var handler = new GetChatHistoryQueryHandler(repo);

        var result = await handler.Handle(
            new GetChatHistoryQuery(Guid.NewGuid(), "  ", 10), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("ChatHistory.SessionIdRequired");
    }

    [Fact]
    public async Task GetHistory_NegativeLimit_DefaultsTo100()
    {
        var repo = Substitute.For<IChatHistoryRepository>();
        var userId = Guid.NewGuid();
        repo.GetBySessionAsync(userId, "s", 100, Arg.Any<CancellationToken>())
            .Returns(new List<ChatHistory>());
        var handler = new GetChatHistoryQueryHandler(repo);

        var result = await handler.Handle(
            new GetChatHistoryQuery(userId, "s", -5), CancellationToken.None);

        result.IsError.Should().BeFalse();
        await repo.Received(1).GetBySessionAsync(userId, "s", 100, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetHistory_LargeLimit_CappedAt500()
    {
        var repo = Substitute.For<IChatHistoryRepository>();
        var userId = Guid.NewGuid();
        repo.GetBySessionAsync(userId, "s", 500, Arg.Any<CancellationToken>())
            .Returns(new List<ChatHistory>());
        var handler = new GetChatHistoryQueryHandler(repo);

        var result = await handler.Handle(
            new GetChatHistoryQuery(userId, "s", 9999), CancellationToken.None);

        result.IsError.Should().BeFalse();
        await repo.Received(1).GetBySessionAsync(userId, "s", 500, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetHistory_RepositoryThrows_ReturnsFailure()
    {
        var repo = Substitute.For<IChatHistoryRepository>();
        repo.GetBySessionAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("network"));
        var handler = new GetChatHistoryQueryHandler(repo);

        var result = await handler.Handle(
            new GetChatHistoryQuery(Guid.NewGuid(), "s", 10), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("ChatHistory.LoadFailed");
    }

    #endregion

    #region ClearChatSessionCommandHandler

    [Fact]
    public async Task ClearChat_ValidSession_ReturnsTrue()
    {
        var repo = Substitute.For<IChatHistoryRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        var handler = new ClearChatSessionCommandHandler(repo, uow);

        var result = await handler.Handle(
            new ClearChatSessionCommand(Guid.NewGuid(), "sess-1"), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().BeTrue();
        await repo.Received(1).DeleteBySessionAsync(Arg.Any<Guid>(), "sess-1", Arg.Any<CancellationToken>());
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClearChat_EmptySessionId_ReturnsValidationError()
    {
        var repo = Substitute.For<IChatHistoryRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        var handler = new ClearChatSessionCommandHandler(repo, uow);

        var result = await handler.Handle(
            new ClearChatSessionCommand(Guid.NewGuid(), ""), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("ChatHistory.SessionIdRequired");
    }

    [Fact]
    public async Task ClearChat_WhitespaceSessionId_ReturnsValidationError()
    {
        var repo = Substitute.For<IChatHistoryRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        var handler = new ClearChatSessionCommandHandler(repo, uow);

        var result = await handler.Handle(
            new ClearChatSessionCommand(Guid.NewGuid(), "   "), CancellationToken.None);

        result.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task ClearChat_RepositoryThrows_ReturnsFailure()
    {
        var repo = Substitute.For<IChatHistoryRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        repo.DeleteBySessionAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("fail"));
        var handler = new ClearChatSessionCommandHandler(repo, uow);

        var result = await handler.Handle(
            new ClearChatSessionCommand(Guid.NewGuid(), "sess"), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("ChatHistory.ClearFailed");
    }

    #endregion

    #region ChatHistory Entity

    [Fact]
    public void ChatHistory_Constructor_WithParams_SetsProperties()
    {
        var userId = Guid.NewGuid();
        var entry = new ChatHistory(userId, "sess", "query", "response");

        entry.UserId.Should().Be(userId);
        entry.SessionId.Should().Be("sess");
        entry.Query.Should().Be("query");
        entry.Response.Should().Be("response");
        entry.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ChatHistory_Constructor_NullSessionId_Throws()
    {
        var act = () => new ChatHistory(Guid.NewGuid(), null!, "q", "r");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ChatHistory_Constructor_NullQuery_Throws()
    {
        var act = () => new ChatHistory(Guid.NewGuid(), "s", null!, "r");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ChatHistory_Constructor_NullResponse_Throws()
    {
        var act = () => new ChatHistory(Guid.NewGuid(), "s", "q", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ChatHistory_FullConstructor_SetsAIFields()
    {
        var entry = new ChatHistory(Guid.NewGuid(), "s", "q", "r", "OpenAI", "gpt-4", 123);
        entry.AIProvider.Should().Be("OpenAI");
        entry.ModelUsed.Should().Be("gpt-4");
        entry.TokenCount.Should().Be(123);
    }

    [Fact]
    public void ChatHistory_MarkAsFailed_SetsFields()
    {
        var entry = new ChatHistory(Guid.NewGuid(), "s", "q", "r");
        entry.MarkAsFailed("oops");

        entry.IsSuccessful.Should().BeFalse();
        entry.ErrorMessage.Should().Be("oops");
    }

    [Fact]
    public void ChatHistory_DefaultConstructor_HasDefaults()
    {
        var entry = new ChatHistory();
        entry.SessionId.Should().BeEmpty();
        entry.Query.Should().BeEmpty();
        entry.Response.Should().BeEmpty();
        entry.IsSuccessful.Should().BeTrue();
        entry.TokenCount.Should().Be(0);
    }

    #endregion
}
