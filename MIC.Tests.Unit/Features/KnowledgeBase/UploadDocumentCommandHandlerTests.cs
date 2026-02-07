using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.KnowledgeBase.Commands.UploadDocument;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace MIC.Tests.Unit.Features.KnowledgeBase;

public class UploadDocumentCommandHandlerTests
{
    private readonly UploadDocumentCommandHandler _sut;
    private readonly IKnowledgeBaseService _knowledgeBaseService;
    private readonly ILogger<UploadDocumentCommandHandler> _logger;

    public UploadDocumentCommandHandlerTests()
    {
        _knowledgeBaseService = Substitute.For<IKnowledgeBaseService>();
        _logger = Substitute.For<ILogger<UploadDocumentCommandHandler>>();
        _sut = new UploadDocumentCommandHandler(_knowledgeBaseService, _logger);
    }

    [Fact]
    public async Task Handle_WithLargeTextDocument_TruncatesPreviewAndStoresFullContent()
    {
        var longText = new string('a', 600);
        KnowledgeEntry? capturedEntry = null;

        _knowledgeBaseService
            .CreateEntryAsync(Arg.Do<KnowledgeEntry>(entry => capturedEntry = entry), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new KnowledgeEntry()));

        var command = new UploadDocumentCommand
        {
            FileName = "report.txt",
            Content = Encoding.UTF8.GetBytes(longText),
            FileSize = 600,
            ContentType = "text/plain",
            UserId = Guid.NewGuid()
        };

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedEntry.Should().NotBeNull();
        capturedEntry!.Title.Should().Be("report.txt");
        capturedEntry.FullContent.Should().Be(longText);
        capturedEntry.Content.Should().HaveLength(503);
        capturedEntry.Content.Should().EndWith("...");
        capturedEntry.Tags.Should().Contain(new[] { "document", "upload", "txt" });
        capturedEntry.FileSize.Should().Be(600);
        capturedEntry.ContentType.Should().Be("text/plain");
        await _knowledgeBaseService.Received(1).CreateEntryAsync(Arg.Any<KnowledgeEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithBinaryDocument_ProvidesPlaceholderContent()
    {
        KnowledgeEntry? capturedEntry = null;

        _knowledgeBaseService
            .CreateEntryAsync(Arg.Do<KnowledgeEntry>(entry => capturedEntry = entry), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new KnowledgeEntry()));

        var command = new UploadDocumentCommand
        {
            FileName = "invoice.pdf",
            Content = new byte[] { 1, 2, 3, 4 },
            FileSize = 4,
            ContentType = "application/pdf",
            UserId = Guid.NewGuid()
        };

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedEntry.Should().NotBeNull();
        capturedEntry!.Content.Should().Contain("Document uploaded: invoice.pdf");
        capturedEntry.FullContent.Should().Contain("invoice.pdf");
        capturedEntry.Content.Should().NotEndWith("...");
        capturedEntry.Tags.Should().Contain("pdf");
    }

    [Fact]
    public async Task Handle_WhenServiceThrows_ReturnsFailure()
    {
        _knowledgeBaseService
            .CreateEntryAsync(Arg.Any<KnowledgeEntry>(), Arg.Any<CancellationToken>())
            .Returns<Task<KnowledgeEntry>>(_ => throw new InvalidOperationException("store failed"));

        var command = new UploadDocumentCommand
        {
            FileName = "bad.txt",
            Content = Encoding.UTF8.GetBytes("content"),
            FileSize = 7,
            ContentType = "text/plain",
            UserId = Guid.NewGuid()
        };

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("store failed");
    }
}
