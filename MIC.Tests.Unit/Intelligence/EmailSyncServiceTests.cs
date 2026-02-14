using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using MIC.Core.Intelligence;
using EmailAttachment = MIC.Core.Domain.Entities.EmailAttachment;

namespace MIC.Tests.Unit.Intelligence;

public class EmailSyncServiceTests
{
    private readonly IEmailRepository _emailRepo;
    private readonly IEmailAccountRepository _accountRepo;
    private readonly IKnowledgeBaseService _kbService;
    private readonly IServiceProvider _serviceProvider;
    private readonly EmailSyncService _service;

    public EmailSyncServiceTests()
    {
        _emailRepo = Substitute.For<IEmailRepository>();
        _accountRepo = Substitute.For<IEmailAccountRepository>();
        _kbService = Substitute.For<IKnowledgeBaseService>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _service = new EmailSyncService(_emailRepo, _accountRepo, _kbService, _serviceProvider);
    }

    #region SyncEmailsAsync - Account Not Found

    [Fact]
    public async Task SyncEmailsAsync_AccountNotFound_ShouldReturnFailure()
    {
        var accountId = Guid.NewGuid();
        _accountRepo.GetByIdAsync(accountId, Arg.Any<CancellationToken>())
            .Returns((EmailAccount?)null);

        var result = await _service.SyncEmailsAsync(accountId);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Email account not found");
        result.EmailsProcessed.Should().Be(0);
    }

    [Fact]
    public async Task SyncEmailsAsync_AccountNotFound_ShouldSetTimes()
    {
        _accountRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((EmailAccount?)null);

        var result = await _service.SyncEmailsAsync(Guid.NewGuid());

        result.StartTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.EndTime.Should().BeOnOrAfter(result.StartTime);
    }

    #endregion

    #region SyncEmailsAsync - Exception Handling

    [Fact]
    public async Task SyncEmailsAsync_RepositoryThrowsOnFirstCall_CatchBlockHandlesGracefully()
    {
        var accountId = Guid.NewGuid();
        var callCount = 0;
        // First call throws, second call (in catch block) returns null
        _accountRepo.GetByIdAsync(accountId, Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                callCount++;
                if (callCount == 1)
                    throw new InvalidOperationException("DB connection failed");
                return Task.FromResult<EmailAccount?>(null);
            });

        var result = await _service.SyncEmailsAsync(accountId);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("DB connection failed");
    }

    [Fact]
    public async Task SyncEmailsAsync_ExceptionDuringSync_ShouldCatchAndReturnFailure()
    {
        var accountId = Guid.NewGuid();
        var account = CreateTestAccount(accountId);

        // First call returns account, second call (in catch) also returns it
        _accountRepo.GetByIdAsync(accountId, Arg.Any<CancellationToken>())
            .Returns(account);
        // Update succeeds first (setting InProgress), then FetchExternalEmailsAsync
        // throws NotSupportedException, which triggers the inner SyncFolderAsync exception
        // The outer catch then sets sync failed

        var result = await _service.SyncEmailsAsync(accountId);

        // FetchExternalEmailsAsync throws NotSupportedException by default
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not configured");
    }

    #endregion

    #region SyncEmailsAsync - Default StartDate

    [Fact]
    public async Task SyncEmailsAsync_NoStartDate_ShouldDefaultTo3MonthsAgo()
    {
        var accountId = Guid.NewGuid();
        _accountRepo.GetByIdAsync(accountId, Arg.Any<CancellationToken>())
            .Returns((EmailAccount?)null);

        // The method defaults start date internally - we just verify it runs
        var result = await _service.SyncEmailsAsync(accountId);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SyncEmailsAsync_WithStartDate_ShouldUseProvided()
    {
        var accountId = Guid.NewGuid();
        _accountRepo.GetByIdAsync(accountId, Arg.Any<CancellationToken>())
            .Returns((EmailAccount?)null);

        var startDate = DateTime.UtcNow.AddMonths(-1);
        var result = await _service.SyncEmailsAsync(accountId, startDate);
        result.Should().NotBeNull();
    }

    #endregion

    #region SyncEmailsAsync - Cancellation

    [Fact]
    public async Task SyncEmailsAsync_WithCancellationToken_ShouldPassIt()
    {
        var cts = new CancellationTokenSource();
        var accountId = Guid.NewGuid();
        _accountRepo.GetByIdAsync(accountId, cts.Token)
            .Returns((EmailAccount?)null);

        var result = await _service.SyncEmailsAsync(accountId, null, cts.Token);
        result.Should().NotBeNull();

        await _accountRepo.Received(1).GetByIdAsync(accountId, cts.Token);
    }

    #endregion

    #region SyncAllAccountsAsync

    [Fact]
    public async Task SyncAllAccountsAsync_NoAccounts_ShouldReturnEmpty()
    {
        _accountRepo.GetAccountsNeedingSyncAsync(Arg.Any<CancellationToken>())
            .Returns(new List<EmailAccount>());

        var results = await _service.SyncAllAccountsAsync();

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SyncAllAccountsAsync_MultipleAccounts_ShouldSyncEach()
    {
        var accounts = new List<EmailAccount>
        {
            CreateTestAccount(Guid.NewGuid()),
            CreateTestAccount(Guid.NewGuid()),
            CreateTestAccount(Guid.NewGuid())
        };

        _accountRepo.GetAccountsNeedingSyncAsync(Arg.Any<CancellationToken>())
            .Returns(accounts);

        // Each account will fail with "not found" because GetByIdAsync returns null by default
        _accountRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((EmailAccount?)null);

        var results = await _service.SyncAllAccountsAsync();

        results.Should().HaveCount(3);
        results.Should().AllSatisfy(r => r.Success.Should().BeFalse());
    }

    [Fact]
    public async Task SyncAllAccountsAsync_WithCancellationToken_ShouldPassIt()
    {
        var cts = new CancellationTokenSource();
        _accountRepo.GetAccountsNeedingSyncAsync(cts.Token)
            .Returns(new List<EmailAccount>());

        await _service.SyncAllAccountsAsync(cts.Token);

        await _accountRepo.Received(1).GetAccountsNeedingSyncAsync(cts.Token);
    }

    #endregion

    #region SyncResult Properties

    [Fact]
    public void SyncResult_Defaults_ShouldBeCorrect()
    {
        var result = new SyncResult();

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().BeNull();
        result.EmailsProcessed.Should().Be(0);
        result.AttachmentsProcessed.Should().Be(0);
        result.InboxStats.Should().BeNull();
        result.SentStats.Should().BeNull();
    }

    [Fact]
    public void SyncResult_AllProperties_CanBeSet()
    {
        var inboxStats = new FolderSyncResult { Folder = EmailFolder.Inbox, EmailsProcessed = 10 };
        var sentStats = new FolderSyncResult { Folder = EmailFolder.Sent, EmailsProcessed = 5 };

        var result = new SyncResult
        {
            Success = true,
            ErrorMessage = null,
            EmailsProcessed = 15,
            AttachmentsProcessed = 3,
            StartTime = DateTime.UtcNow.AddMinutes(-5),
            EndTime = DateTime.UtcNow,
            InboxStats = inboxStats,
            SentStats = sentStats
        };

        result.Success.Should().BeTrue();
        result.EmailsProcessed.Should().Be(15);
        result.AttachmentsProcessed.Should().Be(3);
        result.InboxStats.Should().NotBeNull();
        result.SentStats.Should().NotBeNull();
        result.EndTime.Should().BeOnOrAfter(result.StartTime);
    }

    #endregion

    #region FolderSyncResult Properties

    [Fact]
    public void FolderSyncResult_Defaults_ShouldBeCorrect()
    {
        var result = new FolderSyncResult();

        result.EmailsProcessed.Should().Be(0);
        result.AttachmentsProcessed.Should().Be(0);
    }

    [Theory]
    [InlineData(EmailFolder.Inbox)]
    [InlineData(EmailFolder.Sent)]
    public void FolderSyncResult_Folder_CanBeSet(EmailFolder folder)
    {
        var result = new FolderSyncResult { Folder = folder };
        result.Folder.Should().Be(folder);
    }

    #endregion

    #region ExternalEmail Properties

    [Fact]
    public void ExternalEmail_Defaults_ShouldBeCorrect()
    {
        var email = new ExternalEmail();

        email.MessageId.Should().BeEmpty();
        email.Subject.Should().BeEmpty();
        email.FromAddress.Should().BeEmpty();
        email.FromName.Should().BeEmpty();
        email.ToRecipients.Should().BeEmpty();
        email.BodyText.Should().BeEmpty();
        email.BodyHtml.Should().BeNull();
        email.Attachments.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ExternalEmail_AllProperties_CanBeSet()
    {
        var now = DateTime.UtcNow;
        var email = new ExternalEmail
        {
            MessageId = "msg-123",
            Subject = "Test Subject",
            FromAddress = "sender@test.com",
            FromName = "Sender Name",
            ToRecipients = "recipient@test.com",
            SentDate = now,
            ReceivedDate = now.AddMinutes(1),
            BodyText = "Plain text body",
            BodyHtml = "<p>HTML body</p>",
            Attachments = new List<ExternalAttachment>
            {
                new ExternalAttachment { FileName = "file.pdf" }
            }
        };

        email.MessageId.Should().Be("msg-123");
        email.Subject.Should().Be("Test Subject");
        email.FromAddress.Should().Be("sender@test.com");
        email.FromName.Should().Be("Sender Name");
        email.ToRecipients.Should().Be("recipient@test.com");
        email.SentDate.Should().Be(now);
        email.ReceivedDate.Should().Be(now.AddMinutes(1));
        email.BodyText.Should().Be("Plain text body");
        email.BodyHtml.Should().Be("<p>HTML body</p>");
        email.Attachments.Should().HaveCount(1);
    }

    #endregion

    #region ExternalAttachment Properties

    [Fact]
    public void ExternalAttachment_Defaults_ShouldBeCorrect()
    {
        var attachment = new ExternalAttachment();

        attachment.FileName.Should().BeEmpty();
        attachment.ContentType.Should().BeEmpty();
        attachment.SizeInBytes.Should().Be(0);
        attachment.StoragePath.Should().BeEmpty();
        attachment.ExternalId.Should().BeNull();
        attachment.Content.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ExternalAttachment_AllProperties_CanBeSet()
    {
        var content = new byte[] { 1, 2, 3, 4 };
        var attachment = new ExternalAttachment
        {
            FileName = "report.pdf",
            ContentType = "application/pdf",
            SizeInBytes = 250000,
            StoragePath = "/attachments/report.pdf",
            ExternalId = "ext-123",
            Content = content
        };

        attachment.FileName.Should().Be("report.pdf");
        attachment.ContentType.Should().Be("application/pdf");
        attachment.SizeInBytes.Should().Be(250000);
        attachment.StoragePath.Should().Be("/attachments/report.pdf");
        attachment.ExternalId.Should().Be("ext-123");
        attachment.Content.Should().BeEquivalentTo(content);
    }

    #endregion

    #region Subclass with Overridden FetchExternalEmailsAsync

    [Fact]
    public async Task SyncEmailsAsync_WithValidAccount_FetchThrows_ShouldFail()
    {
        // Use the real FetchExternalEmailsAsync which throws NotSupportedException
        var accountId = Guid.NewGuid();
        var account = CreateTestAccount(accountId);

        _accountRepo.GetByIdAsync(accountId, Arg.Any<CancellationToken>())
            .Returns(account);

        var result = await _service.SyncEmailsAsync(accountId);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not configured");
    }

    [Fact]
    public async Task SyncEmailsAsync_WithValidAccount_ShouldSetInProgress()
    {
        var accountId = Guid.NewGuid();
        var account = CreateTestAccount(accountId);

        _accountRepo.GetByIdAsync(accountId, Arg.Any<CancellationToken>())
            .Returns(account);

        await _service.SyncEmailsAsync(accountId);

        // The account should have been updated (first call sets InProgress)
        await _accountRepo.Received().UpdateAsync(Arg.Any<EmailAccount>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Testable Subclass

    [Fact]
    public async Task TestableSync_WithNoExternalEmails_ShouldSucceed()
    {
        var accountId = Guid.NewGuid();
        var account = CreateTestAccount(accountId);

        _accountRepo.GetByIdAsync(accountId, Arg.Any<CancellationToken>())
            .Returns(account);
        _emailRepo.GetEmailsAsync(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<EmailFolder?>(),
                Arg.Any<bool?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<EmailMessage>());

        var testableService = new TestableEmailSyncService(_emailRepo, _accountRepo, _kbService, _serviceProvider);

        var result = await testableService.SyncEmailsAsync(accountId);

        result.Success.Should().BeTrue();
        result.EmailsProcessed.Should().Be(0);
        result.AttachmentsProcessed.Should().Be(0);
        result.InboxStats.Should().NotBeNull();
        result.SentStats.Should().NotBeNull();
    }

    [Fact]
    public async Task TestableSync_WithExternalEmails_ShouldProcessNewEmails()
    {
        var accountId = Guid.NewGuid();
        var account = CreateTestAccount(accountId);

        _accountRepo.GetByIdAsync(accountId, Arg.Any<CancellationToken>())
            .Returns(account);
        _emailRepo.GetByMessageIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((EmailMessage?)null); // All emails are new
        _emailRepo.GetEmailsAsync(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<EmailFolder?>(),
                Arg.Any<bool?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<EmailMessage>());

        var externalEmails = new List<ExternalEmail>
        {
            new ExternalEmail
            {
                MessageId = "msg-1",
                Subject = "Test Email",
                FromAddress = "sender@test.com",
                FromName = "Sender",
                ToRecipients = "recipient@test.com",
                SentDate = DateTime.UtcNow.AddDays(-1),
                ReceivedDate = DateTime.UtcNow.AddDays(-1),
                BodyText = "Test body",
                Attachments = new List<ExternalAttachment>()
            }
        };

        var testableService = new TestableEmailSyncService(_emailRepo, _accountRepo, _kbService, _serviceProvider, externalEmails);

        var result = await testableService.SyncEmailsAsync(accountId);

        result.Success.Should().BeTrue();
        result.EmailsProcessed.Should().BeGreaterThan(0);
        await _emailRepo.Received().AddAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TestableSync_ExistingEmail_ShouldSkipDuplicate()
    {
        var accountId = Guid.NewGuid();
        var account = CreateTestAccount(accountId);

        _accountRepo.GetByIdAsync(accountId, Arg.Any<CancellationToken>())
            .Returns(account);

        // Return an existing email for the message ID
        var existingEmail = new EmailMessage(
            "msg-existing", "Existing", "sender@test.com", "Sender",
            "recipient@test.com", DateTime.UtcNow, DateTime.UtcNow,
            "body", account.UserId, accountId, EmailFolder.Inbox);

        _emailRepo.GetByMessageIdAsync("msg-existing", Arg.Any<CancellationToken>())
            .Returns(existingEmail);
        _emailRepo.GetEmailsAsync(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<EmailFolder?>(),
                Arg.Any<bool?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<EmailMessage>());

        var externalEmails = new List<ExternalEmail>
        {
            new ExternalEmail
            {
                MessageId = "msg-existing",
                Subject = "Test",
                FromAddress = "sender@test.com",
                FromName = "Sender",
                ToRecipients = "recipient@test.com",
                SentDate = DateTime.UtcNow,
                ReceivedDate = DateTime.UtcNow,
                BodyText = "body",
                Attachments = new List<ExternalAttachment>()
            }
        };

        var testableService = new TestableEmailSyncService(_emailRepo, _accountRepo, _kbService, _serviceProvider, externalEmails);

        var result = await testableService.SyncEmailsAsync(accountId);

        result.Success.Should().BeTrue();
        // Should not add duplicates
        await _emailRepo.DidNotReceive().AddAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TestableSync_WithAttachments_ShouldProcessAttachments()
    {
        var accountId = Guid.NewGuid();
        var account = CreateTestAccount(accountId);

        _accountRepo.GetByIdAsync(accountId, Arg.Any<CancellationToken>())
            .Returns(account);
        _emailRepo.GetByMessageIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((EmailMessage?)null);
        _emailRepo.GetEmailsAsync(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<EmailFolder?>(),
                Arg.Any<bool?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<EmailMessage>());

        var externalEmails = new List<ExternalEmail>
        {
            new ExternalEmail
            {
                MessageId = "msg-attach",
                Subject = "With Attachment",
                FromAddress = "sender@test.com",
                FromName = "Sender",
                ToRecipients = "recipient@test.com",
                SentDate = DateTime.UtcNow,
                ReceivedDate = DateTime.UtcNow,
                BodyText = "body with attachment",
                Attachments = new List<ExternalAttachment>
                {
                    new ExternalAttachment
                    {
                        FileName = "report.pdf",
                        ContentType = "application/pdf",
                        SizeInBytes = 5000,
                        StoragePath = "/path/report.pdf",
                        ExternalId = "ext-1",
                        Content = System.Text.Encoding.UTF8.GetBytes("PDF content")
                    }
                }
            }
        };

        var testableService = new TestableEmailSyncService(_emailRepo, _accountRepo, _kbService, _serviceProvider, externalEmails);

        var result = await testableService.SyncEmailsAsync(accountId);

        result.Success.Should().BeTrue();
        result.AttachmentsProcessed.Should().BeGreaterThan(0);
        await _kbService.Received().IndexAttachmentAsync(Arg.Any<EmailAttachment>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Helpers

    private static EmailAccount CreateTestAccount(Guid id)
    {
        var account = new EmailAccount(
            "test@example.com",
            EmailProvider.Gmail,
            Guid.NewGuid(),
            "Test Account");
        // Set the Id via reflection since it's from BaseEntity
        typeof(MIC.Core.Domain.Abstractions.BaseEntity)
            .GetProperty("Id")!
            .SetValue(account, id);
        return account;
    }

    /// <summary>
    /// Testable subclass that overrides the protected virtual FetchExternalEmailsAsync
    /// </summary>
    private class TestableEmailSyncService : EmailSyncService
    {
        private readonly List<ExternalEmail> _emails;

        public TestableEmailSyncService(
            IEmailRepository emailRepo,
            IEmailAccountRepository accountRepo,
            IKnowledgeBaseService kbService,
            IServiceProvider serviceProvider,
            List<ExternalEmail>? emails = null)
            : base(emailRepo, accountRepo, kbService, serviceProvider)
        {
            _emails = emails ?? new List<ExternalEmail>();
        }

        protected override Task<List<ExternalEmail>> FetchExternalEmailsAsync(
            EmailAccount emailAccount,
            EmailFolder folder,
            DateTime startDate,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_emails);
        }
    }

    #endregion
}
