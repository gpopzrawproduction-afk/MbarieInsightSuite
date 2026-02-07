using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Emails.Common;
using MIC.Core.Application.Emails.Queries.GetEmailById;
using MIC.Core.Domain.Entities;
using DomainEmailAttachment = MIC.Core.Domain.Entities.EmailAttachment;
using NSubstitute;
using Xunit;

namespace MIC.Tests.Unit.Features.Email;

public class GetEmailByIdQueryHandlerTests
{
    private readonly GetEmailByIdQueryHandler _sut;
    private readonly IEmailRepository _emailRepository;

    public GetEmailByIdQueryHandlerTests()
    {
        _emailRepository = Substitute.For<IEmailRepository>();
        _sut = new GetEmailByIdQueryHandler(_emailRepository);
    }

    [Fact]
    public async Task Handle_WhenEmailExists_ReturnsMappedDto()
    {
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var email = CreateEmail(userId, accountId);

        _emailRepository
            .GetByIdAsync(email.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<EmailMessage?>(email));

        var query = new GetEmailByIdQuery(email.Id);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        var dto = result.Value!;
        dto.Id.Should().Be(email.Id);
        dto.Subject.Should().Be(email.Subject);
        dto.FromName.Should().Be(email.FromName);
        dto.CcRecipients.Should().Be("cc@example.com");
        dto.HasAttachments.Should().BeTrue();
        dto.Attachments.Should().HaveCount(1);
        dto.Attachments[0].FileName.Should().Be("contract.pdf");
        dto.Attachments[0].AISummary.Should().Be("Contract summary");
        dto.AIPriority.Should().Be(email.AIPriority);
        dto.AICategory.Should().Be(email.AICategory);
        dto.RequiresResponse.Should().Be(email.RequiresResponse);
    }

    [Fact]
    public async Task Handle_WhenEmailMissing_ReturnsNull()
    {
        var emailId = Guid.NewGuid();

        _emailRepository
            .GetByIdAsync(emailId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<EmailMessage?>(null));

        var query = new GetEmailByIdQuery(emailId);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().BeNull();
    }

    private static EmailMessage CreateEmail(Guid userId, Guid accountId)
    {
        var email = new EmailMessage(
            messageId: $"msg-{Guid.NewGuid()}",
            subject: "Contract Review",
            fromAddress: "legal@example.com",
            fromName: "Legal Team",
            toRecipients: "executive@example.com",
            sentDate: DateTime.UtcNow.AddHours(-5),
            receivedDate: DateTime.UtcNow.AddHours(-4),
            bodyText: "Please review the attached contract.",
            userId: userId,
            emailAccountId: accountId,
            folder: EmailFolder.Inbox);

        email.SetCopyRecipients("cc@example.com", null);
        email.SetHtmlBody("<p>Please review the attached contract.</p>");
        email.SetThreadInfo("conv-contract", null);

        email.SetAIAnalysis(
            priority: EmailPriority.High,
            category: EmailCategory.Decision,
            sentiment: SentimentType.Neutral,
            hasActionItems: true,
            requiresResponse: true,
            summary: "Review contract changes",
            keywords: new List<string> { "contract", "review" },
            actionItems: new List<string> { "Approve changes" },
            confidenceScore: 0.85);

        var attachment = new DomainEmailAttachment(
            fileName: "contract.pdf",
            contentType: "application/pdf",
            sizeInBytes: 4096,
            storagePath: "/tmp/contract.pdf",
            emailMessageId: email.Id);
        attachment.SetAIAnalysis(
            summary: "Contract summary",
            keywords: new List<string> { "obligations" },
            category: DocumentCategory.Contract,
            confidence: 0.9);
        email.AddAttachment(attachment);

        return email;
    }
}
