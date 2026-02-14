using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using FluentAssertions;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Email.Commands.AddEmailAccount;
using MIC.Core.Domain.Entities;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace MIC.Tests.Unit.Features.Email;

public class AddEmailAccountCommandHandlerTests
{
    private readonly AddEmailAccountCommandHandler _sut;
    private readonly IEmailAccountRepository _emailAccountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddEmailAccountCommandHandler> _logger;

    public AddEmailAccountCommandHandlerTests()
    {
        _emailAccountRepository = Substitute.For<IEmailAccountRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _logger = Substitute.For<ILogger<AddEmailAccountCommandHandler>>();
        _sut = new AddEmailAccountCommandHandler(_emailAccountRepository, _unitOfWork, _logger);
    }

    [Fact]
    public async Task Handle_WithNewGmailAccount_PersistsAccountWithTokens()
    {
        var command = CreateOAuthCommand();
        EmailAccount? persistedAccount = null;

        _emailAccountRepository
            .GetByUserIdAsync(command.UserId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<EmailAccount>>(Array.Empty<EmailAccount>()));

        _emailAccountRepository
            .AddAsync(Arg.Do<EmailAccount>(account => persistedAccount = account), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().NotBe(Guid.Empty);
        persistedAccount.Should().NotBeNull();
        persistedAccount!.EmailAddress.Should().Be(command.EmailAddress);
        persistedAccount.Provider.Should().Be(EmailProvider.Gmail);
        persistedAccount.AccessTokenEncrypted.Should().Be(command.AccessToken);
        persistedAccount.RefreshTokenEncrypted.Should().Be(command.RefreshToken);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _emailAccountRepository.Received(1).AddAsync(Arg.Any<EmailAccount>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAccountAlreadyExists_UpdatesTokensAndReturnsExistingId()
    {
        var command = CreateOAuthCommand() with
        {
            AccessToken = "new-token",
            RefreshToken = "new-refresh"
        };

        var existingAccount = new EmailAccount(
            emailAddress: command.EmailAddress,
            provider: EmailProvider.Gmail,
            userId: command.UserId);
        existingAccount.SetTokens("old-token", "old-refresh", DateTime.UtcNow.AddMinutes(-5));
        existingAccount.Deactivate();

        _emailAccountRepository
            .GetByUserIdAsync(command.UserId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<EmailAccount>>(new List<EmailAccount> { existingAccount }));

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().Be(existingAccount.Id);
        existingAccount.AccessTokenEncrypted.Should().Be("new-token");
        existingAccount.RefreshTokenEncrypted.Should().Be("new-refresh");
        existingAccount.IsActive.Should().BeTrue();
        await _emailAccountRepository.DidNotReceive().AddAsync(Arg.Any<EmailAccount>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithMissingEmail_ReturnsValidationError()
    {
        var command = CreateOAuthCommand() with { EmailAddress = string.Empty };

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Code.Should().Be("EmailAccount.Validation.EmailAddress");
        await _emailAccountRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_WithImapAccountMissingPassword_ReturnsValidationError()
    {
        var command = new AddEmailAccountCommand
        {
            UserId = Guid.NewGuid(),
            EmailAddress = "user@example.com",
            Provider = EmailProvider.IMAP.ToString(),
            ImapServer = "imap.example.com",
            ImapPort = 993,
            SmtpServer = "smtp.example.com",
            SmtpPort = 465,
            Password = null
        };

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("EmailAccount.Validation.Password");
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrows_ReturnsFailureError()
    {
        var command = CreateOAuthCommand();

        _emailAccountRepository
            .GetByUserIdAsync(command.UserId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<EmailAccount>>(Array.Empty<EmailAccount>()));

        _emailAccountRepository
            .AddAsync(Arg.Any<EmailAccount>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("db down"));

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("EmailAccount.AddFailed");
        result.FirstError.Type.Should().Be(ErrorType.Failure);
        result.FirstError.Description.Should().Contain("db down");
    }

    [Fact]
    public async Task Handle_WithEmptyUserId_ReturnsValidationError()
    {
        var command = CreateOAuthCommand() with { UserId = Guid.Empty };

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Code.Should().Contain("UserId");
    }

    [Fact]
    public async Task Handle_WithInvalidEmailFormat_ReturnsValidationError()
    {
        var command = CreateOAuthCommand() with { EmailAddress = "not-an-email" };

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Handle_WithOAuthMissingAccessToken_ReturnsValidationError()
    {
        var command = CreateOAuthCommand() with { AccessToken = null };

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Code.Should().Contain("AccessToken");
    }

    [Fact]
    public async Task Handle_WithNewImapAccount_PersistsAccountWithCredentials()
    {
        var command = new AddEmailAccountCommand
        {
            UserId = Guid.NewGuid(),
            EmailAddress = "user@custom.com",
            AccountName = "Custom IMAP",
            Provider = EmailProvider.IMAP.ToString(),
            ImapServer = "imap.custom.com",
            ImapPort = 993,
            SmtpServer = "smtp.custom.com",
            SmtpPort = 465,
            Password = "secret",
            UseSsl = true
        };

        EmailAccount? persisted = null;
        _emailAccountRepository
            .GetByUserIdAsync(command.UserId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<EmailAccount>>(Array.Empty<EmailAccount>()));
        _emailAccountRepository
            .AddAsync(Arg.Do<EmailAccount>(a => persisted = a), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        persisted.Should().NotBeNull();
        persisted!.Provider.Should().Be(EmailProvider.IMAP);
        await _emailAccountRepository.Received(1).AddAsync(Arg.Any<EmailAccount>(), Arg.Any<CancellationToken>());
    }

    private static AddEmailAccountCommand CreateOAuthCommand()
    {
        return new AddEmailAccountCommand
        {
            UserId = Guid.NewGuid(),
            EmailAddress = "user@example.com",
            AccountName = "User",
            Provider = EmailProvider.Gmail.ToString(),
            AccessToken = "token",
            RefreshToken = "refresh",
            ExpiresAt = DateTime.UtcNow.AddHours(2)
        };
    }
}
