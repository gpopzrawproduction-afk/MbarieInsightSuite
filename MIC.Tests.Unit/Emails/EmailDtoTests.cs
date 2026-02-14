using System;
using System.Collections.Generic;
using FluentAssertions;
using MIC.Core.Application.Emails.Common;
using MIC.Core.Domain.Entities;
using Xunit;

namespace MIC.Tests.Unit.Emails;

/// <summary>
/// Tests for EmailDto, EmailAttachmentDto, and EmailAccountDto computed display properties.
/// </summary>
public class EmailDtoTests
{
    #region EmailDto.SenderDisplay

    [Fact]
    public void SenderDisplay_ReturnsFromName_WhenPresent()
    {
        var dto = new EmailDto { FromName = "John Doe", FromAddress = "john@test.com" };
        dto.SenderDisplay.Should().Be("John Doe");
    }

    [Fact]
    public void SenderDisplay_ReturnsFromAddress_WhenNameEmpty()
    {
        var dto = new EmailDto { FromName = "", FromAddress = "john@test.com" };
        dto.SenderDisplay.Should().Be("john@test.com");
    }

    [Fact]
    public void SenderDisplay_ReturnsFromAddress_WhenNameNull()
    {
        var dto = new EmailDto { FromAddress = "john@test.com" };
        dto.SenderDisplay.Should().Be("john@test.com");
    }

    #endregion

    #region EmailDto.TimeAgo

    [Fact]
    public void TimeAgo_ReturnsJustNow_ForRecentDate()
    {
        var dto = new EmailDto { ReceivedDate = DateTime.UtcNow };
        dto.TimeAgo.Should().Be("Just now");
    }

    [Fact]
    public void TimeAgo_ReturnsMinutesAgo()
    {
        var dto = new EmailDto { ReceivedDate = DateTime.UtcNow.AddMinutes(-5) };
        dto.TimeAgo.Should().Be("5m ago");
    }

    [Fact]
    public void TimeAgo_ReturnsHoursAgo()
    {
        var dto = new EmailDto { ReceivedDate = DateTime.UtcNow.AddHours(-3) };
        dto.TimeAgo.Should().Be("3h ago");
    }

    [Fact]
    public void TimeAgo_ReturnsDaysAgo()
    {
        var dto = new EmailDto { ReceivedDate = DateTime.UtcNow.AddDays(-2) };
        dto.TimeAgo.Should().Be("2d ago");
    }

    [Fact]
    public void TimeAgo_ReturnsWeeksAgo()
    {
        var dto = new EmailDto { ReceivedDate = DateTime.UtcNow.AddDays(-14) };
        dto.TimeAgo.Should().Be("2w ago");
    }

    [Fact]
    public void TimeAgo_ReturnsFormattedDate_ForOldEmails()
    {
        var date = DateTime.UtcNow.AddDays(-60);
        var dto = new EmailDto { ReceivedDate = date };
        dto.TimeAgo.Should().Be(date.ToString("MMM dd"));
    }

    #endregion

    #region EmailDto.PriorityColor

    [Theory]
    [InlineData(EmailPriority.Urgent, "#FF0055")]
    [InlineData(EmailPriority.High, "#FF6B00")]
    [InlineData(EmailPriority.Normal, "#00E5FF")]
    [InlineData(EmailPriority.Low, "#607D8B")]
    public void PriorityColor_ReturnsCorrectColor(EmailPriority priority, string expectedColor)
    {
        var dto = new EmailDto { AIPriority = priority };
        dto.PriorityColor.Should().Be(expectedColor);
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
    [InlineData(EmailCategory.General)]
    public void CategoryIcon_ReturnsNonEmpty(EmailCategory category)
    {
        var dto = new EmailDto { AICategory = category };
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
    public void SentimentIcon_ReturnsNonEmpty(SentimentType sentiment)
    {
        var dto = new EmailDto { Sentiment = sentiment };
        dto.SentimentIcon.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region EmailDto Defaults

    [Fact]
    public void EmailDto_HasCorrectDefaults()
    {
        var dto = new EmailDto();
        dto.Subject.Should().BeEmpty();
        dto.FromAddress.Should().BeEmpty();
        dto.BodyText.Should().BeEmpty();
        dto.ExtractedKeywords.Should().NotBeNull().And.BeEmpty();
        dto.ActionItems.Should().NotBeNull().And.BeEmpty();
        dto.Attachments.Should().NotBeNull().And.BeEmpty();
    }

    #endregion
}

/// <summary>
/// Tests for EmailAttachmentDto computed properties.
/// </summary>
public class EmailAttachmentDtoTests
{
    #region FormattedSize

    [Fact]
    public void FormattedSize_ReturnsBytes_ForSmallFiles()
    {
        var dto = new EmailAttachmentDto { SizeInBytes = 500 };
        dto.FormattedSize.Should().Be("500 B");
    }

    [Fact]
    public void FormattedSize_ReturnsKB()
    {
        var dto = new EmailAttachmentDto { SizeInBytes = 1024 };
        dto.FormattedSize.Should().Be("1 KB");
    }

    [Fact]
    public void FormattedSize_ReturnsMB()
    {
        var dto = new EmailAttachmentDto { SizeInBytes = 1024 * 1024 };
        dto.FormattedSize.Should().Be("1 MB");
    }

    [Fact]
    public void FormattedSize_ReturnsGB()
    {
        var dto = new EmailAttachmentDto { SizeInBytes = (long)1024 * 1024 * 1024 };
        dto.FormattedSize.Should().Be("1 GB");
    }

    [Fact]
    public void FormattedSize_HandlesZero()
    {
        var dto = new EmailAttachmentDto { SizeInBytes = 0 };
        dto.FormattedSize.Should().Be("0 B");
    }

    [Fact]
    public void FormattedSize_HandlesFractionalKB()
    {
        var dto = new EmailAttachmentDto { SizeInBytes = 1536 }; // 1.5 KB
        dto.FormattedSize.Should().Be("1.5 KB");
    }

    #endregion

    #region TypeIcon

    [Theory]
    [InlineData(AttachmentType.PDF)]
    [InlineData(AttachmentType.Word)]
    [InlineData(AttachmentType.Excel)]
    [InlineData(AttachmentType.PowerPoint)]
    [InlineData(AttachmentType.Image)]
    [InlineData(AttachmentType.Archive)]
    public void TypeIcon_ReturnsNonEmpty(AttachmentType type)
    {
        var dto = new EmailAttachmentDto { Type = type };
        dto.TypeIcon.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Defaults

    [Fact]
    public void EmailAttachmentDto_HasCorrectDefaults()
    {
        var dto = new EmailAttachmentDto();
        dto.FileName.Should().BeEmpty();
        dto.ContentType.Should().BeEmpty();
        dto.SizeInBytes.Should().Be(0);
    }

    #endregion
}

/// <summary>
/// Tests for EmailAccountDto computed properties.
/// </summary>
public class EmailAccountDtoTests
{
    #region ProviderName

    [Theory]
    [InlineData(EmailProvider.Outlook, "Microsoft 365")]
    [InlineData(EmailProvider.Gmail, "Gmail")]
    [InlineData(EmailProvider.Exchange, "Exchange")]
    [InlineData(EmailProvider.IMAP, "IMAP")]
    public void ProviderName_ReturnsCorrectName(EmailProvider provider, string expected)
    {
        var dto = new EmailAccountDto { Provider = provider };
        dto.ProviderName.Should().Be(expected);
    }

    #endregion

    #region StatusText

    [Theory]
    [InlineData(SyncStatus.NotStarted, "Not synced")]
    [InlineData(SyncStatus.InProgress, "Syncing...")]
    [InlineData(SyncStatus.Completed, "Up to date")]
    [InlineData(SyncStatus.Failed, "Sync failed")]
    [InlineData(SyncStatus.Paused, "Paused")]
    public void StatusText_ReturnsCorrectText(SyncStatus status, string expected)
    {
        var dto = new EmailAccountDto { Status = status };
        dto.StatusText.Should().Be(expected);
    }

    #endregion

    #region StatusColor

    [Theory]
    [InlineData(SyncStatus.Completed, "#39FF14")]
    [InlineData(SyncStatus.InProgress, "#00E5FF")]
    [InlineData(SyncStatus.Failed, "#FF0055")]
    [InlineData(SyncStatus.Paused, "#FF6B00")]
    [InlineData(SyncStatus.NotStarted, "#607D8B")]
    public void StatusColor_ReturnsCorrectColor(SyncStatus status, string expected)
    {
        var dto = new EmailAccountDto { Status = status };
        dto.StatusColor.Should().Be(expected);
    }

    #endregion

    #region Defaults

    [Fact]
    public void EmailAccountDto_HasCorrectDefaults()
    {
        var dto = new EmailAccountDto();
        dto.EmailAddress.Should().BeEmpty();
        dto.IsActive.Should().BeFalse();
        dto.IsPrimary.Should().BeFalse();
    }

    #endregion
}
