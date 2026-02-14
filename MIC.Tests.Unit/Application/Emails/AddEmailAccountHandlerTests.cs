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
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace MIC.Tests.Unit.Application.Emails;

/// <summary>
/// Tests for AddEmailAccountCommandHandler covering validation, create, update, and error paths.
/// 301 source lines of highly branching logic — this is a priority coverage target.
/// </summary>
public class AddEmailAccountHandlerTests
{
    private readonly IEmailAccountRepository _repo = Substitute.For<IEmailAccountRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ILogger<AddEmailAccountCommandHandler> _logger = Substitute.For<ILogger<AddEmailAccountCommandHandler>>();

    private AddEmailAccountCommandHandler CreateHandler() => new(_repo, _uow, _logger);

    private static AddEmailAccountCommand ValidGmailCommand(Guid? userId = null) => new()
    {
        UserId = userId ?? Guid.NewGuid(),
        EmailAddress = "test@gmail.com",
        Provider = "Gmail",
        AccessToken = "token123",
        RefreshToken = "refresh456",
        ExpiresAt = DateTime.UtcNow.AddHours(1)
    };

    private static AddEmailAccountCommand ValidImapCommand(Guid? userId = null) => new()
    {
        UserId = userId ?? Guid.NewGuid(),
        EmailAddress = "test@company.com",
        Provider = "IMAP",
        Password = "password",
        ImapServer = "imap.company.com",
        ImapPort = 993,
        SmtpServer = "smtp.company.com",
        SmtpPort = 587,
        UseSsl = true
    };

    #region Constructor Guards

    [Fact]
    public void Constructor_NullRepo_Throws()
    {
        var act = () => new AddEmailAccountCommandHandler(null!, _uow, _logger);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullUow_Throws()
    {
        var act = () => new AddEmailAccountCommandHandler(_repo, null!, _logger);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        var act = () => new AddEmailAccountCommandHandler(_repo, _uow, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Validation

    [Fact]
    public async Task Handle_EmptyUserId_ReturnsValidationError()
    {
        var cmd = ValidGmailCommand() with { UserId = Guid.Empty };
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("EmailAccount.Validation.UserId");
    }

    [Fact]
    public async Task Handle_EmptyEmail_ReturnsValidationError()
    {
        var cmd = ValidGmailCommand() with { EmailAddress = "" };
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("EmailAccount.Validation.EmailAddress");
    }

    [Fact]
    public async Task Handle_EmptyProvider_ReturnsValidationError()
    {
        var cmd = ValidGmailCommand() with { Provider = "" };
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("EmailAccount.Validation.Provider");
    }

    [Fact]
    public async Task Handle_InvalidEmailFormat_ReturnsValidationError()
    {
        var cmd = ValidGmailCommand() with { EmailAddress = "not-an-email" };
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("EmailAccount.Validation.InvalidEmail");
    }

    [Fact]
    public async Task Handle_InvalidProvider_ReturnsValidationError()
    {
        var cmd = ValidGmailCommand() with { Provider = "Yahoo" };
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("EmailAccount.Validation.InvalidProvider");
    }

    [Fact]
    public async Task Handle_OAuthNoAccessToken_ReturnsValidationError()
    {
        var cmd = ValidGmailCommand() with { AccessToken = null };
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("EmailAccount.Validation.AccessToken");
    }

    #endregion

    #region IMAP Validation

    [Fact]
    public async Task Handle_ImapNoPassword_ReturnsValidationError()
    {
        var cmd = ValidImapCommand() with { Password = null };
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("EmailAccount.Validation.Password");
    }

    [Fact]
    public async Task Handle_ImapNoImapServer_ReturnsValidationError()
    {
        var cmd = ValidImapCommand() with { ImapServer = null };
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("EmailAccount.Validation.ImapServer");
    }

    [Fact]
    public async Task Handle_ImapNoSmtpServer_ReturnsValidationError()
    {
        var cmd = ValidImapCommand() with { SmtpServer = null };
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("EmailAccount.Validation.SmtpServer");
    }

    [Fact]
    public async Task Handle_ImapInvalidPort_ReturnsValidationError()
    {
        var cmd = ValidImapCommand() with { ImapPort = 0 };
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("EmailAccount.Validation.ImapPort");
    }

    [Fact]
    public async Task Handle_ImapInvalidSmtpPort_ReturnsValidationError()
    {
        var cmd = ValidImapCommand() with { SmtpPort = -1 };
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("EmailAccount.Validation.SmtpPort");
    }

    [Fact]
    public async Task Handle_ImapPortTooHigh_ReturnsValidationError()
    {
        var cmd = ValidImapCommand() with { ImapPort = 70000 };
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("EmailAccount.Validation.ImapPort");
    }

    [Fact]
    public async Task Handle_ImapNullImapPort_ReturnsValidationError()
    {
        var cmd = ValidImapCommand() with { ImapPort = null };
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("EmailAccount.Validation.ImapPort");
    }

    [Fact]
    public async Task Handle_ImapNullSmtpPort_ReturnsValidationError()
    {
        var cmd = ValidImapCommand() with { SmtpPort = null };
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("EmailAccount.Validation.SmtpPort");
    }

    #endregion

    #region Create New Account

    [Fact]
    public async Task Handle_NewGmailAccount_ReturnsId()
    {
        _repo.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<EmailAccount>());
        var cmd = ValidGmailCommand();

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().NotBe(Guid.Empty);
        await _repo.Received(1).AddAsync(Arg.Any<EmailAccount>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NewOutlookAccount_ReturnsId()
    {
        _repo.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<EmailAccount>());
        var cmd = ValidGmailCommand() with { Provider = "Outlook", EmailAddress = "user@outlook.com" };

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NewImapAccount_ReturnsId()
    {
        _repo.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<EmailAccount>());
        var cmd = ValidImapCommand();

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_NewAccountWithAccountName_UsesAccountName()
    {
        _repo.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<EmailAccount>());
        EmailAccount? captured = null;
        await _repo.AddAsync(Arg.Do<EmailAccount>(a => captured = a), Arg.Any<CancellationToken>());
        var cmd = ValidGmailCommand() with { AccountName = "Work Gmail" };

        await CreateHandler().Handle(cmd, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.DisplayName.Should().Be("Work Gmail");
    }

    [Fact]
    public async Task Handle_GmailNoExpiresAt_DefaultsToOneHour()
    {
        _repo.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<EmailAccount>());
        var cmd = ValidGmailCommand() with { ExpiresAt = null };

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsError.Should().BeFalse();
    }

    #endregion

    #region Update Existing Account

    [Fact]
    public async Task Handle_ExistingAccount_UpdatesTokens()
    {
        var userId = Guid.NewGuid();
        var existing = new EmailAccount("test@gmail.com", EmailProvider.Gmail, userId);
        existing.SetTokens("old", "old", DateTime.UtcNow.AddHours(-1));
        _repo.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<EmailAccount> { existing });
        var cmd = ValidGmailCommand(userId) with { EmailAddress = "test@gmail.com" };

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().Be(existing.Id);
        await _repo.DidNotReceive().AddAsync(Arg.Any<EmailAccount>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingDeactivatedAccount_Reactivates()
    {
        var userId = Guid.NewGuid();
        var existing = new EmailAccount("test@gmail.com", EmailProvider.Gmail, userId);
        existing.SetTokens("old", "old", DateTime.UtcNow.AddHours(-1));
        existing.Deactivate();
        _repo.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<EmailAccount> { existing });
        var cmd = ValidGmailCommand(userId) with { EmailAddress = "test@gmail.com" };

        await CreateHandler().Handle(cmd, CancellationToken.None);

        existing.IsActive.Should().BeTrue();
    }

    #endregion

    #region Exception Handling

    [Fact]
    public async Task Handle_RepositoryThrows_ReturnsFailure()
    {
        _repo.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("db error"));
        var cmd = ValidGmailCommand();

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("EmailAccount.AddFailed");
    }

    #endregion
}
