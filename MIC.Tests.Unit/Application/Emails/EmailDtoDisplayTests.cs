using System;
using System.Collections.Generic;
using FluentAssertions;
using MIC.Core.Application.Emails.Common;
using MIC.Core.Domain.Entities;
using Xunit;

namespace MIC.Tests.Unit.Application.Emails;

/// <summary>
/// Tests for EmailDto, EmailAttachmentDto, EmailAccountDto display computed properties.
/// These cover switch expressions and helper methods for maximal line coverage.
/// </summary>
public class EmailDtoDisplayTests
{
    #region EmailDto.SenderDisplay

    [Fact]
    public void SenderDisplay_WithFromName_ReturnsFromName()
    {
        var dto = new EmailDto { FromName = "John Doe", FromAddress = "john@test.com" };
        dto.SenderDisplay.Should().Be("John Doe");
    }

    [Fact]
    public void SenderDisplay_EmptyFromName_ReturnsFromAddress()
    {
        var dto = new EmailDto { FromName = "", FromAddress = "john@test.com" };
        dto.SenderDisplay.Should().Be("john@test.com");
    }

    #endregion

    #region EmailDto.PriorityColor

    [Theory]
    [InlineData(EmailPriority.Urgent, "#FF0055")]
    [InlineData(EmailPriority.High, "#FF6B00")]
    [InlineData(EmailPriority.Normal, "#00E5FF")]
    [InlineData(EmailPriority.Low, "#607D8B")]
    public void PriorityColor_AllValues(EmailPriority priority, string expected)
    {
        var dto = new EmailDto { AIPriority = priority };
        dto.PriorityColor.Should().Be(expected);
    }

    #endregion

    #region EmailDto.CategoryIcon

    [Theory]
    [InlineData(EmailCategory.Meeting)]
    [InlineData(EmailCategory.Project)]
    [InlineData(EmailCategory.Decision)]
    [InlineData(EmailCategory.Action)]
    [InlineData(EmailCategory.Report)]
    [InlineData(EmailCategory.FYI)]
    [InlineData(EmailCategory.Newsletter)]
    public void CategoryIcon_AllValues_ReturnsNonEmpty(EmailCategory cat)
    {
        var dto = new EmailDto { AICategory = cat };
        dto.CategoryIcon.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region EmailDto.SentimentIcon

    [Theory]
    [InlineData(SentimentType.VeryPositive)]
    [InlineData(SentimentType.Positive)]
    [InlineData(SentimentType.Neutral)]
    [InlineData(SentimentType.Negative)]
    [InlineData(SentimentType.VeryNegative)]
    public void SentimentIcon_AllValues_ReturnsNonEmpty(SentimentType sentiment)
    {
        var dto = new EmailDto { Sentiment = sentiment };
        dto.SentimentIcon.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region EmailDto.TimeAgo

    [Fact]
    public void TimeAgo_JustNow()
    {
        var dto = new EmailDto { ReceivedDate = DateTime.UtcNow };
        dto.TimeAgo.Should().Be("Just now");
    }

    [Fact]
    public void TimeAgo_MinutesAgo()
    {
        var dto = new EmailDto { ReceivedDate = DateTime.UtcNow.AddMinutes(-5) };
        dto.TimeAgo.Should().Be("5m ago");
    }

    [Fact]
    public void TimeAgo_HoursAgo()
    {
        var dto = new EmailDto { ReceivedDate = DateTime.UtcNow.AddHours(-3) };
        dto.TimeAgo.Should().Be("3h ago");
    }

    [Fact]
    public void TimeAgo_DaysAgo()
    {
        var dto = new EmailDto { ReceivedDate = DateTime.UtcNow.AddDays(-2) };
        dto.TimeAgo.Should().Be("2d ago");
    }

    [Fact]
    public void TimeAgo_WeeksAgo()
    {
        var dto = new EmailDto { ReceivedDate = DateTime.UtcNow.AddDays(-14) };
        dto.TimeAgo.Should().Be("2w ago");
    }

    [Fact]
    public void TimeAgo_OlderThanMonth_ShowsDate()
    {
        var date = DateTime.UtcNow.AddDays(-60);
        var dto = new EmailDto { ReceivedDate = date };
        dto.TimeAgo.Should().Be(date.ToString("MMM dd"));
    }

    #endregion

    #region EmailAttachmentDto.FormattedSize

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1048576, "1 MB")]
    [InlineData(1073741824, "1 GB")]
    public void FormattedSize_AllRanges(long size, string expected)
    {
        var dto = new EmailAttachmentDto { SizeInBytes = size };
        dto.FormattedSize.Should().Be(expected);
    }

    #endregion

    #region EmailAttachmentDto.TypeIcon

    [Theory]
    [InlineData(AttachmentType.PDF)]
    [InlineData(AttachmentType.Word)]
    [InlineData(AttachmentType.Excel)]
    [InlineData(AttachmentType.PowerPoint)]
    [InlineData(AttachmentType.Image)]
    [InlineData(AttachmentType.Archive)]
    public void TypeIcon_AllValues_ReturnsNonEmpty(AttachmentType type)
    {
        var dto = new EmailAttachmentDto { Type = type };
        dto.TypeIcon.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region EmailAccountDto.ProviderName

    [Theory]
    [InlineData(EmailProvider.Outlook, "Microsoft 365")]
    [InlineData(EmailProvider.Gmail, "Gmail")]
    [InlineData(EmailProvider.Exchange, "Exchange")]
    [InlineData(EmailProvider.IMAP, "IMAP")]
    public void ProviderName_AllValues(EmailProvider provider, string expected)
    {
        var dto = new EmailAccountDto { Provider = provider };
        dto.ProviderName.Should().Be(expected);
    }

    #endregion

    #region EmailAccountDto.StatusText

    [Theory]
    [InlineData(SyncStatus.NotStarted, "Not synced")]
    [InlineData(SyncStatus.InProgress, "Syncing...")]
    [InlineData(SyncStatus.Completed, "Up to date")]
    [InlineData(SyncStatus.Failed, "Sync failed")]
    [InlineData(SyncStatus.Paused, "Paused")]
    public void StatusText_AllValues(SyncStatus status, string expected)
    {
        var dto = new EmailAccountDto { Status = status };
        dto.StatusText.Should().Be(expected);
    }

    #endregion

    #region EmailAccountDto.StatusColor

    [Theory]
    [InlineData(SyncStatus.Completed, "#39FF14")]
    [InlineData(SyncStatus.InProgress, "#00E5FF")]
    [InlineData(SyncStatus.Failed, "#FF0055")]
    [InlineData(SyncStatus.Paused, "#FF6B00")]
    [InlineData(SyncStatus.NotStarted, "#607D8B")]
    public void StatusColor_AllValues(SyncStatus status, string expected)
    {
        var dto = new EmailAccountDto { Status = status };
        dto.StatusColor.Should().Be(expected);
    }

    #endregion

    #region EmailDto defaults

    [Fact]
    public void EmailDto_DefaultProperties()
    {
        var dto = new EmailDto();
        dto.MessageId.Should().BeEmpty();
        dto.Subject.Should().BeEmpty();
        dto.FromAddress.Should().BeEmpty();
        dto.FromName.Should().BeEmpty();
        dto.ToRecipients.Should().BeEmpty();
        dto.BodyText.Should().BeEmpty();
        dto.IsRead.Should().BeFalse();
        dto.IsFlagged.Should().BeFalse();
        dto.HasAttachments.Should().BeFalse();
        dto.ContainsActionItems.Should().BeFalse();
        dto.RequiresResponse.Should().BeFalse();
        dto.IsAIProcessed.Should().BeFalse();
        dto.ExtractedKeywords.Should().BeEmpty();
        dto.ActionItems.Should().BeEmpty();
        dto.Attachments.Should().BeEmpty();
    }

    #endregion

    #region EmailAttachmentDto defaults

    [Fact]
    public void EmailAttachmentDto_DefaultProperties()
    {
        var dto = new EmailAttachmentDto();
        dto.FileName.Should().BeEmpty();
        dto.ContentType.Should().BeEmpty();
        dto.SizeInBytes.Should().Be(0);
        dto.IsProcessed.Should().BeFalse();
        dto.AISummary.Should().BeNull();
    }

    #endregion

    #region EmailAccountDto defaults

    [Fact]
    public void EmailAccountDto_DefaultProperties()
    {
        var dto = new EmailAccountDto();
        dto.EmailAddress.Should().BeEmpty();
        dto.DisplayName.Should().BeNull();
        dto.IsActive.Should().BeFalse();
        dto.IsPrimary.Should().BeFalse();
        dto.LastSyncedAt.Should().BeNull();
        dto.LastSyncError.Should().BeNull();
    }

    #endregion

    #region Supporting types

    [Fact]
    public void FolderOption_Record()
    {
        var folder = new MIC.Desktop.Avalonia.ViewModels.FolderOption("Inbox", EmailFolder.Inbox, "📥");
        folder.Name.Should().Be("Inbox");
        folder.Folder.Should().Be(EmailFolder.Inbox);
        folder.Icon.Should().Be("📥");
    }

    [Fact]
    public void CategoryOption_Record()
    {
        var cat = new MIC.Desktop.Avalonia.ViewModels.CategoryOption("All", null);
        cat.Name.Should().Be("All");
        cat.Category.Should().BeNull();
    }

    [Fact]
    public void CategoryOption_WithValue()
    {
        var cat = new MIC.Desktop.Avalonia.ViewModels.CategoryOption("Meeting", EmailCategory.Meeting);
        cat.Category.Should().Be(EmailCategory.Meeting);
    }

    [Fact]
    public void PriorityOption_Record()
    {
        var pri = new MIC.Desktop.Avalonia.ViewModels.PriorityOption("All", null);
        pri.Name.Should().Be("All");
        pri.Priority.Should().BeNull();
    }

    [Fact]
    public void PriorityOption_WithValue()
    {
        var pri = new MIC.Desktop.Avalonia.ViewModels.PriorityOption("Urgent", EmailPriority.Urgent);
        pri.Priority.Should().Be(EmailPriority.Urgent);
    }

    #endregion

    #region ChatMessageViewModel

    [Fact]
    public void ChatMessageViewModel_Properties()
    {
        var vm = new MIC.Desktop.Avalonia.ViewModels.ChatMessageViewModel
        {
            Content = "Hello",
            IsUser = true,
            Timestamp = new DateTime(2025, 6, 15, 14, 30, 0)
        };
        vm.Content.Should().Be("Hello");
        vm.IsUser.Should().BeTrue();
        vm.FormattedTime.Should().Be("14:30");
    }

    [Fact]
    public void ChatMessageViewModel_DefaultValues()
    {
        var vm = new MIC.Desktop.Avalonia.ViewModels.ChatMessageViewModel();
        vm.Content.Should().BeEmpty();
        vm.IsUser.Should().BeFalse();
    }

    #endregion
}
