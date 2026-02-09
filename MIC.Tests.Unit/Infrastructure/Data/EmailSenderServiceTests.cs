using System.Reflection;
using FluentAssertions;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.Data.Services;
using MimeKit;
using Moq;

namespace MIC.Tests.Unit.Infrastructure.Data;

public class EmailSenderServiceTests
{
    private readonly Mock<IEmailAccountRepository> _accountRepository = new();
    private readonly Mock<IEmailRepository> _emailRepository = new();
    private readonly Mock<ILogger<EmailSenderService>> _logger = new();

    private EmailSenderService CreateService() =>
        new(_accountRepository.Object, _emailRepository.Object, _logger.Object);

    [Fact]
    public async Task SendEmailAsync_WhenAccountMissing_ReturnsFailure()
    {
        _accountRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailAccount?)null);

        var result = await CreateService().SendEmailAsync(
            Guid.NewGuid(),
            "recipient@example.com",
            "Subject",
            "Body");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task SendEmailAsync_WhenAccountInactive_ReturnsFailure()
    {
        var account = new EmailAccount("user@example.com", EmailProvider.Outlook, Guid.NewGuid());
        account.Deactivate();

        _accountRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var result = await CreateService().SendEmailAsync(
            Guid.NewGuid(),
            "recipient@example.com",
            "Subject",
            "Body");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not active");
    }

    [Fact]
    public async Task SendEmailAsync_WhenNoRecipientsProvided_ReturnsFailure()
    {
        var account = new EmailAccount("user@example.com", EmailProvider.Outlook, Guid.NewGuid());

        _accountRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var result = await CreateService().SendEmailAsync(
            Guid.NewGuid(),
            string.Empty,
            "Subject",
            "Body");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNull();
    }

    [Fact]
    public async Task ReplyToEmailAsync_WhenOriginalMissing_ReturnsFailure()
    {
        _emailRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailMessage?)null);

        var result = await CreateService().ReplyToEmailAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Thanks",
            replyToAll: false,
            isHtml: false);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task ForwardEmailAsync_WhenAccountInactive_ReturnsFailure()
    {
        var account = new EmailAccount("user@example.com", EmailProvider.Outlook, Guid.NewGuid());
        account.Deactivate();

        _accountRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var email = CreateEmailMessage(account.UserId, account.Id);
        _emailRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(email);

        var result = await CreateService().ForwardEmailAsync(
            account.Id,
            Guid.NewGuid(),
            "recipient@example.com");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not active");
    }

    [Fact]
    public void CreateMimeMessage_WithMultipleRecipients_ComposesProperMessage()
    {
        var account = new EmailAccount("sender@example.com", EmailProvider.Outlook, Guid.NewGuid(), "Sender");
        account.SetImapSmtpCredentials("imap.example.com", 993, "smtp.example.com", 587, true, "password");

        var service = CreateService();
        var method = typeof(EmailSenderService).GetMethod(
            "CreateMimeMessage",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        var message = (MimeMessage)method.Invoke(
            service,
            new object?[]
            {
                account,
                "user1@example.com; user2@example.com",
                "Greetings",
                "<p>Hello</p>",
                "cc@example.com",
                "bcc@example.com",
                true
            })!;

        message.Subject.Should().Be("Greetings");
        message.From.Mailboxes.Should().ContainSingle(m => m.Address == "sender@example.com");
        message.To.Mailboxes.Select(m => m.Address).Should().Contain(new[] { "user1@example.com", "user2@example.com" });
        message.Cc.Mailboxes.Should().ContainSingle(m => m.Address == "cc@example.com");
        message.Bcc.Mailboxes.Should().ContainSingle(m => m.Address == "bcc@example.com");
        message.HtmlBody.Should().Contain("<p>Hello</p>");
        message.TextBody.Should().Contain("Hello");
    }

    private static EmailMessage CreateEmailMessage(Guid userId, Guid accountId)
    {
        var email = new EmailMessage(
            messageId: Guid.NewGuid().ToString("N"),
            subject: "Original subject",
            fromAddress: "from@example.com",
            fromName: "From",
            toRecipients: "to@example.com",
            sentDate: DateTime.UtcNow.AddHours(-1),
            receivedDate: DateTime.UtcNow.AddMinutes(-30),
            bodyText: "Original body",
            userId: userId,
            emailAccountId: accountId);

        return email;
    }
}
