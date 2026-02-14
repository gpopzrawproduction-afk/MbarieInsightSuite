using System;
using System.Collections.Generic;
using FluentAssertions;
using MIC.Core.Domain.Entities;
using Xunit;

namespace MIC.Tests.Unit.Domain.Entities;

/// <summary>
/// Tests for EmailMessage domain entity covering validation and state transitions.
/// Target: 8 tests for critical behaviors.
/// </summary>
public class EmailMessageEntityTests
{
    private static EmailMessage CreateEmail(
        string subject = "Quarterly Update",
        Guid? userId = null,
        Guid? accountId = null,
        DateTime? sentDate = null,
        EmailFolder folder = EmailFolder.Inbox)
    {
        return new EmailMessage(
            messageId: Guid.NewGuid().ToString(),
            subject: subject,
            fromAddress: "sender@example.com",
            fromName: "Sender",
            toRecipients: "recipient@example.com",
            sentDate: sentDate ?? DateTime.UtcNow.AddMinutes(-15),
            receivedDate: DateTime.UtcNow,
            bodyText: "Hello team, here is the latest.",
            userId: userId ?? Guid.NewGuid(),
            emailAccountId: accountId ?? Guid.NewGuid(),
            folder: folder);
    }

    [Fact]
    public void Constructor_WithValidInputs_SetsCoreProperties()
    {
        // Act
        var email = CreateEmail();

        // Assert
        email.Subject.Should().Be("Quarterly Update");
        email.FromAddress.Should().Be("sender@example.com");
        email.ToRecipients.Should().Be("recipient@example.com");
        email.Folder.Should().Be(EmailFolder.Inbox);
        email.AIPriority.Should().Be(EmailPriority.Normal);
        email.AICategory.Should().Be(EmailCategory.General);
        email.Sentiment.Should().Be(SentimentType.Neutral);
        email.Importance.Should().Be(EmailImportance.Normal);
    }

    [Fact]
    public void Constructor_WithNullSubject_UsesPlaceholder()
    {
        // Act
        var email = CreateEmail(subject: null!);

        // Assert
        email.Subject.Should().Be("(No Subject)");
        email.FromName.Should().Be("Sender");
        email.BodyPreview.Should().StartWith("Hello team");
    }

    [Fact]
    public void SetAIAnalysis_WithRequiresResponseHighPriority_SetsState()
    {
        // Arrange
        var email = CreateEmail();
        var keywords = new List<string> { "budget", "q1" };
        var actions = new List<string> { "Approve budget" };

        // Act
        email.SetAIAnalysis(
            EmailPriority.High,
            EmailCategory.Project,
            SentimentType.Positive,
            hasActionItems: true,
            requiresResponse: true,
            summary: "Budget approval needed",
            keywords: keywords,
            actionItems: actions,
            confidenceScore: 0.92);

        // Assert
        email.AIPriority.Should().Be(EmailPriority.High);
        email.AICategory.Should().Be(EmailCategory.Project);
        email.Sentiment.Should().Be(SentimentType.Positive);
        email.ContainsActionItems.Should().BeTrue();
        email.RequiresResponse.Should().BeTrue();
        email.AISummary.Should().Be("Budget approval needed");
        email.AIConfidenceScore.Should().Be(0.92);
        email.IsAIProcessed.Should().BeTrue();
        email.AIProcessedAt.Should().NotBeNull();
        email.SuggestedResponseBy.Should().BeCloseTo(DateTime.UtcNow.AddHours(24), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void SetAIAnalysis_WithCustomLists_ReplacesCollections()
    {
        // Arrange
        var email = CreateEmail();
        email.SetAIAnalysis(
            EmailPriority.Low,
            EmailCategory.General,
            SentimentType.Neutral,
            hasActionItems: false,
            requiresResponse: false,
            summary: null);

        var keywords = new List<string> { "follow-up" };
        var actions = new List<string> { "Schedule meeting" };

        // Act
        email.SetAIAnalysis(
            EmailPriority.Normal,
            EmailCategory.Report,
            SentimentType.Neutral,
            hasActionItems: true,
            requiresResponse: false,
            summary: "Follow-up required",
            keywords: keywords,
            actionItems: actions,
            confidenceScore: 0.75);

        // Assert
        email.ExtractedKeywords.Should().BeSameAs(keywords);
        email.ActionItems.Should().BeSameAs(actions);
        email.ContainsActionItems.Should().BeTrue();
        email.RequiresResponse.Should().BeFalse();
    }

    [Fact]
    public void SetInboxFlags_WithUrgentRead_SetsInboxState()
    {
        // Arrange
        var email = CreateEmail();

        // Act
        email.SetInboxFlags(EmailPriority.Urgent, isUrgent: true, isRead: true, requiresResponse: true, containsActionItems: true);

        // Assert
        email.Priority.Should().Be(EmailPriority.Urgent);
        email.IsUrgent.Should().BeTrue();
        email.IsRead.Should().BeTrue();
        email.RequiresResponse.Should().BeTrue();
        email.ContainsActionItems.Should().BeTrue();
    }

    [Fact]
    public void MarkAsReadAndUnread_ToggleState()
    {
        // Arrange
        var email = CreateEmail();

        // Act
        email.MarkAsRead();
        email.IsRead.Should().BeTrue();

        email.MarkAsUnread();

        // Assert
        email.IsRead.Should().BeFalse();
    }

    [Fact]
    public void AddAttachment_WithValidAttachment_AddsAndFlags()
    {
        // Arrange
        var email = CreateEmail();
        var attachment = new EmailAttachment(
            fileName: "report.pdf",
            contentType: "application/pdf",
            sizeInBytes: 1024,
            storagePath: "/attachments/report.pdf",
            emailMessageId: Guid.NewGuid());

        // Act
        email.AddAttachment(attachment);

        // Assert
        email.HasAttachments.Should().BeTrue();
        email.Attachments.Should().ContainSingle().Which.Should().Be(attachment);
    }

    [Fact]
    public void SetThreadInfoAndCopyRecipients_UpdateMetadata()
    {
        // Arrange
        var email = CreateEmail();

        // Act
        email.SetThreadInfo("conv-123", "reply-456");
        email.SetCopyRecipients("cc@example.com", "  ");

        // Assert
        email.ConversationId.Should().Be("conv-123");
        email.InReplyTo.Should().Be("reply-456");
        email.CcRecipients.Should().Be("cc@example.com");
        email.BccRecipients.Should().BeNull();
    }
}
