using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using MIC.Core.Intelligence;
using Moq;
using DomainEmailAttachment = MIC.Core.Domain.Entities.EmailAttachment;
using ExternalAttachment = MIC.Core.Intelligence.ExternalAttachment;
using ExternalEmail = MIC.Core.Intelligence.ExternalEmail;

namespace MIC.Tests.Unit.Core.Intelligence;

public sealed class EmailSyncServiceTests
{
    [Fact]
    public async Task SyncEmailsAsync_WhenAccountMissing_ReturnsFailure()
    {
        var emailRepository = new FakeEmailRepository();
        var accountRepository = new FakeEmailAccountRepository();
        var knowledgeBase = new FakeKnowledgeBaseService();
        var serviceProvider = Mock.Of<IServiceProvider>();
        var sut = CreateService(emailRepository, accountRepository, knowledgeBase, serviceProvider, _ => new List<ExternalEmail>());

        var result = await sut.SyncEmailsAsync(Guid.NewGuid());

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Email account not found");
    }

    [Fact]
    public async Task SyncEmailsAsync_WhenExternalEmailsFetched_PersistsEmailsAndAttachments()
    {
        var userId = Guid.NewGuid();
        var account = new EmailAccount("owner@example.com", EmailProvider.Outlook, userId) { Id = Guid.NewGuid() };
        var emailRepository = new FakeEmailRepository();
        var accountRepository = new FakeEmailAccountRepository(account);
        var knowledgeBase = new FakeKnowledgeBaseService();
        var now = DateTime.UtcNow;

        var inboxEmails = new List<ExternalEmail>
        {
            new()
            {
                MessageId = "inbox-1",
                Subject = "Action Required",
                FromAddress = "sender@example.com",
                FromName = "Sender",
                ToRecipients = "owner@example.com",
                SentDate = now.AddHours(-2),
                ReceivedDate = now.AddHours(-2),
                BodyText = "Please confirm the meeting.",
                Attachments =
                {
                    new ExternalAttachment
                    {
                        FileName = "report.pdf",
                        ContentType = "application/pdf",
                        SizeInBytes = 1_024,
                        StoragePath = "attachments/report.pdf",
                        ExternalId = "att-1",
                        Content = new byte[] { 1, 2, 3 }
                    }
                }
            }
        };

        var sentEmails = new List<ExternalEmail>
        {
            new()
            {
                MessageId = "sent-1",
                Subject = "Project Update",
                FromAddress = "owner@example.com",
                FromName = "Owner",
                ToRecipients = "recipient@example.com",
                SentDate = now.AddHours(-1),
                ReceivedDate = now.AddHours(-1),
                BodyText = "Completed the milestone."
            }
        };

        var sut = CreateService(
            emailRepository,
            accountRepository,
            knowledgeBase,
            Mock.Of<IServiceProvider>(),
            request => request.folder switch
            {
                EmailFolder.Inbox => inboxEmails,
                EmailFolder.Sent => sentEmails,
                _ => new List<ExternalEmail>()
            });

        var result = await sut.SyncEmailsAsync(account.Id, now.AddDays(-1));

        result.Success.Should().BeTrue();
        result.EmailsProcessed.Should().Be(2);
        result.AttachmentsProcessed.Should().Be(1);
        result.InboxStats.Should().NotBeNull();
        result.InboxStats!.EmailsProcessed.Should().Be(1);
        result.SentStats.Should().NotBeNull();
        result.SentStats!.EmailsProcessed.Should().Be(1);

        emailRepository.Emails.Should().HaveCount(2);
        emailRepository.Emails.Should().OnlyContain(email => email.UserId == userId && email.IsAIProcessed);
        emailRepository.Emails.First(e => e.MessageId == "inbox-1").Attachments.Should().HaveCount(1);
        knowledgeBase.ProcessedAttachments.Should().HaveCount(1);

        var persistedAccount = await accountRepository.GetByIdAsync(account.Id, CancellationToken.None);
        persistedAccount.Should().NotBeNull();
        persistedAccount!.Status.Should().Be(SyncStatus.Completed);
        persistedAccount.TotalEmailsSynced.Should().BeGreaterThan(0);
        persistedAccount.TotalAttachmentsSynced.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SyncEmailsAsync_WhenProviderThrows_SetsAccountStatusToFailed()
    {
        var userId = Guid.NewGuid();
        var account = new EmailAccount("owner@example.com", EmailProvider.Outlook, userId) { Id = Guid.NewGuid() };
        var emailRepository = new FakeEmailRepository();
        var accountRepository = new FakeEmailAccountRepository(account);
        var knowledgeBase = new FakeKnowledgeBaseService();

        var sut = CreateService(
            emailRepository,
            accountRepository,
            knowledgeBase,
            Mock.Of<IServiceProvider>(),
            _ => throw new InvalidOperationException("Provider offline"));

        var result = await sut.SyncEmailsAsync(account.Id, DateTime.UtcNow.AddDays(-1));

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Error syncing Inbox folder");

        var persistedAccount = await accountRepository.GetByIdAsync(account.Id, CancellationToken.None);
        persistedAccount.Should().NotBeNull();
        persistedAccount!.Status.Should().Be(SyncStatus.Failed);
        persistedAccount.LastSyncError.Should().Contain("Provider offline");
    }

    private static TestEmailSyncService CreateService(
        IEmailRepository emailRepository,
        IEmailAccountRepository accountRepository,
        IKnowledgeBaseService knowledgeBase,
        IServiceProvider serviceProvider,
        Func<(EmailAccount account, EmailFolder folder, DateTime startDate, CancellationToken cancellationToken), IEnumerable<ExternalEmail>> fetch)
    {
        return new TestEmailSyncService(emailRepository, accountRepository, knowledgeBase, serviceProvider, fetch);
    }

    private sealed class TestEmailSyncService : EmailSyncService
    {
        private readonly Func<(EmailAccount account, EmailFolder folder, DateTime startDate, CancellationToken cancellationToken), IEnumerable<ExternalEmail>> _fetch;

        public TestEmailSyncService(
            IEmailRepository emailRepository,
            IEmailAccountRepository emailAccountRepository,
            IKnowledgeBaseService knowledgeBaseService,
            IServiceProvider serviceProvider,
            Func<(EmailAccount account, EmailFolder folder, DateTime startDate, CancellationToken cancellationToken), IEnumerable<ExternalEmail>> fetch)
            : base(emailRepository, emailAccountRepository, knowledgeBaseService, serviceProvider)
        {
            _fetch = fetch;
        }

        protected override Task<List<ExternalEmail>> FetchExternalEmailsAsync(
            EmailAccount emailAccount,
            EmailFolder folder,
            DateTime startDate,
            CancellationToken cancellationToken)
        {
            var results = _fetch((emailAccount, folder, startDate, cancellationToken)).ToList();
            return Task.FromResult(results);
        }
    }

    private sealed class FakeEmailRepository : IEmailRepository
    {
        private readonly ConcurrentDictionary<Guid, EmailMessage> _store = new();

        public List<EmailMessage> Emails => _store.Values.ToList();

        public Task AddAsync(EmailMessage entity, CancellationToken cancellationToken = default)
        {
            _store[entity.Id] = entity;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(EmailMessage entity, CancellationToken cancellationToken = default)
        {
            _store.TryRemove(entity.Id, out _);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<EmailMessage>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IEnumerable<EmailMessage>>(_store.Values.ToList());
        }

        public Task<IReadOnlyList<EmailMessage>> GetConversationAsync(string conversationId, CancellationToken cancellationToken = default)
        {
            var results = _store.Values.Where(email => email.ConversationId == conversationId).ToList();
            return Task.FromResult<IReadOnlyList<EmailMessage>>(results);
        }

        public Task<EmailMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _store.TryGetValue(id, out var entity);
            return Task.FromResult(entity);
        }

        public Task<EmailMessage?> GetByMessageIdAsync(string messageId, CancellationToken cancellationToken = default)
        {
            var entity = _store.Values.FirstOrDefault(email => email.MessageId == messageId);
            return Task.FromResult(entity);
        }

        public Task<IReadOnlyList<EmailMessage>> GetEmailsAsync(
            Guid userId,
            Guid? emailAccountId = null,
            EmailFolder? folder = null,
            bool? isUnread = null,
            int skip = 0,
            int take = 50,
            CancellationToken cancellationToken = default)
        {
            var query = _store.Values.Where(email => email.UserId == userId);

            if (emailAccountId.HasValue)
            {
                query = query.Where(email => email.EmailAccountId == emailAccountId.Value);
            }

            if (folder.HasValue)
            {
                query = query.Where(email => email.Folder == folder.Value);
            }

            if (isUnread.HasValue)
            {
                query = query.Where(email => isUnread.Value ? !email.IsRead : email.IsRead);
            }

            var results = query
                .Skip(skip)
                .Take(take)
                .ToList();

            return Task.FromResult<IReadOnlyList<EmailMessage>>(results);
        }

        public Task<int> GetRequiresResponseCountAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var count = _store.Values.Count(email => email.UserId == userId && email.RequiresResponse);
            return Task.FromResult(count);
        }

        public Task<int> GetUnreadCountAsync(Guid userId, Guid? emailAccountId = null, CancellationToken cancellationToken = default)
        {
            var query = _store.Values.Where(email => email.UserId == userId && !email.IsRead);

            if (emailAccountId.HasValue)
            {
                query = query.Where(email => email.EmailAccountId == emailAccountId.Value);
            }

            return Task.FromResult(query.Count());
        }

        public Task<bool> ExistsAsync(string messageId, CancellationToken cancellationToken = default)
        {
            var exists = _store.Values.Any(email => email.MessageId == messageId);
            return Task.FromResult(exists);
        }

        public Task MarkAsReadAsync(IEnumerable<Guid> emailIds, CancellationToken cancellationToken = default)
        {
            foreach (var id in emailIds)
            {
                if (_store.TryGetValue(id, out var email))
                {
                    email.MarkAsRead();
                }
            }

            return Task.CompletedTask;
        }

        public Task UpdateAsync(EmailMessage entity, CancellationToken cancellationToken = default)
        {
            _store[entity.Id] = entity;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeEmailAccountRepository : IEmailAccountRepository
    {
        private readonly ConcurrentDictionary<Guid, EmailAccount> _accounts = new();

        public FakeEmailAccountRepository(params EmailAccount[] accounts)
        {
            foreach (var account in accounts)
            {
                _accounts[account.Id] = account;
            }
        }

        public Task AddAsync(EmailAccount entity, CancellationToken cancellationToken = default)
        {
            _accounts[entity.Id] = entity;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(EmailAccount entity, CancellationToken cancellationToken = default)
        {
            _accounts.TryRemove(entity.Id, out _);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<EmailAccount>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IEnumerable<EmailAccount>>(_accounts.Values.ToList());
        }

        public Task<IReadOnlyList<EmailAccount>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var results = _accounts.Values.Where(account => account.UserId == userId).ToList();
            return Task.FromResult<IReadOnlyList<EmailAccount>>(results);
        }

        public Task<EmailAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _accounts.TryGetValue(id, out var account);
            return Task.FromResult(account);
        }

        public Task<IReadOnlyList<EmailAccount>> GetAccountsNeedingSyncAsync(CancellationToken cancellationToken = default)
        {
            var results = _accounts.Values.Where(account => account.IsActive).ToList();
            return Task.FromResult<IReadOnlyList<EmailAccount>>(results);
        }

        public Task<bool> IsEmailConnectedAsync(string emailAddress, Guid userId, CancellationToken cancellationToken = default)
        {
            var exists = _accounts.Values.Any(account =>
                account.UserId == userId &&
                string.Equals(account.EmailAddress, emailAddress, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(exists);
        }

        public Task<EmailAccount?> GetPrimaryAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var account = _accounts.Values.FirstOrDefault(a => a.UserId == userId && a.IsPrimary);
            return Task.FromResult(account);
        }

        public Task UpdateAsync(EmailAccount entity, CancellationToken cancellationToken = default)
        {
            _accounts[entity.Id] = entity;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeKnowledgeBaseService : IKnowledgeBaseService
    {
        public List<DomainEmailAttachment> ProcessedAttachments { get; } = new();

        public Task IndexAttachmentAsync(DomainEmailAttachment attachment, CancellationToken cancellationToken = default)
        {
            ProcessedAttachments.Add(attachment);
            return Task.CompletedTask;
        }

        public Task<KnowledgeEntry> CreateEntryAsync(KnowledgeEntry entry, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task DeleteEntryAsync(Guid entryId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<List<KnowledgeEntry>> GetRelatedEntriesAsync(string topic, Guid userId, int limit = 10, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task IndexEmailAsync(EmailMessage emailMessage, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<List<KnowledgeEntry>> SearchAsync(string query, Guid userId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task UpdateEntryAsync(Guid entryId, KnowledgeEntry updatedEntry, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
