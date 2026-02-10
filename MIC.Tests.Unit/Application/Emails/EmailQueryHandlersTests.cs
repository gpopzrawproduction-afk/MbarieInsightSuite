using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using FluentAssertions;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Emails.Queries.GetEmailById;
using MIC.Core.Application.Emails.Queries.GetEmails;
using MIC.Core.Domain.Entities;
using Moq;
using Xunit;

namespace MIC.Tests.Unit.Application.Emails;

/// <summary>
/// Basic tests for Email CQRS query handlers.
/// Tests email retrieval and DTO mapping.
/// Target: 5 tests for basic email query coverage
/// </summary>
public class EmailQueryHandlersTests
{
    private readonly Mock<IEmailRepository> _mockRepository;
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _testAccountId = Guid.NewGuid();

    public EmailQueryHandlersTests()
    {
        _mockRepository = new Mock<IEmailRepository>();
    }

    #region GetEmailByIdQueryHandler Tests (3 tests)

    [Fact]
    public async Task GetEmailById_WithValidId_ReturnsEmailDto()
    {
        // Arrange
        var emailMessage = CreateTestEmail("Test Subject", "test@example.com");
        var query = new GetEmailByIdQuery(emailMessage.Id);

        _mockRepository.Setup(x => x.GetByIdAsync(emailMessage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emailMessage);

        var handler = new GetEmailByIdQueryHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(emailMessage.Id);
        result.Value.Subject.Should().Be("Test Subject");
        result.Value.FromAddress.Should().Be("test@example.com");
        _mockRepository.Verify(x => x.GetByIdAsync(emailMessage.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetEmailById_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var query = new GetEmailByIdQuery(nonExistentId);

        _mockRepository.Setup(x => x.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailMessage?)null);

        var handler = new GetEmailByIdQueryHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetEmailById_MapsAllPropertiesCorrectly()
    {
        // Arrange
        var emailMessage = CreateTestEmail("Important Meeting", "boss@company.com", "Boss Name");
        var query = new GetEmailByIdQuery(emailMessage.Id);

        _mockRepository.Setup(x => x.GetByIdAsync(emailMessage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emailMessage);

        var handler = new GetEmailByIdQueryHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.Should().NotBeNull();
        result.Value!.MessageId.Should().Be(emailMessage.MessageId);
        result.Value.Subject.Should().Be("Important Meeting");
        result.Value.FromAddress.Should().Be("boss@company.com");
        result.Value.FromName.Should().Be("Boss Name");
        result.Value.ToRecipients.Should().Be("recipient@test.com");
        result.Value.EmailAccountId.Should().Be(_testAccountId);
        result.Value.Folder.Should().Be(EmailFolder.Inbox);
    }

    #endregion

    #region GetEmailsQueryHandler Tests (2 tests)

    [Fact]
    public async Task GetEmails_WithNoFilters_ReturnsAllEmails()
    {
        // Arrange
        var emails = new List<EmailMessage>
        {
            CreateTestEmail("Email 1", "sender1@test.com"),
            CreateTestEmail("Email 2", "sender2@test.com"),
            CreateTestEmail("Email 3", "sender3@test.com")
        };

        var query = new GetEmailsQuery { UserId = _testUserId };

        _mockRepository.Setup(x => x.GetEmailsAsync(
            _testUserId,
            null,
            null,
            null,
            0,
            50,
            It.IsAny<CancellationToken>())).ReturnsAsync(emails);

        var handler = new GetEmailsQueryHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(3);
        result.Value[0].Subject.Should().Be("Email 1");
        result.Value[1].Subject.Should().Be("Email 2");
        result.Value[2].Subject.Should().Be("Email 3");
    }

    [Fact]
    public async Task GetEmails_WithSearchText_FiltersResults()
    {
        // Arrange
        var email1 = CreateTestEmail("Important Meeting Tomorrow", "sender1@test.com");
        var email2 = CreateTestEmail("Project Update", "sender2@test.com");
        var email3 = CreateTestEmail("Meeting Notes", "sender3@test.com");

        var emails = new List<EmailMessage> { email1, email2, email3 };
        var query = new GetEmailsQuery
        {
            UserId = _testUserId,
            SearchText = "meeting"
        };

        _mockRepository.Setup(x => x.GetEmailsAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid?>(),
            It.IsAny<EmailFolder?>(),
            It.IsAny<bool?>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(emails);

        var handler = new GetEmailsQueryHandler(_mockRepository.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.Should().HaveCount(2);
        result.Value.Should().AllSatisfy(e =>
            e.Subject.ToLowerInvariant().Should().Contain("meeting"));
    }

    #endregion

    #region Helper Methods

    private EmailMessage CreateTestEmail(string subject, string fromAddress, string fromName = "Test Sender")
    {
        return new EmailMessage(
            messageId: $"msg-{Guid.NewGuid()}",
            subject: subject,
            fromAddress: fromAddress,
            fromName: fromName,
            toRecipients: "recipient@test.com",
            sentDate: DateTime.UtcNow.AddHours(-1),
            receivedDate: DateTime.UtcNow,
            bodyText: $"This is the body of {subject}",
            userId: _testUserId,
            emailAccountId: _testAccountId,
            folder: EmailFolder.Inbox
        );
    }

    #endregion
}
