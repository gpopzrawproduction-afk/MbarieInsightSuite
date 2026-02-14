using System;
using System.Threading.Tasks;
using FluentAssertions;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.Data.Persistence;
using MIC.Infrastructure.Data.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MIC.Tests.Unit.Infrastructure.Data;

/// <summary>
/// Tests for KnowledgeBaseService covering CRUD operations on knowledge entries.
/// </summary>
public class KnowledgeBaseServiceTests : IDisposable
{
    private readonly MicDbContext _context;
    private readonly KnowledgeBaseService _service;

    public KnowledgeBaseServiceTests()
    {
        var options = new DbContextOptionsBuilder<MicDbContext>()
            .UseInMemoryDatabase(databaseName: $"KnowledgeBaseTest_{Guid.NewGuid()}")
            .Options;

        _context = new MicDbContext(options);
        _service = new KnowledgeBaseService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region CreateEntryAsync Tests

    [Fact]
    public async Task CreateEntryAsync_AddsEntryToDatabase()
    {
        var userId = Guid.NewGuid();
        var entry = new KnowledgeEntry
        {
            Title = "Test Entry",
            Content = "Test content",
            FullContent = "Full test content",
            SourceType = "Manual",
            UserId = userId,
            Tags = new() { "test" }
        };

        var result = await _service.CreateEntryAsync(entry);

        result.Should().NotBeNull();
        result.Title.Should().Be("Test Entry");

        var stored = await _context.KnowledgeEntries.FindAsync(result.Id);
        stored.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateEntryAsync_ReturnsCreatedEntry()
    {
        var entry = new KnowledgeEntry
        {
            Title = "Return Test",
            Content = "content",
            FullContent = "content",
            UserId = Guid.NewGuid()
        };

        var result = await _service.CreateEntryAsync(entry);

        result.Should().BeSameAs(entry);
        result.Id.Should().NotBe(Guid.Empty);
    }

    #endregion

    #region UpdateEntryAsync Tests

    [Fact]
    public async Task UpdateEntryAsync_UpdatesExistingEntry()
    {
        var entry = new KnowledgeEntry
        {
            Title = "Original",
            Content = "Original content",
            FullContent = "Original full content",
            UserId = Guid.NewGuid(),
            Tags = new() { "original" }
        };
        await _context.KnowledgeEntries.AddAsync(entry);
        await _context.SaveChangesAsync();

        var updated = new KnowledgeEntry
        {
            Title = "Updated",
            Content = "Updated content",
            Tags = new() { "updated" }
        };

        await _service.UpdateEntryAsync(entry.Id, updated);

        var stored = await _context.KnowledgeEntries.FindAsync(entry.Id);
        stored!.Title.Should().Be("Updated");
        stored.Content.Should().Be("Updated content");
        stored.Tags.Should().Contain("updated");
    }

    [Fact]
    public async Task UpdateEntryAsync_DoesNothingForMissingEntry()
    {
        var updated = new KnowledgeEntry
        {
            Title = "Ghost",
            Content = "Ghost content",
            Tags = new()
        };

        // Should not throw
        await _service.UpdateEntryAsync(Guid.NewGuid(), updated);
    }

    #endregion

    #region DeleteEntryAsync Tests

    [Fact]
    public async Task DeleteEntryAsync_RemovesEntry()
    {
        var entry = new KnowledgeEntry
        {
            Title = "Delete Me",
            Content = "To be deleted",
            FullContent = "To be deleted full",
            UserId = Guid.NewGuid()
        };
        await _context.KnowledgeEntries.AddAsync(entry);
        await _context.SaveChangesAsync();

        await _service.DeleteEntryAsync(entry.Id);

        var stored = await _context.KnowledgeEntries.FindAsync(entry.Id);
        stored.Should().BeNull();
    }

    [Fact]
    public async Task DeleteEntryAsync_DoesNothingForMissingEntry()
    {
        // Should not throw
        await _service.DeleteEntryAsync(Guid.NewGuid());
    }

    #endregion

    #region SearchAsync Tests

    // Note: SearchAsync uses Tags.Any(t => t.ToLower().Contains(...)) which is not 
    // supported by EF Core InMemory provider. These tests are only valid with a real DB.
    // CRUD operations are tested above.

    #endregion

    #region GetRelatedEntriesAsync Tests

    // Note: GetRelatedEntriesAsync uses Tags.Contains(...) which is not supported
    // by EF Core InMemory provider for List<string> columns. Testing deferred to integration tests.

    #endregion

    #region IndexEmailAsync Tests

    [Fact]
    public async Task IndexEmailAsync_CreatesKnowledgeEntry()
    {
        var userId = Guid.NewGuid();
        var email = new EmailMessage(
            messageId: "index-test",
            subject: "Important Email",
            fromAddress: "sender@example.com",
            fromName: "Sender",
            toRecipients: "recipient@example.com",
            sentDate: DateTime.UtcNow,
            receivedDate: DateTime.UtcNow,
            bodyText: "Email body content",
            userId: userId,
            emailAccountId: Guid.NewGuid());

        await _service.IndexEmailAsync(email);

        var entries = await _context.KnowledgeEntries.ToListAsync();
        entries.Should().HaveCount(1);
        entries[0].Title.Should().Be("Important Email");
        entries[0].SourceType.Should().Be("EmailMessage");
        entries[0].UserId.Should().Be(userId);
    }

    [Fact]
    public async Task IndexEmailAsync_SetsDefaultTags_ForNormalEmail()
    {
        var email = new EmailMessage(
            messageId: "tag-test",
            subject: "Normal Email",
            fromAddress: "sender@example.com",
            fromName: "Sender",
            toRecipients: "recipient@example.com",
            sentDate: DateTime.UtcNow,
            receivedDate: DateTime.UtcNow,
            bodyText: "Body",
            userId: Guid.NewGuid(),
            emailAccountId: Guid.NewGuid());

        await _service.IndexEmailAsync(email);

        var entry = await _context.KnowledgeEntries.FirstAsync();
        // Normal priority + General category = no special tags added
        entry.Tags.Should().NotBeNull();
    }

    [Fact]
    public async Task IndexEmailAsync_SetsSourceId_ToEmailId()
    {
        var email = new EmailMessage(
            messageId: "source-test",
            subject: "Source Test",
            fromAddress: "sender@example.com",
            fromName: "Sender",
            toRecipients: "recipient@example.com",
            sentDate: DateTime.UtcNow,
            receivedDate: DateTime.UtcNow,
            bodyText: "Body",
            userId: Guid.NewGuid(),
            emailAccountId: Guid.NewGuid());

        await _service.IndexEmailAsync(email);

        var entry = await _context.KnowledgeEntries.FirstAsync();
        entry.SourceId.Should().Be(email.Id);
    }

    [Fact]
    public async Task IndexEmailAsync_UsesBodyText_AsFullContent()
    {
        var email = new EmailMessage(
            messageId: "content-test",
            subject: "Content Test",
            fromAddress: "sender@example.com",
            fromName: "Sender",
            toRecipients: "recipient@example.com",
            sentDate: DateTime.UtcNow,
            receivedDate: DateTime.UtcNow,
            bodyText: "Full body text here",
            userId: Guid.NewGuid(),
            emailAccountId: Guid.NewGuid());

        await _service.IndexEmailAsync(email);

        var entry = await _context.KnowledgeEntries.FirstAsync();
        entry.FullContent.Should().Be("Full body text here");
    }

    #endregion

    #region Multiple Operations Tests

    [Fact]
    public async Task CreateAndDelete_RemovesEntry()
    {
        var entry = new KnowledgeEntry
        {
            Title = "Temp",
            Content = "Temp content",
            FullContent = "Temp full",
            UserId = Guid.NewGuid()
        };
        
        var created = await _service.CreateEntryAsync(entry);
        var preCount = await _context.KnowledgeEntries.CountAsync();
        preCount.Should().Be(1);

        await _service.DeleteEntryAsync(created.Id);
        
        var postCount = await _context.KnowledgeEntries.CountAsync();
        postCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateAndUpdate_ModifiesCorrectEntry()
    {
        var entry1 = new KnowledgeEntry { Title = "First", Content = "Content 1", FullContent = "Full 1", UserId = Guid.NewGuid() };
        var entry2 = new KnowledgeEntry { Title = "Second", Content = "Content 2", FullContent = "Full 2", UserId = Guid.NewGuid() };
        
        await _service.CreateEntryAsync(entry1);
        await _service.CreateEntryAsync(entry2);

        var update = new KnowledgeEntry { Title = "Updated First", Content = "Updated Content 1", Tags = new() { "updated" } };
        await _service.UpdateEntryAsync(entry1.Id, update);

        var stored1 = await _context.KnowledgeEntries.FindAsync(entry1.Id);
        var stored2 = await _context.KnowledgeEntries.FindAsync(entry2.Id);
        
        stored1!.Title.Should().Be("Updated First");
        stored2!.Title.Should().Be("Second"); // Unchanged
    }

    #endregion
}
