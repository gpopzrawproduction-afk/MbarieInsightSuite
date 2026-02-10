using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Email.Commands.AddEmailAccount;
using MIC.Core.Domain.Entities;
using Moq;
using Xunit;

namespace MIC.Tests.Unit.Application.Email;

/// <summary>
/// Focused tests for AddEmailAccountCommandHandler.
/// Tests email account validation for OAuth and IMAP providers.
/// Target: 8 tests for email account management coverage
/// </summary>
public class EmailHandlersTests
{
    private readonly Mock<IEmailAccountRepository> _mockAccountRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<AddEmailAccountCommandHandler>> _mockLogger;
    private readonly Guid _testUserId = Guid.NewGuid();

    public EmailHandlersTests()
    {
        _mockAccountRepository = new Mock<IEmailAccountRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<AddEmailAccountCommandHandler>>();
    }

    [Fact]
    public async Task AddEmailAccount_WithValidOAuthAccount_ReturnsAccountId()
    {
        // Arrange
        var command = new AddEmailAccountCommand
        {
            UserId = _testUserId,
            EmailAddress = "test@gmail.com",
            Provider = "Gmail",
            AccessToken = "oauth_token_123",
            RefreshToken = "refresh_token_456",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        _mockAccountRepository.Setup(x => x.GetByUserIdAsync(_testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailAccount>());
        _mockAccountRepository.Setup(x => x.AddAsync(It.IsAny<EmailAccount>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new AddEmailAccountCommandHandler(_mockAccountRepository.Object, _mockUnitOfWork.Object, _mockLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBe(Guid.Empty);
        _mockAccountRepository.Verify(x => x.AddAsync(It.IsAny<EmailAccount>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddEmailAccount_WithMissingOAuthToken_ReturnsValidationError()
    {
        // Arrange
        var command = new AddEmailAccountCommand
        {
            UserId = _testUserId,
            EmailAddress = "test@gmail.com",
            Provider = "Gmail",
            AccessToken = null // Missing token
        };

        var handler = new AddEmailAccountCommandHandler(_mockAccountRepository.Object, _mockUnitOfWork.Object, _mockLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Code.Should().Be("EmailAccount.Validation.AccessToken");
        result.FirstError.Description.Should().Contain("Access token is required");
    }

    [Fact]
    public async Task AddEmailAccount_WithValidIMAPAccount_ReturnsAccountId()
    {
        // Arrange
        var command = new AddEmailAccountCommand
        {
            UserId = _testUserId,
            EmailAddress = "test@company.com",
            Provider = "IMAP",
            ImapServer = "imap.company.com",
            ImapPort = 993,
            SmtpServer = "smtp.company.com",
            SmtpPort = 587,
            Password = "password123",
            UseSsl = true
        };

        _mockAccountRepository.Setup(x => x.GetByUserIdAsync(_testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailAccount>());
        _mockAccountRepository.Setup(x => x.AddAsync(It.IsAny<EmailAccount>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new AddEmailAccountCommandHandler(_mockAccountRepository.Object, _mockUnitOfWork.Object, _mockLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task AddEmailAccount_WithMissingIMAPServer_ReturnsValidationError()
    {
        // Arrange
        var command = new AddEmailAccountCommand
        {
            UserId = _testUserId,
            EmailAddress = "test@company.com",
            Provider = "IMAP",
            Password = "password123"
            // Missing ImapServer, SmtpServer, ports
        };

        var handler = new AddEmailAccountCommandHandler(_mockAccountRepository.Object, _mockUnitOfWork.Object, _mockLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("EmailAccount.Validation.ImapServer");
    }

    [Fact]
    public async Task AddEmailAccount_WithInvalidEmailFormat_ReturnsValidationError()
    {
        // Arrange
        var command = new AddEmailAccountCommand
        {
            UserId = _testUserId,
            EmailAddress = "invalid-email",
            Provider = "Gmail",
            AccessToken = "token"
        };

        var handler = new AddEmailAccountCommandHandler(_mockAccountRepository.Object, _mockUnitOfWork.Object, _mockLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("EmailAccount.Validation.InvalidEmail");
        result.FirstError.Description.Should().Contain("Invalid email address format");
    }

    [Fact]
    public async Task AddEmailAccount_WithDuplicateEmail_UpdatesExistingAccount()
    {
        // Arrange
        var existingAccount = new EmailAccount("test@gmail.com", EmailProvider.Gmail, _testUserId, "Test");
        var command = new AddEmailAccountCommand
        {
            UserId = _testUserId,
            EmailAddress = "test@gmail.com",
            Provider = "Gmail",
            AccessToken = "new_token",
            RefreshToken = "new_refresh"
        };

        _mockAccountRepository.Setup(x => x.GetByUserIdAsync(_testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailAccount> { existingAccount });
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new AddEmailAccountCommandHandler(_mockAccountRepository.Object, _mockUnitOfWork.Object, _mockLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(existingAccount.Id);
        _mockAccountRepository.Verify(x => x.AddAsync(It.IsAny<EmailAccount>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddEmailAccount_WithInvalidProvider_ReturnsValidationError()
    {
        // Arrange
        var command = new AddEmailAccountCommand
        {
            UserId = _testUserId,
            EmailAddress = "test@test.com",
            Provider = "InvalidProvider",
            AccessToken = "token"
        };

        var handler = new AddEmailAccountCommandHandler(_mockAccountRepository.Object, _mockUnitOfWork.Object, _mockLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("EmailAccount.Validation.InvalidProvider");
        result.FirstError.Description.Should().Contain("Invalid provider");
    }

    [Fact]
    public async Task AddEmailAccount_WithEmptyGuidUserId_ReturnsValidationError()
    {
        // Arrange
        var command = new AddEmailAccountCommand
        {
            UserId = Guid.Empty,
            EmailAddress = "test@gmail.com",
            Provider = "Gmail",
            AccessToken = "token"
        };

        var handler = new AddEmailAccountCommandHandler(_mockAccountRepository.Object, _mockUnitOfWork.Object, _mockLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("EmailAccount.Validation.UserId");
        result.FirstError.Description.Should().Contain("User ID is required");
    }
}
