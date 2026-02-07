using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Emails.Queries.GetEmails;
using MIC.Core.Domain.Entities;
using DomainEmailAttachment = MIC.Core.Domain.Entities.EmailAttachment;
using NSubstitute;
using Xunit;

namespace MIC.Tests.Unit.Features.Email;

public class GetEmailsQueryHandlerTests
{
    private readonly GetEmailsQueryHandler _sut;
    private readonly IEmailRepository _emailRepository;

    public GetEmailsQueryHandlerTests()
    {
        _emailRepository = Substitute.For<IEmailRepository>();
        _sut = new GetEmailsQueryHandler(_emailRepository);
    }

    [Fact]
    public async Task Handle_WithFiltersAndSearch_ReturnsMatchingEmailDtos()
    {
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var matchingEmail = CreateEmail(
            userId,
            accountId,
            category: EmailCategory.Project,
            priority: EmailPriority.High,
            isFlagged: true,
            requiresResponse: true,
            hasActionItems: true,
            subject: "Status Update",
            fromName: "Project Bot",
            bodyText: "Status update without keyword",
            summary: "Progress update",
            keywords: new List<string> { "delta launch" });
        matchingEmail.SetCopyRecipients("team@example.com", null);
        matchingEmail.SetHtmlBody("<p>Status update</p>");
        matchingEmail.SetThreadInfo("conv-123", null);
        var attachment = new DomainEmailAttachment(
            fileName: "report.pdf",
            contentType: "application/pdf",
            sizeInBytes: 2048,
            storagePath: "/tmp/report.pdf",
            emailMessageId: matchingEmail.Id);
        matchingEmail.AddAttachment(attachment);

        var otherEmail = CreateEmail(
            userId,
            accountId,
            category: EmailCategory.FYI,
            priority: EmailPriority.Low,
            isFlagged: false,
            requiresResponse: false,
            hasActionItems: false,
            subject: "Weekly Newsletter",
            fromName: "News Bot",
            bodyText: "Newsletter content",
            summary: "Weekly news",
            keywords: new List<string> { "newsletter" });

        _emailRepository
            .GetEmailsAsync(userId, accountId, EmailFolder.Inbox, true, 5, 10, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<EmailMessage>>(new List<EmailMessage> { matchingEmail, otherEmail }));

        var query = new GetEmailsQuery
        {
            UserId = userId,
            EmailAccountId = accountId,
            Folder = EmailFolder.Inbox,
            IsUnread = true,
            Category = EmailCategory.Project,
            Priority = EmailPriority.High,
            IsFlagged = true,
            RequiresResponse = true,
            SearchText = "delta",
            Skip = 5,
            Take = 10
        };

        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().ContainSingle();
        var dto = result.Value.Single();
        dto.Id.Should().Be(matchingEmail.Id);
        dto.Subject.Should().Be("Status Update");
        dto.CcRecipients.Should().Be("team@example.com");
        dto.AICategory.Should().Be(EmailCategory.Project);
        dto.AIPriority.Should().Be(EmailPriority.High);
        dto.RequiresResponse.Should().BeTrue();
        dto.IsFlagged.Should().BeTrue();
        dto.Attachments.Should().HaveCount(1);
        dto.Attachments[0].FileName.Should().Be("report.pdf");
        dto.Attachments[0].Type.Should().Be(AttachmentType.PDF);
        dto.Attachments[0].SizeInBytes.Should().Be(2048);

        await _emailRepository.Received(1).GetEmailsAsync(
            query.UserId,
            query.EmailAccountId,
            query.Folder,
            query.IsUnread,
            query.Skip,
            query.Take,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithSearchMatchingBodyPreview_ReturnsEmail()
    {
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var email = CreateEmail(
            userId,
            accountId,
            category: EmailCategory.Action,
            priority: EmailPriority.High,
            isFlagged: false,
            requiresResponse: true,
            hasActionItems: true,
            subject: "Follow up",
            fromName: "Client",
            bodyText: "Please review the proposal preview details.",
            summary: "Client follow up",
            keywords: new List<string> { "proposal" });

        _emailRepository
            .GetEmailsAsync(userId, accountId, null, null, 0, 50, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<EmailMessage>>(new List<EmailMessage> { email }));

        var query = new GetEmailsQuery
        {
            UserId = userId,
            EmailAccountId = accountId,
            SearchText = "preview"
        };

        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(1);
        result.Value[0].Subject.Should().Be("Follow up");
        result.Value[0].BodyPreview.Should().Contain("preview");
    }

    [Fact]
    public async Task Handle_WithRequiresResponseFilter_ReturnsOnlyMatchingEmails()
    {
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var requiresResponse = CreateEmail(
            userId,
            accountId,
            category: EmailCategory.Meeting,
            priority: EmailPriority.High,
            isFlagged: false,
            requiresResponse: true,
            hasActionItems: true,
            subject: "Decision Needed",
            fromName: "Leader",
            bodyText: "Need approval",
            summary: "Approve request",
            keywords: new List<string> { "approval" });

        var noResponse = CreateEmail(
            userId,
            accountId,
            category: EmailCategory.Meeting,
            priority: EmailPriority.High,
            isFlagged: false,
            requiresResponse: false,
            hasActionItems: false,
            subject: "FYI",
            fromName: "Leader",
            bodyText: "For your information",
            summary: "FYI message",
            keywords: new List<string> { "fyi" });

        _emailRepository
            .GetEmailsAsync(userId, accountId, null, null, 0, 50, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<EmailMessage>>(new List<EmailMessage> { requiresResponse, noResponse }));

        var query = new GetEmailsQuery
        {
            UserId = userId,
            EmailAccountId = accountId,
            RequiresResponse = true
        };

        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().ContainSingle();
        result.Value[0].RequiresResponse.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithSearchMatchingKeywords_ReturnsEmail()
    {
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var keywordMatch = CreateEmail(
            userId,
            accountId,
            category: EmailCategory.Project,
            priority: EmailPriority.Normal,
            isFlagged: false,
            requiresResponse: false,
            hasActionItems: false,
            subject: "Roadmap",
            fromName: "PM",
            bodyText: "Roadmap details",
            summary: "Roadmap update",
            keywords: new List<string> { "milestone" });

        var other = CreateEmail(
            userId,
            accountId,
            category: EmailCategory.Project,
            priority: EmailPriority.Normal,
            isFlagged: false,
            requiresResponse: false,
            hasActionItems: false,
            subject: "Notes",
            fromName: "PM",
            bodyText: "Notes",
            summary: "General",
            keywords: new List<string> { "notes" });

        _emailRepository
            .GetEmailsAsync(userId, accountId, null, null, 0, 50, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<EmailMessage>>(new List<EmailMessage> { keywordMatch, other }));

        var query = new GetEmailsQuery
        {
            UserId = userId,
            EmailAccountId = accountId,
            SearchText = "mile"
        };

        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().ContainSingle();
        result.Value[0].ExtractedKeywords.Should().Contain(k => k.Contains("mile", StringComparison.OrdinalIgnoreCase));
    }

    private static EmailMessage CreateEmail(
        Guid userId,
        Guid accountId,
        EmailCategory category,
        EmailPriority priority,
        bool isFlagged,
        bool requiresResponse,
        bool hasActionItems,
        string subject,
        string fromName,
        string bodyText,
        string summary,
        List<string> keywords)
    {
        var email = new EmailMessage(
            messageId: $"msg-{Guid.NewGuid()}",
            subject: subject,
            fromAddress: "sender@example.com",
            fromName: fromName,
            toRecipients: "recipient@example.com",
            sentDate: DateTime.UtcNow.AddHours(-2),
            receivedDate: DateTime.UtcNow.AddHours(-1),
            bodyText: bodyText,
            userId: userId,
            emailAccountId: accountId,
            folder: EmailFolder.Inbox);

        email.SetAIAnalysis(
            priority: priority,
            category: category,
            sentiment: SentimentType.Positive,
            hasActionItems: hasActionItems,
            requiresResponse: requiresResponse,
            summary: summary,
            keywords: keywords,
            actionItems: hasActionItems ? new List<string> { "Follow up" } : new List<string>(),
            confidenceScore: 0.9);

        if (isFlagged)
        {
            email.ToggleFlag();
        }

        return email;
    }
}
