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
/// Tests for EmailAccountRepository covering email account management and sync operations.
/// Target: 12 tests for critical account synchronization
/// </summary>
public class EmailAccountRepositoryTests : IDisposable
{
    private readonly MicDbContext _context;
    private readonly EmailAccountRepository _repository;
    private readonly Guid _testUserId;

    public EmailAccountRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<MicDbContext>()
            .UseInMemoryDatabase(databaseName: $"EmailAccountRepoTest_{Guid.NewGuid()}")
            .Options;

        _context = new MicDbContext(options);
        _repository = new EmailAccountRepository(_context);
        _testUserId = Guid.NewGuid();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetByUserIdAsync_WithMultipleAccounts_ReturnsPrimaryFirst()
    {
        // Arrange
        var account1 = CreateEmailAccount("secondary@test.com", isPrimary: false);
        var account2 = CreateEmailAccount("primary@test.com", isPrimary: true);
        var account3 = CreateEmailAccount("another@test.com", isPrimary: false);

        await _repository.AddAsync(account1);
        await _repository.AddAsync(account2);
        await _repository.AddAsync(account3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByUserIdAsync(_testUserId);

        // Assert
        result.Should().HaveCount(3);
        result.First().EmailAddress.Should().Be("primary@test.com");
        result.First().IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task GetByUserIdAsync_WithNoPrimaryAccount_OrdersAlphabetically()
    {
        // Arrange
        var account1 = CreateEmailAccount("zebra@test.com", isPrimary: false);
        var account2 = CreateEmailAccount("alpha@test.com", isPrimary: false);
        var account3 = CreateEmailAccount("beta@test.com", isPrimary: false);

        await _repository.AddAsync(account1);
        await _repository.AddAsync(account2);
        await _repository.AddAsync(account3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByUserIdAsync(_testUserId);

        // Assert
        result.Should().HaveCount(3);
        result[0].EmailAddress.Should().Be("alpha@test.com");
        result[1].EmailAddress.Should().Be("beta@test.com");
        result[2].EmailAddress.Should().Be("zebra@test.com");
    }

    [Fact]
    public async Task GetByUserIdAsync_WithNoAccounts_ReturnsEmptyList()
    {
        // Act
        var result = await _repository.GetByUserIdAsync(_testUserId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByUserIdAsync_OnlyReturnsUserAccounts()
    {
        // Arrange
        var userAccount = CreateEmailAccount("user@test.com");
        var otherUserAccount = CreateEmailAccount("other@test.com", userId: Guid.NewGuid());

        await _repository.AddAsync(userAccount);
        await _repository.AddAsync(otherUserAccount);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByUserIdAsync(_testUserId);

        // Assert
        result.Should().ContainSingle();
        result.First().EmailAddress.Should().Be("user@test.com");
    }

    [Fact]
    public async Task GetPrimaryAsync_WithPrimaryAccount_ReturnsIt()
    {
        // Arrange
        var primaryAccount = CreateEmailAccount("primary@test.com", isPrimary: true);
        var secondaryAccount = CreateEmailAccount("secondary@test.com", isPrimary: false);

        await _repository.AddAsync(primaryAccount);
        await _repository.AddAsync(secondaryAccount);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetPrimaryAsync(_testUserId);

        // Assert
        result.Should().NotBeNull();
        result!.EmailAddress.Should().Be("primary@test.com");
        result.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task GetPrimaryAsync_WithNoPrimaryAccount_ReturnsNull()
    {
        // Arrange
        var account = CreateEmailAccount("test@test.com", isPrimary: false);
        await _repository.AddAsync(account);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetPrimaryAsync(_testUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAccountsNeedingSyncAsync_ReturnsAccountsNeverSynced()
    {
        // Arrange
        var neverSynced = CreateEmailAccount("never@test.com", lastSyncedAt: null);
        var recentlySynced = CreateEmailAccount("recent@test.com", lastSyncedAt: DateTime.UtcNow.AddMinutes(-5));

        await _repository.AddAsync(neverSynced);
        await _repository.AddAsync(recentlySynced);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAccountsNeedingSyncAsync();

        // Assert
        result.Should().ContainSingle();
        result.First().EmailAddress.Should().Be("never@test.com");
    }

    [Fact]
    public async Task GetAccountsNeedingSyncAsync_ReturnsAccountsPastSyncInterval()
    {
        // Arrange
        var needsSync = CreateEmailAccount("old@test.com",
            lastSyncedAt: DateTime.UtcNow.AddMinutes(-35),
            syncIntervalMinutes: 30);
        var recentSync = CreateEmailAccount("recent@test.com",
            lastSyncedAt: DateTime.UtcNow.AddMinutes(-10),
            syncIntervalMinutes: 30);

        await _repository.AddAsync(needsSync);
        await _repository.AddAsync(recentSync);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAccountsNeedingSyncAsync();

        // Assert
        result.Should().ContainSingle();
        result.First().EmailAddress.Should().Be("old@test.com");
    }

    [Fact]
    public async Task GetAccountsNeedingSyncAsync_ExcludesInactiveAccounts()
    {
        // Arrange
        var activeAccount = CreateEmailAccount("active@test.com", isActive: true, lastSyncedAt: null);
        var inactiveAccount = CreateEmailAccount("inactive@test.com", isActive: false, lastSyncedAt: null);

        await _repository.AddAsync(activeAccount);
        await _repository.AddAsync(inactiveAccount);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAccountsNeedingSyncAsync();

        // Assert
        result.Should().ContainSingle();
        result.First().EmailAddress.Should().Be("active@test.com");
    }

    [Fact]
    public async Task GetAccountsNeedingSyncAsync_ExcludesAccountsCurrentlySyncing()
    {
        // Arrange
        var availableAccount = CreateEmailAccount("available@test.com", status: SyncStatus.Completed, lastSyncedAt: null);
        var syncingAccount = CreateEmailAccount("syncing@test.com", status: SyncStatus.InProgress, lastSyncedAt: null);

        await _repository.AddAsync(availableAccount);
        await _repository.AddAsync(syncingAccount);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAccountsNeedingSyncAsync();

        // Assert
        result.Should().ContainSingle();
        result.First().EmailAddress.Should().Be("available@test.com");
    }

    [Fact]
    public async Task GetAccountsNeedingSyncAsync_WithNoAccountsNeeded_ReturnsEmpty()
    {
        // Arrange
        var recentSync = CreateEmailAccount("recent@test.com",
            lastSyncedAt: DateTime.UtcNow,
            syncIntervalMinutes: 30);

        await _repository.AddAsync(recentSync);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAccountsNeedingSyncAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task IsEmailConnectedAsync_WithExistingEmail_ReturnsTrue()
    {
        // Arrange
        var account = CreateEmailAccount("test@example.com");
        await _repository.AddAsync(account);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.IsEmailConnectedAsync("test@example.com", _testUserId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsEmailConnectedAsync_IsCaseInsensitive()
    {
        // Arrange
        var account = CreateEmailAccount("Test@Example.COM");
        await _repository.AddAsync(account);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.IsEmailConnectedAsync("test@example.com", _testUserId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsEmailConnectedAsync_WithNonExistentEmail_ReturnsFalse()
    {
        // Act
        var result = await _repository.IsEmailConnectedAsync("nonexistent@test.com", _testUserId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsEmailConnectedAsync_OnlyChecksCurrentUser()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        var otherUserAccount = CreateEmailAccount("test@example.com", userId: otherUserId);
        await _repository.AddAsync(otherUserAccount);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.IsEmailConnectedAsync("test@example.com", _testUserId);

        // Assert
        result.Should().BeFalse();
    }

    #region Helper Methods

    private EmailAccount CreateEmailAccount(
        string emailAddress,
        Guid? userId = null,
        bool isPrimary = false,
        bool isActive = true,
        SyncStatus status = SyncStatus.Completed,
        DateTime? lastSyncedAt = null,
        int syncIntervalMinutes = 30)
    {
        var account = new EmailAccount(
            emailAddress: emailAddress,
            provider: EmailProvider.Gmail,
            userId: userId ?? _testUserId,
            displayName: $"Display Name for {emailAddress}");

        // Set primary status if needed
        if (isPrimary)
        {
            account.SetAsPrimary();
        }
        else
        {
            account.RemovePrimary();
        }

        // Set active status
        if (!isActive)
        {
            account.Deactivate();
        }

        // Update sync settings
        account.UpdateSyncSettings(syncIntervalMinutes: syncIntervalMinutes);

        // Update sync status only if we need a specific status or last synced date
        // Note: Don't set status to Completed with null lastSyncedAt as that will auto-set it to UtcNow
        if (status != SyncStatus.NotStarted)
        {
            if (status == SyncStatus.Completed && !lastSyncedAt.HasValue)
            {
                // Don't update status - leave it as NotStarted to keep LastSyncedAt null
            }
            else
            {
                account.UpdateSyncStatus(status, 0, 0, lastSyncedAt);
            }
        }

        return account;
    }

    #endregion
}
