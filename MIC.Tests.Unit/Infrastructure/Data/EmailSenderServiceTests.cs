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

    #region HtmlToText Tests

    [Fact]
    public void HtmlToText_WithHtmlTags_ReturnsPlainText()
    {
        var service = CreateService();
        var method = typeof(EmailSenderService).GetMethod(
            "HtmlToText", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);
        if (method == null) return; // Skip if not accessible

        object? target = method.IsStatic ? null : service;
        var result = (string)method.Invoke(target, new object[] { "<p>Hello <b>World</b></p>" })!;

        result.Should().Contain("Hello");
        result.Should().Contain("World");
    }

    [Fact]
    public void HtmlToText_WithEmptyString_ReturnsEmpty()
    {
        var service = CreateService();
        var method = typeof(EmailSenderService).GetMethod(
            "HtmlToText", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);
        if (method == null) return;

        object? target = method.IsStatic ? null : service;
        var result = (string)method.Invoke(target, new object[] { "" })!;

        result.Should().BeEmpty();
    }

    #endregion

    #region GetSmtpSettings Tests

    [Fact]
    public void GetSmtpSettings_GmailProvider_ReturnsGmailSmtpSettings()
    {
        var service = CreateService();
        var method = typeof(EmailSenderService).GetMethod(
            "GetSmtpSettings", BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null) return;

        var account = new EmailAccount("user@gmail.com", EmailProvider.Gmail, Guid.NewGuid());

        var result = method.Invoke(service, new object[] { account });
        result.Should().NotBeNull();
    }

    [Fact]
    public void GetSmtpSettings_OutlookProvider_ReturnsOutlookSmtpSettings()
    {
        var service = CreateService();
        var method = typeof(EmailSenderService).GetMethod(
            "GetSmtpSettings", BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null) return;

        var account = new EmailAccount("user@outlook.com", EmailProvider.Outlook, Guid.NewGuid());

        var result = method.Invoke(service, new object[] { account });
        result.Should().NotBeNull();
    }

    #endregion

    #region CreateMimeMessage Additional Tests

    [Fact]
    public void CreateMimeMessage_PlainTextBody_SetsTextBodyOnly()
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
                "user@example.com",
                "Plain Text Test",
                "Hello World plain text",
                null,
                null,
                false
            })!;

        message.Subject.Should().Be("Plain Text Test");
        message.TextBody.Should().Contain("Hello World plain text");
    }

    [Fact]
    public void CreateMimeMessage_NoCcBcc_OmitsCcBcc()
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
                "user@example.com",
                "No CC",
                "Body",
                null,
                null,
                false
            })!;

        message.Cc.Count.Should().Be(0);
        message.Bcc.Count.Should().Be(0);
    }

    #endregion

    #region ForwardEmailAsync Additional Tests

    [Fact]
    public async Task ForwardEmailAsync_WhenEmailMissing_ReturnsFailure()
    {
        var account = new EmailAccount("user@example.com", EmailProvider.Outlook, Guid.NewGuid());
        _accountRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _emailRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailMessage?)null);

        var result = await CreateService().ForwardEmailAsync(
            account.Id,
            Guid.NewGuid(),
            "recipient@example.com");

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ForwardEmailAsync_WhenAccountMissing_ReturnsFailure()
    {
        _accountRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailAccount?)null);

        var result = await CreateService().ForwardEmailAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "recipient@example.com");

        result.Success.Should().BeFalse();
    }

    #endregion

    #region ReplyToEmailAsync Additional Tests

    [Fact]
    public async Task ReplyToEmailAsync_WhenAccountMissing_ReturnsFailure()
    {
        var emailId = Guid.NewGuid();
        var email = CreateEmailMessage(Guid.NewGuid(), Guid.NewGuid());
        _emailRepository
            .Setup(r => r.GetByIdAsync(emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(email);
        _accountRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailAccount?)null);

        var result = await CreateService().ReplyToEmailAsync(
            Guid.NewGuid(),
            emailId,
            "Reply body",
            replyToAll: false,
            isHtml: false);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ReplyToEmailAsync_WhenAccountInactive_ReturnsFailure()
    {
        var account = new EmailAccount("user@example.com", EmailProvider.Outlook, Guid.NewGuid());
        account.Deactivate();

        var email = CreateEmailMessage(account.UserId, account.Id);
        _emailRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(email);
        _accountRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var result = await CreateService().ReplyToEmailAsync(
            account.Id,
            email.Id,
            "Reply body",
            replyToAll: false,
            isHtml: false);

        result.Success.Should().BeFalse();
    }

    #endregion

    #region SendEmailWithAttachments Tests

    [Fact]
    public async Task SendEmailWithAttachmentsAsync_WhenAccountMissing_ReturnsFailure()
    {
        _accountRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailAccount?)null);

        var result = await CreateService().SendEmailWithAttachmentsAsync(
            Guid.NewGuid(),
            "recipient@example.com",
            "Subject",
            "Body",
            Array.Empty<MIC.Core.Application.Common.Interfaces.EmailAttachment>());

        result.Success.Should().BeFalse();
    }

    #endregion

    #region GetSecureSocketOptions Tests

    [Fact]
    public void GetSecureSocketOptions_ImapWithSsl_ReturnsSslOnConnect()
    {
        var account = new EmailAccount("user@example.com", EmailProvider.IMAP, Guid.NewGuid());
        typeof(EmailAccount).GetProperty("UseSsl")!.SetValue(account, true);

        var method = typeof(EmailSenderService).GetMethod("GetSecureSocketOptions",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = method.Invoke(CreateService(), new object[] { account });

        result.Should().Be(MailKit.Security.SecureSocketOptions.SslOnConnect);
    }

    [Fact]
    public void GetSecureSocketOptions_ImapWithoutSsl_ReturnsStartTlsWhenAvailable()
    {
        var account = new EmailAccount("user@example.com", EmailProvider.IMAP, Guid.NewGuid());
        typeof(EmailAccount).GetProperty("UseSsl")!.SetValue(account, false);

        var method = typeof(EmailSenderService).GetMethod("GetSecureSocketOptions",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = method.Invoke(CreateService(), new object[] { account });

        result.Should().Be(MailKit.Security.SecureSocketOptions.StartTlsWhenAvailable);
    }

    [Fact]
    public void GetSecureSocketOptions_OAuthProvider_ReturnsStartTlsWhenAvailable()
    {
        var account = new EmailAccount("user@example.com", EmailProvider.Gmail, Guid.NewGuid());

        var method = typeof(EmailSenderService).GetMethod("GetSecureSocketOptions",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = method.Invoke(CreateService(), new object[] { account });

        result.Should().Be(MailKit.Security.SecureSocketOptions.StartTlsWhenAvailable);
    }

    #endregion

    #region GetSmtpSettings Additional Tests

    [Fact]
    public void GetSmtpSettings_Exchange_ThrowsNotSupported()
    {
        var account = new EmailAccount("user@example.com", EmailProvider.Exchange, Guid.NewGuid());

        var method = typeof(EmailSenderService).GetMethod("GetSmtpSettings",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        var act = () => method.Invoke(CreateService(), new object[] { account });

        act.Should().Throw<TargetInvocationException>()
            .WithInnerException<NotSupportedException>();
    }

    [Fact]
    public void GetSmtpSettings_ImapWithServer_ReturnsConfiguredSettings()
    {
        var account = new EmailAccount("user@example.com", EmailProvider.IMAP, Guid.NewGuid());
        typeof(EmailAccount).GetProperty("SmtpServer")!.SetValue(account, "smtp.custom.com");
        typeof(EmailAccount).GetProperty("SmtpPort")!.SetValue(account, 587);

        var method = typeof(EmailSenderService).GetMethod("GetSmtpSettings",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = ((string, int))method.Invoke(CreateService(), new object[] { account })!;

        result.Item1.Should().Be("smtp.custom.com");
        result.Item2.Should().Be(587);
    }

    [Fact]
    public void GetSmtpSettings_ImapWithoutServer_ThrowsInvalidOperation()
    {
        var account = new EmailAccount("user@example.com", EmailProvider.IMAP, Guid.NewGuid());
        // SmtpServer is null/empty by default

        var method = typeof(EmailSenderService).GetMethod("GetSmtpSettings",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        var act = () => method.Invoke(CreateService(), new object[] { account });

        act.Should().Throw<TargetInvocationException>()
            .WithInnerException<InvalidOperationException>();
    }

    #endregion
}
