using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using MIC.Core.Application.Chat.Commands.ClearChatSession;
using MIC.Core.Application.Common.Interfaces;

namespace MIC.Tests.Unit.Application.Chat;

public class ClearChatSessionCommandHandlerTests
{
    private readonly Mock<IChatHistoryRepository> _chatHistoryRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    [Fact]
    public async Task Handle_WhenSessionIdMissing_ReturnsValidationError()
    {
        var handler = CreateHandler();
        var command = new ClearChatSessionCommand(Guid.NewGuid(), string.Empty);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("ChatHistory.SessionIdRequired");
        _chatHistoryRepository.Verify(repo => repo.DeleteBySessionAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRepositoryCompletes_ReturnsSuccess()
    {
        _chatHistoryRepository
            .Setup(repo => repo.DeleteBySessionAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWork
            .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        var command = new ClearChatSessionCommand(Guid.NewGuid(), "session-123");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().BeTrue();
        _chatHistoryRepository.Verify(repo => repo.DeleteBySessionAsync(command.UserId, command.SessionId, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrows_ReturnsFailureError()
    {
        _chatHistoryRepository
            .Setup(repo => repo.DeleteBySessionAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("delete failed"));

        var handler = CreateHandler();
        var command = new ClearChatSessionCommand(Guid.NewGuid(), "session-456");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("ChatHistory.ClearFailed");
        _unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private ClearChatSessionCommandHandler CreateHandler()
        => new(_chatHistoryRepository.Object, _unitOfWork.Object);
}
