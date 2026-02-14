using System;
using System.Collections.Generic;
using FluentAssertions;
using MIC.Core.Domain.Entities;
using Xunit;

namespace MIC.Tests.Unit.Core.Domain.Entities;

/// <summary>
/// Tests for EmailAccount entity covering OAuth tokens, sync state, IMAP credentials, and business rules.
/// Target: 35 tests for comprehensive EmailAccount coverage
/// </summary>
public class EmailAccountTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidInputs_CreatesAccount()
    {
        // Arrange
        var emailAddress = "user@example.com";
        var provider = EmailProvider.Gmail;
        var userId = Guid.NewGuid();
        var displayName = "User Display Name";

        // Act
        var account = new EmailAccount(emailAddress, provider, userId, displayName);

        // Assert
        account.EmailAddress.Should().Be(emailAddress);
        account.Provider.Should().Be(provider);
        account.UserId.Should().Be(userId);
        account.DisplayName.Should().Be(displayName);
        account.IsActive.Should().BeTrue();
        account.IsPrimary.Should().BeTrue();
        account.Status.Should().Be(SyncStatus.NotStarted);
    }

    [Fact]
    public void Constructor_WithoutDisplayName_UsesEmailAddress()
    {
        // Arrange
        var emailAddress = "user@example.com";
        var userId = Guid.NewGuid();

        // Act
        var account = new EmailAccount(emailAddress, EmailProvider.Outlook, userId);

        // Assert
        account.DisplayName.Should().Be(emailAddress);
    }

    [Fact]
    public void Constructor_WithNullEmailAddress_ThrowsArgumentException()
    {
        // Act
        Action act = () => new EmailAccount(null!, EmailProvider.Gmail, Guid.NewGuid());

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithEmptyEmailAddress_ThrowsArgumentException()
    {
        // Act
        Action act = () => new EmailAccount("", EmailProvider.Gmail, Guid.NewGuid());

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithEmptyUserId_ThrowsArgumentException()
    {
        // Act
        Action act = () => new EmailAccount("user@example.com", EmailProvider.Gmail, Guid.Empty);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_SetsDefaultSyncSettings()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.Outlook, Guid.NewGuid());

        // Assert
        account.SyncIntervalMinutes.Should().Be(5);
        account.InitialSyncDays.Should().Be(365);
        account.SyncAttachments.Should().BeTrue();
        account.MaxAttachmentSizeMB.Should().Be(25);
    }

    #endregion

    #region SetTokens Tests

    [Fact]
    public void SetTokens_WithValidInputs_SetsAllTokenFields()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.Gmail, Guid.NewGuid());
        var accessToken = "access_token_123";
        var refreshToken = "refresh_token_456";
        var expiresAt = DateTime.UtcNow.AddHours(1);
        var scopes = "email profile";

        // Act
        account.SetTokens(accessToken, refreshToken, expiresAt, scopes);

        // Assert
        account.AccessTokenEncrypted.Should().Be(accessToken);
        account.RefreshTokenEncrypted.Should().Be(refreshToken);
        account.TokenExpiresAt.Should().Be(expiresAt);
        account.GrantedScopes.Should().Be(scopes);
    }

    [Fact]
    public void SetTokens_ClearsErrorState()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.Gmail, Guid.NewGuid());
        account.SetSyncFailed("Previous error");

        // Act
        account.SetTokens("new_token", null, DateTime.UtcNow.AddHours(1));

        // Assert
        account.LastSyncError.Should().BeNull();
        account.ConsecutiveFailures.Should().Be(0);
    }

    [Fact]
    public void SetTokens_WithNullRefreshToken_AllowsNullRefreshToken()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.Gmail, Guid.NewGuid());

        // Act
        account.SetTokens("access_token", null, DateTime.UtcNow.AddHours(1));

        // Assert
        account.RefreshTokenEncrypted.Should().BeNull();
    }

    #endregion

    #region IsTokenExpired Tests

    [Fact]
    public void IsTokenExpired_WithNoToken_ReturnsTrue()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.Gmail, Guid.NewGuid());

        // Act
        var result = account.IsTokenExpired();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsTokenExpired_WithValidToken_ReturnsFalse()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.Gmail, Guid.NewGuid());
        var expiresAt = DateTime.UtcNow.AddHours(1);
        account.SetTokens("token", null, expiresAt);

        // Act
        var result = account.IsTokenExpired();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsTokenExpired_WithExpiredToken_ReturnsTrue()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.Gmail, Guid.NewGuid());
        var expiresAt = DateTime.UtcNow.AddMinutes(-10);
        account.SetTokens("token", null, expiresAt);

        // Act
        var result = account.IsTokenExpired();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsTokenExpired_WithTokenExpiringWithinBuffer_ReturnsTrue()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.Gmail, Guid.NewGuid());
        var expiresAt = DateTime.UtcNow.AddMinutes(3); // Within 5-minute default buffer
        account.SetTokens("token", null, expiresAt);

        // Act
        var result = account.IsTokenExpired();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsTokenExpired_WithCustomBuffer_UsesCustomBuffer()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.Gmail, Guid.NewGuid());
        var expiresAt = DateTime.UtcNow.AddMinutes(15);
        account.SetTokens("token", null, expiresAt);

        // Act
        var result = account.IsTokenExpired(TimeSpan.FromMinutes(20));

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region SetImapSmtpCredentials Tests

    [Fact]
    public void SetImapSmtpCredentials_WithValidInputs_SetsAllFields()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.IMAP, Guid.NewGuid());

        // Act
        account.SetImapSmtpCredentials(
            imapServer: "imap.example.com",
            imapPort: 993,
            smtpServer: "smtp.example.com",
            smtpPort: 465,
            useSsl: true,
            password: "password123");

        // Assert
        account.ImapServer.Should().Be("imap.example.com");
        account.ImapPort.Should().Be(993);
        account.SmtpServer.Should().Be("smtp.example.com");
        account.SmtpPort.Should().Be(465);
        account.UseSsl.Should().BeTrue();
        account.PasswordEncrypted.Should().Be("password123");
        account.Provider.Should().Be(EmailProvider.IMAP);
    }

    [Fact]
    public void SetImapSmtpCredentials_ClearsErrorState()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.IMAP, Guid.NewGuid());
        account.SetSyncFailed("Connection error");

        // Act
        account.SetImapSmtpCredentials("imap.example.com", 993, "smtp.example.com", 465, true, "pass");

        // Assert
        account.LastSyncError.Should().BeNull();
        account.ConsecutiveFailures.Should().Be(0);
    }

    [Fact]
    public void SetImapSmtpCredentials_WithInvalidImapPort_ThrowsArgumentException()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.IMAP, Guid.NewGuid());

        // Act
        Action act = () => account.SetImapSmtpCredentials("imap.example.com", 99999, "smtp.example.com", 465, true, "pass");

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("imapPort");
    }

    [Fact]
    public void SetImapSmtpCredentials_WithInvalidSmtpPort_ThrowsArgumentException()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.IMAP, Guid.NewGuid());

        // Act
        Action act = () => account.SetImapSmtpCredentials("imap.example.com", 993, "smtp.example.com", 0, true, "pass");

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("smtpPort");
    }

    #endregion

    #region UpdateSyncStatus Tests

    [Fact]
    public void UpdateSyncStatus_WithCompletedStatus_UpdatesStatistics()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.Gmail, Guid.NewGuid());

        // Act
        account.UpdateSyncStatus(SyncStatus.Completed, emailsProcessed: 50, attachmentsProcessed: 10);

        // Assert
        account.Status.Should().Be(SyncStatus.Completed);
        account.TotalEmailsSynced.Should().Be(50);
        account.TotalAttachmentsSynced.Should().Be(10);
        account.LastSyncedAt.Should().NotBeNull();
        account.LastSyncError.Should().BeNull();
        account.ConsecutiveFailures.Should().Be(0);
    }

    [Fact]
    public void UpdateSyncStatus_AccumulatesEmailCounts()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.Gmail, Guid.NewGuid());
        account.UpdateSyncStatus(SyncStatus.Completed, emailsProcessed: 50);

        // Act
        account.UpdateSyncStatus(SyncStatus.Completed, emailsProcessed: 30);

        // Assert
        account.TotalEmailsSynced.Should().Be(80);
    }

    [Fact]
    public void UpdateSyncStatus_WithNonCompletedStatus_DoesNotUpdateStatistics()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.Gmail, Guid.NewGuid());

        // Act
        account.UpdateSyncStatus(SyncStatus.InProgress, emailsProcessed: 50);

        // Assert
        account.Status.Should().Be(SyncStatus.InProgress);
        account.TotalEmailsSynced.Should().Be(0);
        account.LastSyncedAt.Should().BeNull();
    }

    [Fact]
    public void UpdateSyncStatus_SetsLastSyncAttemptAt()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.Gmail, Guid.NewGuid());
        var before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        account.UpdateSyncStatus(SyncStatus.InProgress);

        // Assert
        account.LastSyncAttemptAt.Should().NotBeNull();
        account.LastSyncAttemptAt.Should().BeOnOrAfter(before);
    }

    #endregion

    #region SetSyncFailed Tests

    [Fact]
    public void SetSyncFailed_SetsErrorStateAndIncrementsFailureCount()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.Gmail, Guid.NewGuid());
        var errorMessage = "Connection timeout";

        // Act
        account.SetSyncFailed(errorMessage);

        // Assert
        account.Status.Should().Be(SyncStatus.Failed);
        account.LastSyncError.Should().Be(errorMessage);
        account.ConsecutiveFailures.Should().Be(1);
        account.IsActive.Should().BeTrue();
    }

    [Fact]
    public void SetSyncFailed_AfterFiveFailures_DeactivatesAccount()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.Gmail, Guid.NewGuid());

        // Act
        for (int i = 0; i < 5; i++)
        {
            account.SetSyncFailed($"Error {i + 1}");
        }

        // Assert
        account.ConsecutiveFailures.Should().Be(5);
        account.IsActive.Should().BeFalse();
    }

    [Fact]
    public void SetSyncFailed_IncreasesConsecutiveFailureCount()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.Gmail, Guid.NewGuid());

        // Act
        account.SetSyncFailed("Error 1");
        account.SetSyncFailed("Error 2");
        account.SetSyncFailed("Error 3");

        // Assert
        account.ConsecutiveFailures.Should().Be(3);
    }

    #endregion

    #region UpdateSyncSettings Tests

    [Fact]
    public void UpdateSyncSettings_WithValidIntervals_UpdatesSettings()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.Gmail, Guid.NewGuid());

        // Act
        account.UpdateSyncSettings(syncIntervalMinutes: 15);

        // Assert
        account.SyncIntervalMinutes.Should().Be(15);
    }

    [Fact]
    public void UpdateSyncSettings_WithSyncIntervalTooLow_ClampTo1()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.Gmail, Guid.NewGuid());

        // Act
        account.UpdateSyncSettings(syncIntervalMinutes: 0);

        // Assert
        account.SyncIntervalMinutes.Should().Be(1);
    }

    [Fact]
    public void UpdateSyncSettings_WithSyncIntervalTooHigh_ClampTo1440()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.Gmail, Guid.NewGuid());

        // Act
        account.UpdateSyncSettings(syncIntervalMinutes: 2000);

        // Assert
        account.SyncIntervalMinutes.Should().Be(1440);
    }

    [Fact]
    public void UpdateSyncSettings_WithMaxAttachmentSizeTooHigh_ClampTo100()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.Gmail, Guid.NewGuid());

        // Act
        account.UpdateSyncSettings(maxAttachmentSizeMB: 200);

        // Assert
        account.MaxAttachmentSizeMB.Should().Be(100);
    }

    [Fact]
    public void UpdateSyncSettings_WithFoldersToSync_UpdatesFoldersList()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.Gmail, Guid.NewGuid());
        var folders = new List<string> { "Inbox", "Sent", "Important" };

        // Act
        account.UpdateSyncSettings(foldersToSync: folders);

        // Assert
        account.FoldersToSync.Should().BeEquivalentTo(folders);
    }

    #endregion

    #region Activation Tests

    [Fact]
    public void Activate_SetsIsActiveToTrue()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.Gmail, Guid.NewGuid());
        account.Deactivate();

        // Act
        account.Activate();

        // Assert
        account.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Activate_ClearsFailureState()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.Gmail, Guid.NewGuid());
        account.SetSyncFailed("Error");
        account.SetSyncFailed("Error 2");

        // Act
        account.Activate();

        // Assert
        account.ConsecutiveFailures.Should().Be(0);
        account.LastSyncError.Should().BeNull();
    }

    [Fact]
    public void Deactivate_SetsIsActiveToFalse()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.Gmail, Guid.NewGuid());

        // Act
        account.Deactivate();

        // Assert
        account.IsActive.Should().BeFalse();
    }

    #endregion

    #region Primary Status Tests

    [Fact]
    public void SetAsPrimary_SetsPrimaryToTrue()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.Gmail, Guid.NewGuid());
        account.RemovePrimary();

        // Act
        account.SetAsPrimary();

        // Assert
        account.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void RemovePrimary_SetsPrimaryToFalse()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.Gmail, Guid.NewGuid());

        // Act
        account.RemovePrimary();

        // Assert
        account.IsPrimary.Should().BeFalse();
    }

    #endregion

    #region GetFormattedStorageUsed Tests

    [Fact]
    public void GetFormattedStorageUsed_ForBytes_ReturnsFormattedString()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.Gmail, Guid.NewGuid());
        account.UpdateStatistics(storageUsed: 512, unreadCount: 0, requiresResponseCount: 0);

        // Act
        var result = account.GetFormattedStorageUsed();

        // Assert
        result.Should().Be("512 B");
    }

    [Fact]
    public void GetFormattedStorageUsed_ForKilobytes_ReturnsFormattedString()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.Gmail, Guid.NewGuid());
        account.UpdateStatistics(storageUsed: 2048, unreadCount: 0, requiresResponseCount: 0);

        // Act
        var result = account.GetFormattedStorageUsed();

        // Assert
        result.Should().Be("2 KB");
    }

    [Fact]
    public void GetFormattedStorageUsed_ForMegabytes_ReturnsFormattedString()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.Gmail, Guid.NewGuid());
        account.UpdateStatistics(storageUsed: 5242880, unreadCount: 0, requiresResponseCount: 0); // 5 MB

        // Act
        var result = account.GetFormattedStorageUsed();

        // Assert
        result.Should().Be("5 MB");
    }

    [Fact]
    public void GetFormattedStorageUsed_ForGigabytes_ReturnsFormattedString()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.Gmail, Guid.NewGuid());
        account.UpdateStatistics(storageUsed: 2147483648, unreadCount: 0, requiresResponseCount: 0); // 2 GB

        // Act
        var result = account.GetFormattedStorageUsed();

        // Assert
        result.Should().Be("2 GB");
    }

    #endregion

    #region ShouldSyncNow Tests

    [Fact]
    public void ShouldSyncNow_WithInactiveAccount_ReturnsFalse()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.Gmail, Guid.NewGuid());
        account.Deactivate();

        // Act
        var result = account.ShouldSyncNow();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldSyncNow_WithSyncInProgress_ReturnsFalse()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.Gmail, Guid.NewGuid());
        account.UpdateSyncStatus(SyncStatus.InProgress);

        // Act
        var result = account.ShouldSyncNow();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldSyncNow_WithNoLastSync_ReturnsTrue()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.Gmail, Guid.NewGuid());

        // Act
        var result = account.ShouldSyncNow();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldSyncNow_WithRecentSync_ReturnsFalse()
    {
        // Arrange
        var account = new EmailAccount("user@example.com", EmailProvider.Gmail, Guid.NewGuid());
        account.UpdateSyncStatus(SyncStatus.Completed);

        // Act
        var result = account.ShouldSyncNow();

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
