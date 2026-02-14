using System;
using System.Collections.Generic;
using FluentAssertions;
using MIC.Core.Domain.Entities;
using Xunit;

namespace MIC.Tests.Unit.Domain.Entities;

/// <summary>
/// Extended tests for <see cref="EmailMessage"/> entity covering gaps:
/// ToggleFlag, MoveToFolder, SetHtmlBody, LinkToKnowledgeBase,
/// AI priority → deadline branches, confidence clamping, constructor guards, BodyPreview truncation.
/// </summary>
public class EmailMessageEntityExtendedTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid AccountId = Guid.NewGuid();

    private static EmailMessage CreateMessage(
        string subject = "Test Subject",
        string body = "Test body content",
        EmailFolder folder = EmailFolder.Inbox)
    {
        return new EmailMessage(
            messageId: $"msg-{Guid.NewGuid():N}",
            subject: subject,
            fromAddress: "sender@test.com",
            fromName: "Sender Name",
            toRecipients: "recipient@test.com",
            sentDate: DateTime.UtcNow.AddMinutes(-5),
            receivedDate: DateTime.UtcNow,
            bodyText: body,
            userId: UserId,
            emailAccountId: AccountId,
            folder: folder
        );
    }

    #region Constructor Guards

    [Fact]
    public void Constructor_NullMessageId_Throws()
    {
        var act = () => new EmailMessage(null!, "Subject", "from@test.com", "From",
            "to@test.com", DateTime.UtcNow, DateTime.UtcNow, "body", UserId, AccountId);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_EmptyFromAddress_Throws()
    {
        var act = () => new EmailMessage("msg-1", "Subject", "", "From",
            "to@test.com", DateTime.UtcNow, DateTime.UtcNow, "body", UserId, AccountId);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_EmptyToRecipients_Throws()
    {
        var act = () => new EmailMessage("msg-1", "Subject", "from@test.com", "From",
            "", DateTime.UtcNow, DateTime.UtcNow, "body", UserId, AccountId);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_DefaultUserId_Throws()
    {
        var act = () => new EmailMessage("msg-1", "Subject", "from@test.com", "From",
            "to@test.com", DateTime.UtcNow, DateTime.UtcNow, "body", Guid.Empty, AccountId);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_DefaultEmailAccountId_Throws()
    {
        var act = () => new EmailMessage("msg-1", "Subject", "from@test.com", "From",
            "to@test.com", DateTime.UtcNow, DateTime.UtcNow, "body", UserId, Guid.Empty);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_NullSubject_UsesPlaceholder()
    {
        var msg = new EmailMessage("msg-1", null!, "from@test.com", "From",
            "to@test.com", DateTime.UtcNow, DateTime.UtcNow, "body", UserId, AccountId);
        msg.Subject.Should().Be("(No Subject)");
    }

    [Fact]
    public void Constructor_NullFromName_UsesFromAddress()
    {
        var msg = new EmailMessage("msg-1", "Subject", "from@test.com", null!,
            "to@test.com", DateTime.UtcNow, DateTime.UtcNow, "body", UserId, AccountId);
        msg.FromName.Should().Be("from@test.com");
    }

    [Fact]
    public void Constructor_NullBodyText_SetsEmpty()
    {
        var msg = new EmailMessage("msg-1", "Subject", "from@test.com", "From",
            "to@test.com", DateTime.UtcNow, DateTime.UtcNow, null!, UserId, AccountId);
        msg.BodyText.Should().Be(string.Empty);
    }

    #endregion

    #region BodyPreview Truncation

    [Fact]
    public void Constructor_ShortBody_PreviewEqualsBody()
    {
        var msg = CreateMessage(body: "Short body");
        msg.BodyPreview.Should().Be("Short body");
    }

    [Fact]
    public void Constructor_LongBody_PreviewTruncatedTo200()
    {
        var longBody = new string('A', 300);
        var msg = CreateMessage(body: longBody);

        msg.BodyPreview.Should().HaveLength(203); // 200 + "..."
        msg.BodyPreview.Should().EndWith("...");
    }

    [Fact]
    public void Constructor_Exactly200Body_NoTruncation()
    {
        var body = new string('B', 200);
        var msg = CreateMessage(body: body);
        msg.BodyPreview.Should().Be(body);
        msg.BodyPreview.Should().HaveLength(200);
    }

    #endregion

    #region ToggleFlag

    [Fact]
    public void ToggleFlag_FlagsUnflaggedEmail()
    {
        var msg = CreateMessage();
        msg.IsFlagged.Should().BeFalse();

        msg.ToggleFlag();

        msg.IsFlagged.Should().BeTrue();
    }

    [Fact]
    public void ToggleFlag_UnflagsFlaggedEmail()
    {
        var msg = CreateMessage();
        msg.ToggleFlag(); // flag
        msg.ToggleFlag(); // unflag

        msg.IsFlagged.Should().BeFalse();
    }

    #endregion

    #region MoveToFolder

    [Theory]
    [InlineData(EmailFolder.Sent)]
    [InlineData(EmailFolder.Drafts)]
    [InlineData(EmailFolder.Archive)]
    [InlineData(EmailFolder.Junk)]
    [InlineData(EmailFolder.Trash)]
    [InlineData(EmailFolder.Custom)]
    public void MoveToFolder_SetsNewFolder(EmailFolder target)
    {
        var msg = CreateMessage();
        msg.Folder.Should().Be(EmailFolder.Inbox);

        msg.MoveToFolder(target);

        msg.Folder.Should().Be(target);
    }

    #endregion

    #region SetHtmlBody

    [Fact]
    public void SetHtmlBody_SetsHtmlContent()
    {
        var msg = CreateMessage();
        msg.SetHtmlBody("<p>Hello</p>");
        msg.BodyHtml.Should().Be("<p>Hello</p>");
    }

    [Fact]
    public void SetHtmlBody_NullClearsHtml()
    {
        var msg = CreateMessage();
        msg.SetHtmlBody("<p>Hello</p>");
        msg.SetHtmlBody(null);
        msg.BodyHtml.Should().BeNull();
    }

    #endregion

    #region LinkToKnowledgeBase

    [Fact]
    public void LinkToKnowledgeBase_SetsId()
    {
        var msg = CreateMessage();
        var kbId = Guid.NewGuid();

        msg.LinkToKnowledgeBase(kbId);

        msg.KnowledgeEntryId.Should().Be(kbId);
    }

    #endregion

    #region SetAIAnalysis – Priority → Deadline Branches

    [Fact]
    public void SetAIAnalysis_UrgentPriority_SetsDeadlineTo2Hours()
    {
        var msg = CreateMessage();
        var before = DateTime.UtcNow;

        msg.SetAIAnalysis(EmailPriority.Urgent, EmailCategory.Action, SentimentType.Neutral,
            false, true, "Summary");

        msg.SuggestedResponseBy.Should().NotBeNull();
        msg.SuggestedResponseBy!.Value.Should().BeCloseTo(before.AddHours(2), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void SetAIAnalysis_HighPriority_SetsDeadlineTo24Hours()
    {
        var msg = CreateMessage();
        var before = DateTime.UtcNow;

        msg.SetAIAnalysis(EmailPriority.High, EmailCategory.Action, SentimentType.Neutral,
            false, true, "Summary");

        msg.SuggestedResponseBy!.Value.Should().BeCloseTo(before.AddHours(24), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void SetAIAnalysis_NormalPriority_SetsDeadlineTo3Days()
    {
        var msg = CreateMessage();
        var before = DateTime.UtcNow;

        msg.SetAIAnalysis(EmailPriority.Normal, EmailCategory.General, SentimentType.Neutral,
            false, true, "Summary");

        msg.SuggestedResponseBy!.Value.Should().BeCloseTo(before.AddDays(3), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void SetAIAnalysis_LowPriority_SetsDeadlineTo7Days()
    {
        var msg = CreateMessage();
        var before = DateTime.UtcNow;

        msg.SetAIAnalysis(EmailPriority.Low, EmailCategory.FYI, SentimentType.Neutral,
            false, true, "Summary");

        msg.SuggestedResponseBy!.Value.Should().BeCloseTo(before.AddDays(7), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void SetAIAnalysis_NoResponseRequired_NoDeadline()
    {
        var msg = CreateMessage();

        msg.SetAIAnalysis(EmailPriority.High, EmailCategory.FYI, SentimentType.Neutral,
            false, false, "Summary");

        msg.SuggestedResponseBy.Should().BeNull();
    }

    #endregion

    #region SetAIAnalysis – Confidence Clamping

    [Fact]
    public void SetAIAnalysis_ConfidenceAbove1_ClampedTo1()
    {
        var msg = CreateMessage();
        msg.SetAIAnalysis(EmailPriority.Normal, EmailCategory.General, SentimentType.Neutral,
            false, false, "Summary", confidenceScore: 1.5);
        msg.AIConfidenceScore.Should().Be(1.0);
    }

    [Fact]
    public void SetAIAnalysis_ConfidenceBelow0_ClampedTo0()
    {
        var msg = CreateMessage();
        msg.SetAIAnalysis(EmailPriority.Normal, EmailCategory.General, SentimentType.Neutral,
            false, false, "Summary", confidenceScore: -0.5);
        msg.AIConfidenceScore.Should().Be(0.0);
    }

    [Fact]
    public void SetAIAnalysis_ValidConfidence_Preserved()
    {
        var msg = CreateMessage();
        msg.SetAIAnalysis(EmailPriority.Normal, EmailCategory.General, SentimentType.Neutral,
            false, false, "Summary", confidenceScore: 0.85);
        msg.AIConfidenceScore.Should().Be(0.85);
    }

    #endregion

    #region SetAIAnalysis – Keywords and ActionItems

    [Fact]
    public void SetAIAnalysis_NullKeywords_PreservesExisting()
    {
        var msg = CreateMessage();
        msg.SetAIAnalysis(EmailPriority.Normal, EmailCategory.General, SentimentType.Neutral,
            false, false, "Summary", keywords: new List<string> { "urgent" });

        msg.SetAIAnalysis(EmailPriority.High, EmailCategory.Action, SentimentType.Positive,
            true, true, "Updated", keywords: null);

        // Original keywords remain since null was passed
        msg.ExtractedKeywords.Should().Contain("urgent");
    }

    [Fact]
    public void SetAIAnalysis_WithKeywords_ReplacesExisting()
    {
        var msg = CreateMessage();
        msg.SetAIAnalysis(EmailPriority.Normal, EmailCategory.General, SentimentType.Neutral,
            false, false, "Summary", keywords: new List<string> { "old" });

        msg.SetAIAnalysis(EmailPriority.High, EmailCategory.Action, SentimentType.Positive,
            true, true, "Updated", keywords: new List<string> { "new1", "new2" });

        msg.ExtractedKeywords.Should().BeEquivalentTo(new[] { "new1", "new2" });
    }

    #endregion

    #region SetInboxFlags

    [Fact]
    public void SetInboxFlags_SetsAllFlags()
    {
        var msg = CreateMessage();

        msg.SetInboxFlags(EmailPriority.Urgent, true, true, true, true);

        msg.Priority.Should().Be(EmailPriority.Urgent);
        msg.IsUrgent.Should().BeTrue();
        msg.IsRead.Should().BeTrue();
        msg.RequiresResponse.Should().BeTrue();
        msg.ContainsActionItems.Should().BeTrue();
    }

    [Fact]
    public void SetInboxFlags_NotRead_DoesNotChangeReadStatus()
    {
        var msg = CreateMessage();
        msg.IsRead.Should().BeFalse();

        msg.SetInboxFlags(EmailPriority.Normal, false, false, false, false);

        // isRead=false should not change the existing read state
        msg.IsRead.Should().BeFalse();
    }

    #endregion

    #region AddAttachment – null guard

    [Fact]
    public void AddAttachment_NullAttachment_Throws()
    {
        var msg = CreateMessage();
        var act = () => msg.AddAttachment(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region SetCopyRecipients edge cases

    [Fact]
    public void SetCopyRecipients_WhitespaceOnly_SetsNull()
    {
        var msg = CreateMessage();
        msg.SetCopyRecipients("   ", "  ");

        msg.CcRecipients.Should().BeNull();
        msg.BccRecipients.Should().BeNull();
    }

    [Fact]
    public void SetCopyRecipients_EmptyString_SetsNull()
    {
        var msg = CreateMessage();
        msg.SetCopyRecipients("", "");

        msg.CcRecipients.Should().BeNull();
        msg.BccRecipients.Should().BeNull();
    }

    [Fact]
    public void SetCopyRecipients_ValidValues_SetsValues()
    {
        var msg = CreateMessage();
        msg.SetCopyRecipients("cc@test.com", "bcc@test.com");

        msg.CcRecipients.Should().Be("cc@test.com");
        msg.BccRecipients.Should().Be("bcc@test.com");
    }

    #endregion

    #region Constructor – folder parameter

    [Fact]
    public void Constructor_WithSpecificFolder_SetsFolder()
    {
        var msg = CreateMessage(folder: EmailFolder.Sent);
        msg.Folder.Should().Be(EmailFolder.Sent);
    }

    [Fact]
    public void Constructor_DefaultFolder_IsInbox()
    {
        var msg = new EmailMessage("msg-1", "Subject", "from@test.com", "From",
            "to@test.com", DateTime.UtcNow, DateTime.UtcNow, "body", UserId, AccountId);
        msg.Folder.Should().Be(EmailFolder.Inbox);
    }

    #endregion

    #region Constructor – default AI properties

    [Fact]
    public void Constructor_DefaultAIProperties()
    {
        var msg = CreateMessage();

        msg.AIPriority.Should().Be(EmailPriority.Normal);
        msg.AICategory.Should().Be(EmailCategory.General);
        msg.Sentiment.Should().Be(SentimentType.Neutral);
        msg.Importance.Should().Be(EmailImportance.Normal);
        msg.IsAIProcessed.Should().BeFalse();
        msg.AIProcessedAt.Should().BeNull();
        msg.ContainsActionItems.Should().BeFalse();
        msg.RequiresResponse.Should().BeFalse();
    }

    #endregion
}
