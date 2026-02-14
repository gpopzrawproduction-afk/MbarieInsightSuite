using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.Data.Persistence;
using MIC.Infrastructure.Data.Repositories;
using Xunit;

namespace MIC.Tests.Unit.Infrastructure.Data.Repositories;

/// <summary>
/// Tests for EmailRepository covering email message management and querying.
/// Target: 12 tests for critical email infrastructure
/// </summary>
public class EmailRepositoryTests : IDisposable
{
    private readonly MicDbContext _context;
    private readonly EmailRepository _repository;
    private readonly Guid _testUserId;
    private readonly Guid _testAccountId;

    public EmailRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<MicDbContext>()
            .UseInMemoryDatabase(databaseName: $"EmailRepoTest_{Guid.NewGuid()}")
            .Options;

        _context = new MicDbContext(options);
        _repository = new EmailRepository(_context);
        _testUserId = Guid.NewGuid();
        _testAccountId = Guid.NewGuid();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetEmailsAsync_WithNoFilters_ReturnsAllUserEmails()
    {
        // Arrange
        var email1 = CreateEmail("Subject 1", EmailFolder.Inbox);
        var email2 = CreateEmail("Subject 2", EmailFolder.Sent);
        var otherUserEmail = CreateEmail("Other Subject", EmailFolder.Inbox, Guid.NewGuid());

        await _repository.AddAsync(email1);
        await _repository.AddAsync(email2);
        await _repository.AddAsync(otherUserEmail);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetEmailsAsync(_testUserId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(e => e.Subject == "Subject 1");
        result.Should().Contain(e => e.Subject == "Subject 2");
    }

    [Fact]
    public async Task GetEmailsAsync_WithEmailAccountFilter_ReturnsOnlyAccountEmails()
    {
        // Arrange
        var account1 = Guid.NewGuid();
        var account2 = Guid.NewGuid();

        var email1 = CreateEmail("Subject 1", EmailFolder.Inbox, _testUserId, account1);
        var email2 = CreateEmail("Subject 2", EmailFolder.Inbox, _testUserId, account2);

        await _repository.AddAsync(email1);
        await _repository.AddAsync(email2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetEmailsAsync(_testUserId, emailAccountId: account1);

        // Assert
        result.Should().ContainSingle();
        result.First().Subject.Should().Be("Subject 1");
    }

    [Fact]
    public async Task GetEmailsAsync_WithFolderFilter_ReturnsOnlyFolderEmails()
    {
        // Arrange
        var inboxEmail = CreateEmail("Inbox Email", EmailFolder.Inbox);
        var sentEmail = CreateEmail("Sent Email", EmailFolder.Sent);
        var draftEmail = CreateEmail("Draft Email", EmailFolder.Drafts);

        await _repository.AddAsync(inboxEmail);
        await _repository.AddAsync(sentEmail);
        await _repository.AddAsync(draftEmail);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetEmailsAsync(_testUserId, folder: EmailFolder.Inbox);

        // Assert
        result.Should().ContainSingle();
        result.First().Subject.Should().Be("Inbox Email");
    }

    [Fact]
    public async Task GetEmailsAsync_WithUnreadFilter_ReturnsOnlyUnreadEmails()
    {
        // Arrange
        var unreadEmail = CreateEmail("Unread Email", EmailFolder.Inbox, isRead: false);
        var readEmail = CreateEmail("Read Email", EmailFolder.Inbox, isRead: true);

        await _repository.AddAsync(unreadEmail);
        await _repository.AddAsync(readEmail);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetEmailsAsync(_testUserId, isUnread: true);

        // Assert
        result.Should().ContainSingle();
        result.First().Subject.Should().Be("Unread Email");
    }

    [Fact]
    public async Task GetEmailsAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange - Create 25 emails
        for (int i = 1; i <= 25; i++)
        {
            var email = CreateEmail($"Email {i}", EmailFolder.Inbox);
            await _repository.AddAsync(email);
        }
        await _context.SaveChangesAsync();

        // Act - Get second page (skip 10, take 10)
        var page2 = await _repository.GetEmailsAsync(_testUserId, skip: 10, take: 10);

        // Assert
        page2.Should().HaveCount(10);
    }

    [Fact]
    public async Task GetByMessageIdAsync_ExistingEmail_ReturnsEmail()
    {
        // Arrange
        var messageId = "unique-message-id-123";
        var email = CreateEmail("Test Subject", EmailFolder.Inbox, messageId: messageId);
        await _repository.AddAsync(email);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByMessageIdAsync(messageId);

        // Assert
        result.Should().NotBeNull();
        result!.MessageId.Should().Be(messageId);
        result.Subject.Should().Be("Test Subject");
    }

    [Fact]
    public async Task GetByMessageIdAsync_NonExistentEmail_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByMessageIdAsync("non-existent-id");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetConversationAsync_ReturnsEmailsInChronologicalOrder()
    {
        // Arrange
        var conversationId = "conv-123";
        var email1 = CreateEmail("First", EmailFolder.Inbox, conversationId: conversationId, sentDate: DateTime.UtcNow.AddHours(-2));
        var email2 = CreateEmail("Second", EmailFolder.Inbox, conversationId: conversationId, sentDate: DateTime.UtcNow.AddHours(-1));
        var email3 = CreateEmail("Third", EmailFolder.Inbox, conversationId: conversationId, sentDate: DateTime.UtcNow);
        var differentConv = CreateEmail("Different", EmailFolder.Inbox, conversationId: "other-conv");

        await _repository.AddAsync(email1);
        await _repository.AddAsync(email2);
        await _repository.AddAsync(email3);
        await _repository.AddAsync(differentConv);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetConversationAsync(conversationId);

        // Assert
        result.Should().HaveCount(3);
        result[0].Subject.Should().Be("First");
        result[1].Subject.Should().Be("Second");
        result[2].Subject.Should().Be("Third");
    }

    [Fact]
    public async Task GetConversationAsync_WithNoMatchingEmails_ReturnsEmptyList()
    {
        // Act
        var result = await _repository.GetConversationAsync("missing-conv");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetConversationAsync_IncludesEmailsAcrossFolders()
    {
        // Arrange
        var conversationId = "shared-thread";
        var inboxEmail = CreateEmail("Inbox", EmailFolder.Inbox, conversationId: conversationId, sentDate: DateTime.UtcNow.AddHours(-3));
        var sentEmail = CreateEmail("Sent", EmailFolder.Sent, conversationId: conversationId, sentDate: DateTime.UtcNow.AddHours(-2));
        var archiveEmail = CreateEmail("Archive", EmailFolder.Archive, conversationId: conversationId, sentDate: DateTime.UtcNow.AddHours(-1));

        await _repository.AddAsync(inboxEmail);
        await _repository.AddAsync(sentEmail);
        await _repository.AddAsync(archiveEmail);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetConversationAsync(conversationId);

        // Assert
        result.Should().HaveCount(3);
        result.Select(e => e.Subject).Should().Contain(new[] { "Inbox", "Sent", "Archive" });
    }

    [Fact]
    public async Task GetUnreadCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        await _repository.AddAsync(CreateEmail("Unread 1", EmailFolder.Inbox, isRead: false));
        await _repository.AddAsync(CreateEmail("Unread 2", EmailFolder.Inbox, isRead: false));
        await _repository.AddAsync(CreateEmail("Read", EmailFolder.Inbox, isRead: true));
        await _context.SaveChangesAsync();

        // Act
        var count = await _repository.GetUnreadCountAsync(_testUserId);

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public async Task GetUnreadCountAsync_WithAccountFilter_ReturnsFilteredCount()
    {
        // Arrange
        var account1 = Guid.NewGuid();
        var account2 = Guid.NewGuid();

        await _repository.AddAsync(CreateEmail("Account1", EmailFolder.Inbox, accountId: account1, isRead: false));
        await _repository.AddAsync(CreateEmail("Account1 Read", EmailFolder.Inbox, accountId: account1, isRead: true));
        await _repository.AddAsync(CreateEmail("Account2", EmailFolder.Inbox, accountId: account2, isRead: false));
        await _context.SaveChangesAsync();

        // Act
        var count = await _repository.GetUnreadCountAsync(_testUserId, account1);

        // Assert
        count.Should().Be(1);
    }

    [Fact]
    public async Task GetRequiresResponseCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        await _repository.AddAsync(CreateEmail("Needs Response 1", EmailFolder.Inbox, requiresResponse: true, isRead: false));
        await _repository.AddAsync(CreateEmail("Needs Response 2", EmailFolder.Inbox, requiresResponse: true, isRead: false));
        await _repository.AddAsync(CreateEmail("No Response", EmailFolder.Inbox, requiresResponse: false));
        await _repository.AddAsync(CreateEmail("Already Read", EmailFolder.Inbox, requiresResponse: true, isRead: true));
        await _context.SaveChangesAsync();

        // Act
        var count = await _repository.GetRequiresResponseCountAsync(_testUserId);

        // Assert
        count.Should().Be(2); // Only unread emails requiring response
    }

    [Fact]
    public async Task MarkAsReadAsync_UpdatesMultipleEmails()
    {
        // Arrange
        var email1 = CreateEmail("Email 1", EmailFolder.Inbox, isRead: false);
        var email2 = CreateEmail("Email 2", EmailFolder.Inbox, isRead: false);
        var email3 = CreateEmail("Email 3", EmailFolder.Inbox, isRead: false);

        await _repository.AddAsync(email1);
        await _repository.AddAsync(email2);
        await _repository.AddAsync(email3);
        await _context.SaveChangesAsync();

        // Act
        await _repository.MarkAsReadAsync(new[] { email1.Id, email2.Id });

        // Assert
        var updated1 = await _repository.GetByIdAsync(email1.Id);
        var updated2 = await _repository.GetByIdAsync(email2.Id);
        var notUpdated = await _repository.GetByIdAsync(email3.Id);

        updated1!.IsRead.Should().BeTrue();
        updated2!.IsRead.Should().BeTrue();
        notUpdated!.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task MarkAsReadAsync_WithNonExistingIds_LeavesExistingEmailsUnchanged()
    {
        // Arrange
        var email = CreateEmail("Existing", EmailFolder.Inbox, isRead: false);
        await _repository.AddAsync(email);
        await _context.SaveChangesAsync();

        // Act
        await _repository.MarkAsReadAsync(new[] { Guid.NewGuid() });

        // Assert
        var refreshed = await _repository.GetByIdAsync(email.Id);
        refreshed!.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_ExistingEmail_ReturnsTrue()
    {
        // Arrange
        var messageId = "existing-message-id";
        var email = CreateEmail("Test", EmailFolder.Inbox, messageId: messageId);
        await _repository.AddAsync(email);
        await _context.SaveChangesAsync();

        // Act
        var exists = await _repository.ExistsAsync(messageId);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_NonExistentEmail_ReturnsFalse()
    {
        // Act
        var exists = await _repository.ExistsAsync("non-existent-id");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task MarkAsReadAsync_WithEmptyList_DoesNotThrow()
    {
        // Arrange & Act
        var act = async () => await _repository.MarkAsReadAsync(Array.Empty<Guid>());

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetRequiresResponseCountAsync_WithNoEmails_ReturnsZero()
    {
        // Arrange & Act
        var count = await _repository.GetRequiresResponseCountAsync(Guid.NewGuid());

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public async Task GetEmailsAsync_WithReadFilter_ReturnsOnlyReadEmails()
    {
        // Arrange
        var read1 = CreateEmail("Read Email 1", EmailFolder.Inbox);
        var read2 = CreateEmail("Read Email 2", EmailFolder.Inbox);
        var unread = CreateEmail("Unread Email", EmailFolder.Inbox);
        _context.EmailMessages.AddRange(read1, read2, unread);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetEmailsAsync(_testUserId, null, EmailFolder.Inbox, false, 0, 50);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetEmailsAsync_WithDifferentUsers_IsolatesData()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        var email1 = CreateEmail("My Email", EmailFolder.Inbox);
        var otherEmail = CreateEmail("Other Email", EmailFolder.Inbox, userId: otherUserId);
        _context.EmailMessages.AddRange(email1, otherEmail);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetEmailsAsync(_testUserId, null, null, null, 0, 50);

        // Assert
        result.Should().HaveCount(1);
        result[0].Subject.Should().Be("My Email");
    }

    [Fact]
    public async Task GetUnreadCountAsync_WithNoEmails_ReturnsZero()
    {
        // Act
        var count = await _repository.GetUnreadCountAsync(Guid.NewGuid());

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public async Task ExistsAsync_WithEmptyMessageId_ReturnsFalse()
    {
        // Act
        var exists = await _repository.ExistsAsync(string.Empty);

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task GetConversationAsync_WithEmptyConversationId_ReturnsEmpty()
    {
        // Act
        var result = await _repository.GetConversationAsync(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    #region Helper Methods

    private EmailMessage CreateEmail(
        string subject,
        EmailFolder folder,
        Guid? userId = null,
        Guid? accountId = null,
        string? messageId = null,
        string? conversationId = null,
        DateTime? sentDate = null,
        bool isRead = false,
        bool requiresResponse = false)
    {
        var email = new EmailMessage(
            messageId: messageId ?? Guid.NewGuid().ToString(),
            subject: subject,
            fromAddress: "sender@test.com",
            fromName: "Test Sender",
            toRecipients: "recipient@test.com",
            sentDate: sentDate ?? DateTime.UtcNow.AddHours(-1),
            receivedDate: DateTime.UtcNow,
            bodyText: $"Body text for {subject}",
            userId: userId ?? _testUserId,
            emailAccountId: accountId ?? _testAccountId,
            folder: folder);

        if (!string.IsNullOrWhiteSpace(conversationId))
        {
            email.SetThreadInfo(conversationId, null);
        }

        if (isRead)
        {
            email.MarkAsRead();
        }

        if (requiresResponse || isRead)
        {
            email.SetInboxFlags(EmailPriority.Normal, isUrgent: false, isRead: isRead, requiresResponse: requiresResponse, containsActionItems: false);
        }

        return email;
    }

    #endregion
}
