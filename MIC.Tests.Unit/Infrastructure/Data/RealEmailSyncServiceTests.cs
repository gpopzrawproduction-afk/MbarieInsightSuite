using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.AI.Services;
using MIC.Infrastructure.Data.Services;
using Moq;

namespace MIC.Tests.Unit.Infrastructure.Data;

public class RealEmailSyncServiceTests
{
    private readonly Mock<IEmailRepository> _emailRepository = new();
    private readonly Mock<IEmailAccountRepository> _accountRepository = new();
    private readonly Mock<IEmailOAuth2Service> _oauth2Service = new();
    private readonly Mock<IEmailAnalysisService> _analysisService = new();
    private readonly Mock<IAttachmentStorageService> _attachmentStorage = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<RealEmailSyncService>> _logger = new();

    private RealEmailSyncService CreateService(IConfiguration? configuration = null)
    {
        configuration ??= new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EmailSync:InitialSyncMonths"] = "3"
            })
            .Build();

        return new RealEmailSyncService(
            _emailRepository.Object,
            _accountRepository.Object,
            _oauth2Service.Object,
            _analysisService.Object,
            _attachmentStorage.Object,
            _unitOfWork.Object,
            _logger.Object,
            configuration);
    }

    [Fact]
    public async Task SyncHistoricalEmailsAsync_WhenNoAccountsConfigured_ReturnsNoAccountStatus()
    {
        _accountRepository
            .Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailAccount>());

        var settings = new MIC.Core.Domain.Settings.EmailSyncSettings();
        var result = await CreateService().SyncHistoricalEmailsAsync(Guid.NewGuid(), settings);

        result.Status.Should().Be(IEmailSyncService.SyncStatus.NoAccountsConfigured);
        result.EmailsSynced.Should().Be(0);
    }

    [Fact]
    public async Task SyncHistoricalEmailsAsync_WhenCancelledReportsNoWork()
    {
        var account = new EmailAccount("user@example.com", EmailProvider.Outlook, Guid.NewGuid());
        _accountRepository
            .Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailAccount> { account });

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var settings = new MIC.Core.Domain.Settings.EmailSyncSettings();
        var result = await CreateService().SyncHistoricalEmailsAsync(
            Guid.NewGuid(),
            settings,
            progress: null,
            cancellationToken: cts.Token);

        result.Status.Should().Be(IEmailSyncService.SyncStatus.Completed);
        result.EmailsSynced.Should().Be(0);
    }

    [Fact]
    public async Task SyncHistoricalEmailsAsync_WhenRepositoryThrows_ReturnsFailure()
    {
        _accountRepository
            .Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db error"));

        var settings = new MIC.Core.Domain.Settings.EmailSyncSettings();
        var result = await CreateService().SyncHistoricalEmailsAsync(Guid.NewGuid(), settings);

        result.Status.Should().Be(IEmailSyncService.SyncStatus.Failed);
        result.Errors.Should().ContainSingle(e => e.Contains("db error"));
    }

    #region GetImapSettings Tests

    private (string host, int port) InvokeGetImapSettings(EmailAccount account)
    {
        var service = CreateService();
        var method = typeof(RealEmailSyncService).GetMethod("GetImapSettings",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        return ((string, int))method.Invoke(service, new object[] { account })!;
    }

    [Fact]
    public void GetImapSettings_Gmail_ReturnsGmailServer()
    {
        var account = new EmailAccount("user@gmail.com", EmailProvider.Gmail, Guid.NewGuid());
        var (host, port) = InvokeGetImapSettings(account);
        host.Should().Be("imap.gmail.com");
        port.Should().Be(993);
    }

    [Fact]
    public void GetImapSettings_Outlook_ReturnsOutlookServer()
    {
        var account = new EmailAccount("user@outlook.com", EmailProvider.Outlook, Guid.NewGuid());
        var (host, port) = InvokeGetImapSettings(account);
        host.Should().Be("outlook.office365.com");
        port.Should().Be(993);
    }

    [Fact]
    public void GetImapSettings_Exchange_ThrowsNotSupported()
    {
        var account = new EmailAccount("user@company.com", EmailProvider.Exchange, Guid.NewGuid());
        var act = () => InvokeGetImapSettings(account);
        act.Should().Throw<TargetInvocationException>()
            .WithInnerException<NotSupportedException>();
    }

    [Fact]
    public void GetImapSettings_Imap_WithConfiguredServer()
    {
        var account = new EmailAccount("user@custom.com", EmailProvider.IMAP, Guid.NewGuid());
        typeof(EmailAccount).GetProperty("ImapServer")!.SetValue(account, "imap.custom.com");
        typeof(EmailAccount).GetProperty("ImapPort")!.SetValue(account, 143);
        var (host, port) = InvokeGetImapSettings(account);
        host.Should().Be("imap.custom.com");
        port.Should().Be(143);
    }

    [Fact]
    public void GetImapSettings_Imap_EmptyServer_Throws()
    {
        var account = new EmailAccount("user@custom.com", EmailProvider.IMAP, Guid.NewGuid());
        var act = () => InvokeGetImapSettings(account);
        act.Should().Throw<TargetInvocationException>()
            .WithInnerException<InvalidOperationException>();
    }

    [Fact]
    public void GetImapSettings_Imap_ZeroPort_DefaultsTo993()
    {
        var account = new EmailAccount("user@custom.com", EmailProvider.IMAP, Guid.NewGuid());
        typeof(EmailAccount).GetProperty("ImapServer")!.SetValue(account, "imap.custom.com");
        typeof(EmailAccount).GetProperty("ImapPort")!.SetValue(account, 0);
        var (_, port) = InvokeGetImapSettings(account);
        port.Should().Be(993);
    }

    #endregion

    #region GetSecureSocketOptions Tests

    private MailKit.Security.SecureSocketOptions InvokeGetSecureSocketOptions(EmailAccount account)
    {
        var service = CreateService();
        var method = typeof(RealEmailSyncService).GetMethod("GetSecureSocketOptions",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (MailKit.Security.SecureSocketOptions)method.Invoke(service, new object[] { account })!;
    }

    [Fact]
    public void GetSecureSocketOptions_Imap_UseSslTrue_ReturnsSslOnConnect()
    {
        var account = new EmailAccount("user@custom.com", EmailProvider.IMAP, Guid.NewGuid());
        typeof(EmailAccount).GetProperty("UseSsl")!.SetValue(account, true);
        InvokeGetSecureSocketOptions(account).Should().Be(MailKit.Security.SecureSocketOptions.SslOnConnect);
    }

    [Fact]
    public void GetSecureSocketOptions_Imap_UseSslFalse_ReturnsNone()
    {
        var account = new EmailAccount("user@custom.com", EmailProvider.IMAP, Guid.NewGuid());
        typeof(EmailAccount).GetProperty("UseSsl")!.SetValue(account, false);
        InvokeGetSecureSocketOptions(account).Should().Be(MailKit.Security.SecureSocketOptions.None);
    }

    [Fact]
    public void GetSecureSocketOptions_Gmail_ReturnsSslOnConnect()
    {
        var account = new EmailAccount("user@gmail.com", EmailProvider.Gmail, Guid.NewGuid());
        InvokeGetSecureSocketOptions(account).Should().Be(MailKit.Security.SecureSocketOptions.SslOnConnect);
    }

    #endregion

    #region ConvertToEntity Tests

    private EmailMessage InvokeConvertToEntity(MimeKit.MimeMessage message, EmailAccount account, EmailFolder folder)
    {
        var service = CreateService();
        var method = typeof(RealEmailSyncService).GetMethod("ConvertToEntity",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (EmailMessage)method.Invoke(service, new object[] { message, account, folder })!;
    }

    [Fact]
    public void ConvertToEntity_BasicMessage_MapsFieldsCorrectly()
    {
        var account = new EmailAccount("user@test.com", EmailProvider.Gmail, Guid.NewGuid());
        var msg = new MimeKit.MimeMessage();
        msg.Subject = "Test Subject";
        msg.From.Add(new MimeKit.MailboxAddress("Sender Name", "sender@test.com"));
        msg.To.Add(new MimeKit.MailboxAddress("Recipient", "to@test.com"));
        msg.Date = new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.Zero);
        msg.Body = new MimeKit.TextPart("plain") { Text = "Hello body" };

        var entity = InvokeConvertToEntity(msg, account, EmailFolder.Inbox);

        entity.Subject.Should().Be("Test Subject");
        entity.FromAddress.Should().Be("sender@test.com");
        entity.BodyText.Should().Be("Hello body");
        entity.UserId.Should().Be(account.UserId);
        entity.EmailAccountId.Should().Be(account.Id);
    }

    [Fact]
    public void ConvertToEntity_NoSubject_DefaultsToEmpty()
    {
        var account = new EmailAccount("user@test.com", EmailProvider.Gmail, Guid.NewGuid());
        var msg = new MimeKit.MimeMessage();
        msg.From.Add(new MimeKit.MailboxAddress("Sender", "sender@test.com"));
        msg.To.Add(new MimeKit.MailboxAddress("Recipient", "to@test.com"));
        msg.Body = new MimeKit.TextPart("plain") { Text = "Body" };

        var entity = InvokeConvertToEntity(msg, account, EmailFolder.Inbox);

        // MimeKit Subject defaults to "" when no subject header is present
        entity.Subject.Should().NotBeNull();
    }

    [Fact]
    public void ConvertToEntity_NoFrom_UsesAccountEmail()
    {
        var account = new EmailAccount("fallback@test.com", EmailProvider.Gmail, Guid.NewGuid());
        var msg = new MimeKit.MimeMessage();
        msg.Subject = "Test";
        msg.To.Add(new MimeKit.MailboxAddress("Recipient", "to@test.com"));
        msg.Body = new MimeKit.TextPart("plain") { Text = "Body" };

        var entity = InvokeConvertToEntity(msg, account, EmailFolder.Inbox);

        entity.FromAddress.Should().Be("fallback@test.com");
    }

    [Fact]
    public void ConvertToEntity_EmptyMessageId_GeneratesOne()
    {
        var account = new EmailAccount("user@test.com", EmailProvider.Gmail, Guid.NewGuid());
        var msg = new MimeKit.MimeMessage();
        msg.Subject = "Test";
        msg.From.Add(new MimeKit.MailboxAddress("Sender", "s@test.com"));
        msg.To.Add(new MimeKit.MailboxAddress("R", "r@test.com"));
        msg.Body = new MimeKit.TextPart("plain") { Text = "Body" };

        var entity = InvokeConvertToEntity(msg, account, EmailFolder.Inbox);

        entity.MessageId.Should().NotBeNullOrWhiteSpace();
    }

    #endregion
}
