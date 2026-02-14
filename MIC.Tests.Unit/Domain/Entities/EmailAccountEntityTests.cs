using System;
using System.Collections.Generic;
using FluentAssertions;
using MIC.Core.Domain.Entities;
using Xunit;

namespace MIC.Tests.Unit.Domain.Entities;

/// <summary>
/// Comprehensive tests for <see cref="EmailAccount"/> entity covering constructor,
/// token management, IMAP credentials, sync status, settings, and statistics.
/// </summary>
public class EmailAccountEntityTests
{
    private static readonly Guid ValidUserId = Guid.NewGuid();

    private static EmailAccount CreateAccount(
        string email = "test@example.com",
        EmailProvider provider = EmailProvider.Gmail,
        string? displayName = null)
    {
        return new EmailAccount(email, provider, ValidUserId, displayName);
    }

    #region Constructor

    [Fact]
    public void Constructor_WithValidInputs_SetsProperties()
    {
        var account = CreateAccount("user@test.com", EmailProvider.Outlook, "My Outlook");

        account.EmailAddress.Should().Be("user@test.com");
        account.Provider.Should().Be(EmailProvider.Outlook);
        account.UserId.Should().Be(ValidUserId);
        account.DisplayName.Should().Be("My Outlook");
        account.IsActive.Should().BeTrue();
        account.IsPrimary.Should().BeTrue();
        account.Status.Should().Be(SyncStatus.NotStarted);
    }

    [Fact]
    public void Constructor_WithoutDisplayName_UsesEmailAsDisplayName()
    {
        var account = CreateAccount("user@test.com");
        account.DisplayName.Should().Be("user@test.com");
    }

    [Fact]
    public void Constructor_SetsDefaultSyncSettings()
    {
        var account = CreateAccount();
        account.SyncIntervalMinutes.Should().Be(5);
        account.InitialSyncDays.Should().Be(365);
        account.SyncAttachments.Should().BeTrue();
        account.MaxAttachmentSizeMB.Should().Be(25);
    }

    [Fact]
    public void Constructor_WithNullEmail_ThrowsArgumentException()
    {
        var act = () => new EmailAccount(null!, EmailProvider.Gmail, ValidUserId);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithEmptyEmail_ThrowsArgumentException()
    {
        var act = () => new EmailAccount("", EmailProvider.Gmail, ValidUserId);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithWhitespaceEmail_ThrowsArgumentException()
    {
        var act = () => new EmailAccount("   ", EmailProvider.Gmail, ValidUserId);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithDefaultUserId_ThrowsArgumentException()
    {
        var act = () => new EmailAccount("test@test.com", EmailProvider.Gmail, Guid.Empty);
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region SetTokens

    [Fact]
    public void SetTokens_SetsAllTokenProperties()
    {
        var account = CreateAccount();
        var expiresAt = DateTime.UtcNow.AddHours(1);

        account.SetTokens("access-token", "refresh-token", expiresAt, "Mail.Read");

        account.AccessTokenEncrypted.Should().Be("access-token");
        account.RefreshTokenEncrypted.Should().Be("refresh-token");
        account.TokenExpiresAt.Should().Be(expiresAt);
        account.GrantedScopes.Should().Be("Mail.Read");
    }

    [Fact]
    public void SetTokens_ClearsErrors()
    {
        var account = CreateAccount();
        account.SetSyncFailed("Error1");
        account.ConsecutiveFailures.Should().BeGreaterThan(0);

        account.SetTokens("token", null, DateTime.UtcNow.AddHours(1));

        account.LastSyncError.Should().BeNull();
        account.ConsecutiveFailures.Should().Be(0);
    }

    [Fact]
    public void SetTokens_WithNullRefreshToken_IsAllowed()
    {
        var account = CreateAccount();
        account.SetTokens("access", null, DateTime.UtcNow.AddHours(1));

        account.AccessTokenEncrypted.Should().Be("access");
        account.RefreshTokenEncrypted.Should().BeNull();
    }

    #endregion

    #region SetImapSmtpCredentials

    [Fact]
    public void SetImapSmtpCredentials_SetsAllProperties()
    {
        var account = CreateAccount("user@imap.com", EmailProvider.Gmail);

        account.SetImapSmtpCredentials("imap.host.com", 993, "smtp.host.com", 587, true, "password123");

        account.ImapServer.Should().Be("imap.host.com");
        account.ImapPort.Should().Be(993);
        account.SmtpServer.Should().Be("smtp.host.com");
        account.SmtpPort.Should().Be(587);
        account.UseSsl.Should().BeTrue();
        account.PasswordEncrypted.Should().Be("password123");
        account.Provider.Should().Be(EmailProvider.IMAP);
    }

    [Fact]
    public void SetImapSmtpCredentials_ClearsErrors()
    {
        var account = CreateAccount();
        account.SetSyncFailed("Prev error");

        account.SetImapSmtpCredentials("imap.host.com", 993, "smtp.host.com", 587, true, "pass");

        account.LastSyncError.Should().BeNull();
        account.ConsecutiveFailures.Should().Be(0);
    }

    [Fact]
    public void SetImapSmtpCredentials_NullImapServer_Throws()
    {
        var account = CreateAccount();
        var act = () => account.SetImapSmtpCredentials(null!, 993, "smtp.host.com", 587, true, "pass");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetImapSmtpCredentials_NullSmtpServer_Throws()
    {
        var account = CreateAccount();
        var act = () => account.SetImapSmtpCredentials("imap.host.com", 993, null!, 587, true, "pass");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetImapSmtpCredentials_NullPassword_Throws()
    {
        var account = CreateAccount();
        var act = () => account.SetImapSmtpCredentials("imap.host.com", 993, "smtp.host.com", 587, true, null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetImapSmtpCredentials_InvalidImapPort_Throws()
    {
        var account = CreateAccount();
        var act = () => account.SetImapSmtpCredentials("imap.host.com", 0, "smtp.host.com", 587, true, "pass");
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void SetImapSmtpCredentials_InvalidSmtpPort_Throws()
    {
        var account = CreateAccount();
        var act = () => account.SetImapSmtpCredentials("imap.host.com", 993, "smtp.host.com", 70000, true, "pass");
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region IsTokenExpired

    [Fact]
    public void IsTokenExpired_NoExpirySet_ReturnsTrue()
    {
        var account = CreateAccount();
        account.IsTokenExpired().Should().BeTrue();
    }

    [Fact]
    public void IsTokenExpired_TokenNotExpired_ReturnsFalse()
    {
        var account = CreateAccount();
        account.SetTokens("token", null, DateTime.UtcNow.AddHours(1));

        account.IsTokenExpired().Should().BeFalse();
    }

    [Fact]
    public void IsTokenExpired_TokenExpiredPastBuffer_ReturnsTrue()
    {
        var account = CreateAccount();
        account.SetTokens("token", null, DateTime.UtcNow.AddMinutes(-1));

        account.IsTokenExpired().Should().BeTrue();
    }

    [Fact]
    public void IsTokenExpired_WithinDefaultBuffer_ReturnsTrue()
    {
        var account = CreateAccount();
        // Token expires in 3 minutes, default buffer is 5 minutes
        account.SetTokens("token", null, DateTime.UtcNow.AddMinutes(3));

        account.IsTokenExpired().Should().BeTrue();
    }

    [Fact]
    public void IsTokenExpired_CustomBuffer_RespectsBuffer()
    {
        var account = CreateAccount();
        account.SetTokens("token", null, DateTime.UtcNow.AddMinutes(3));

        // With 1-minute buffer, should NOT be expired
        account.IsTokenExpired(TimeSpan.FromMinutes(1)).Should().BeFalse();
    }

    #endregion

    #region UpdateSyncStatus

    [Fact]
    public void UpdateSyncStatus_Completed_UpdatesCountsAndTime()
    {
        var account = CreateAccount();
        var syncTime = DateTime.UtcNow;

        account.UpdateSyncStatus(SyncStatus.Completed, 10, 5, syncTime);

        account.Status.Should().Be(SyncStatus.Completed);
        account.TotalEmailsSynced.Should().Be(10);
        account.TotalAttachmentsSynced.Should().Be(5);
        account.LastSyncedAt.Should().Be(syncTime);
        account.LastSyncError.Should().BeNull();
        account.ConsecutiveFailures.Should().Be(0);
    }

    [Fact]
    public void UpdateSyncStatus_Completed_AccumulatesCounts()
    {
        var account = CreateAccount();
        account.UpdateSyncStatus(SyncStatus.Completed, 10, 5);
        account.UpdateSyncStatus(SyncStatus.Completed, 20, 3);

        account.TotalEmailsSynced.Should().Be(30);
        account.TotalAttachmentsSynced.Should().Be(8);
    }

    [Fact]
    public void UpdateSyncStatus_NotCompleted_DoesNotUpdateCounts()
    {
        var account = CreateAccount();
        account.UpdateSyncStatus(SyncStatus.InProgress, 10, 5);

        account.TotalEmailsSynced.Should().Be(0);
        account.TotalAttachmentsSynced.Should().Be(0);
        account.LastSyncedAt.Should().BeNull();
    }

    [Fact]
    public void UpdateSyncStatus_SetsLastSyncAttempt()
    {
        var account = CreateAccount();
        var before = DateTime.UtcNow;

        account.UpdateSyncStatus(SyncStatus.InProgress);

        account.LastSyncAttemptAt.Should().NotBeNull();
        account.LastSyncAttemptAt!.Value.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void UpdateSyncStatus_Completed_ClearsConsecutiveFailures()
    {
        var account = CreateAccount();
        account.SetSyncFailed("err1");
        account.SetSyncFailed("err2");
        account.ConsecutiveFailures.Should().Be(2);

        account.UpdateSyncStatus(SyncStatus.Completed, 1);

        account.ConsecutiveFailures.Should().Be(0);
        account.LastSyncError.Should().BeNull();
    }

    #endregion

    #region SetSyncFailed

    [Fact]
    public void SetSyncFailed_IncrementsConsecutiveFailures()
    {
        var account = CreateAccount();
        account.SetSyncFailed("Error 1");
        account.ConsecutiveFailures.Should().Be(1);

        account.SetSyncFailed("Error 2");
        account.ConsecutiveFailures.Should().Be(2);
    }

    [Fact]
    public void SetSyncFailed_SetsStatusAndError()
    {
        var account = CreateAccount();
        account.SetSyncFailed("Connection timeout");

        account.Status.Should().Be(SyncStatus.Failed);
        account.LastSyncError.Should().Be("Connection timeout");
    }

    [Fact]
    public void SetSyncFailed_After5Failures_DeactivatesAccount()
    {
        var account = CreateAccount();

        for (int i = 0; i < 5; i++)
            account.SetSyncFailed($"Error {i + 1}");

        account.IsActive.Should().BeFalse();
        account.ConsecutiveFailures.Should().Be(5);
    }

    [Fact]
    public void SetSyncFailed_4Failures_StillActive()
    {
        var account = CreateAccount();

        for (int i = 0; i < 4; i++)
            account.SetSyncFailed("error");

        account.IsActive.Should().BeTrue();
        account.ConsecutiveFailures.Should().Be(4);
    }

    #endregion

    #region DeltaLink and HistoryId

    [Fact]
    public void SetDeltaLink_SetsProperty()
    {
        var account = CreateAccount();
        account.SetDeltaLink("delta-link-value");
        account.DeltaLink.Should().Be("delta-link-value");
    }

    [Fact]
    public void SetHistoryId_SetsProperty()
    {
        var account = CreateAccount();
        account.SetHistoryId("history-123");
        account.HistoryId.Should().Be("history-123");
    }

    #endregion

    #region UpdateSyncSettings

    [Fact]
    public void UpdateSyncSettings_SetsInterval()
    {
        var account = CreateAccount();
        account.UpdateSyncSettings(syncIntervalMinutes: 30);
        account.SyncIntervalMinutes.Should().Be(30);
    }

    [Fact]
    public void UpdateSyncSettings_ClampsIntervalMin()
    {
        var account = CreateAccount();
        account.UpdateSyncSettings(syncIntervalMinutes: 0);
        account.SyncIntervalMinutes.Should().Be(1);
    }

    [Fact]
    public void UpdateSyncSettings_ClampsIntervalMax()
    {
        var account = CreateAccount();
        account.UpdateSyncSettings(syncIntervalMinutes: 5000);
        account.SyncIntervalMinutes.Should().Be(1440);
    }

    [Fact]
    public void UpdateSyncSettings_SetsInitialSyncDays()
    {
        var account = CreateAccount();
        account.UpdateSyncSettings(initialSyncDays: 90);
        account.InitialSyncDays.Should().Be(90);
    }

    [Fact]
    public void UpdateSyncSettings_ClampsInitialSyncDaysMin()
    {
        var account = CreateAccount();
        account.UpdateSyncSettings(initialSyncDays: -10);
        account.InitialSyncDays.Should().Be(1);
    }

    [Fact]
    public void UpdateSyncSettings_ClampsInitialSyncDaysMax()
    {
        var account = CreateAccount();
        account.UpdateSyncSettings(initialSyncDays: 10000);
        account.InitialSyncDays.Should().Be(3650);
    }

    [Fact]
    public void UpdateSyncSettings_SetsSyncAttachments()
    {
        var account = CreateAccount();
        account.UpdateSyncSettings(syncAttachments: false);
        account.SyncAttachments.Should().BeFalse();
    }

    [Fact]
    public void UpdateSyncSettings_SetsMaxAttachmentSize()
    {
        var account = CreateAccount();
        account.UpdateSyncSettings(maxAttachmentSizeMB: 50);
        account.MaxAttachmentSizeMB.Should().Be(50);
    }

    [Fact]
    public void UpdateSyncSettings_ClampsMaxAttachmentSizeMin()
    {
        var account = CreateAccount();
        account.UpdateSyncSettings(maxAttachmentSizeMB: 0);
        account.MaxAttachmentSizeMB.Should().Be(1);
    }

    [Fact]
    public void UpdateSyncSettings_ClampsMaxAttachmentSizeMax()
    {
        var account = CreateAccount();
        account.UpdateSyncSettings(maxAttachmentSizeMB: 500);
        account.MaxAttachmentSizeMB.Should().Be(100);
    }

    [Fact]
    public void UpdateSyncSettings_SetsFoldersToSync()
    {
        var account = CreateAccount();
        var folders = new List<string> { "INBOX", "Sent" };
        account.UpdateSyncSettings(foldersToSync: folders);
        account.FoldersToSync.Should().BeEquivalentTo(folders);
    }

    [Fact]
    public void UpdateSyncSettings_NullValues_DoNotChange()
    {
        var account = CreateAccount();
        var originalInterval = account.SyncIntervalMinutes;
        var originalDays = account.InitialSyncDays;

        account.UpdateSyncSettings(); // all nulls

        account.SyncIntervalMinutes.Should().Be(originalInterval);
        account.InitialSyncDays.Should().Be(originalDays);
    }

    #endregion

    #region UpdateStatistics

    [Fact]
    public void UpdateStatistics_SetsAllValues()
    {
        var account = CreateAccount();
        account.UpdateStatistics(1024 * 1024 * 500, 42, 7);

        account.StorageUsedBytes.Should().Be(1024 * 1024 * 500);
        account.UnreadCount.Should().Be(42);
        account.RequiresResponseCount.Should().Be(7);
    }

    #endregion

    #region Activate / Deactivate / Primary

    [Fact]
    public void Activate_SetsActiveAndClearsErrors()
    {
        var account = CreateAccount();
        account.Deactivate();
        account.SetSyncFailed("error");

        account.Activate();

        account.IsActive.Should().BeTrue();
        account.ConsecutiveFailures.Should().Be(0);
        account.LastSyncError.Should().BeNull();
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var account = CreateAccount();
        account.Deactivate();
        account.IsActive.Should().BeFalse();
    }

    [Fact]
    public void SetAsPrimary_SetsPrimary()
    {
        var account = CreateAccount();
        account.RemovePrimary();
        account.IsPrimary.Should().BeFalse();

        account.SetAsPrimary();
        account.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void RemovePrimary_ClearsPrimary()
    {
        var account = CreateAccount();
        account.IsPrimary.Should().BeTrue();

        account.RemovePrimary();
        account.IsPrimary.Should().BeFalse();
    }

    #endregion

    #region GetFormattedStorageUsed

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1048576, "1 MB")]
    [InlineData(1073741824, "1 GB")]
    public void GetFormattedStorageUsed_FormatsCorrectly(long bytes, string expected)
    {
        var account = CreateAccount();
        account.UpdateStatistics(bytes, 0, 0);
        account.GetFormattedStorageUsed().Should().Be(expected);
    }

    [Fact]
    public void GetFormattedStorageUsed_LargeGB_FormatsCorrectly()
    {
        var account = CreateAccount();
        account.UpdateStatistics(5L * 1024 * 1024 * 1024, 0, 0);
        account.GetFormattedStorageUsed().Should().Be("5 GB");
    }

    #endregion

    #region ShouldSyncNow

    [Fact]
    public void ShouldSyncNow_Inactive_ReturnsFalse()
    {
        var account = CreateAccount();
        account.Deactivate();
        account.ShouldSyncNow().Should().BeFalse();
    }

    [Fact]
    public void ShouldSyncNow_InProgress_ReturnsFalse()
    {
        var account = CreateAccount();
        account.UpdateSyncStatus(SyncStatus.InProgress);
        account.ShouldSyncNow().Should().BeFalse();
    }

    [Fact]
    public void ShouldSyncNow_NeverSynced_ReturnsTrue()
    {
        var account = CreateAccount();
        account.ShouldSyncNow().Should().BeTrue();
    }

    [Fact]
    public void ShouldSyncNow_IntervalElapsed_ReturnsTrue()
    {
        var account = CreateAccount();
        // Complete a sync, then set LastSyncedAt far in the past
        account.UpdateSyncStatus(SyncStatus.Completed, 1, 0, DateTime.UtcNow.AddMinutes(-10));
        // Default interval is 5 min, last sync 10 min ago
        account.ShouldSyncNow().Should().BeTrue();
    }

    [Fact]
    public void ShouldSyncNow_IntervalNotElapsed_ReturnsFalse()
    {
        var account = CreateAccount();
        account.UpdateSyncStatus(SyncStatus.Completed, 1, 0, DateTime.UtcNow);
        // Just synced, should not sync again immediately
        account.ShouldSyncNow().Should().BeFalse();
    }

    #endregion

    #region Enum Coverage

    [Theory]
    [InlineData(EmailProvider.Outlook)]
    [InlineData(EmailProvider.Gmail)]
    [InlineData(EmailProvider.Exchange)]
    [InlineData(EmailProvider.IMAP)]
    public void Constructor_AcceptsAllProviders(EmailProvider provider)
    {
        var account = new EmailAccount("test@test.com", provider, ValidUserId);
        account.Provider.Should().Be(provider);
    }

    [Theory]
    [InlineData(SyncStatus.NotStarted)]
    [InlineData(SyncStatus.InProgress)]
    [InlineData(SyncStatus.Completed)]
    [InlineData(SyncStatus.Failed)]
    [InlineData(SyncStatus.Paused)]
    [InlineData(SyncStatus.Cancelled)]
    public void UpdateSyncStatus_AcceptsAllStatuses(SyncStatus status)
    {
        var account = CreateAccount();
        account.UpdateSyncStatus(status);
        account.Status.Should().Be(status);
    }

    #endregion
}
