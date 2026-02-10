using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.KnowledgeBase.Commands.UploadDocument;
using Moq;
using Xunit;

namespace MIC.Tests.Unit.Application.KnowledgeBase;

/// <summary>
/// Comprehensive tests for UploadDocumentCommandHandler.
/// Tests document upload processing, content extraction, and knowledge base integration.
/// Target: 10 tests for document upload coverage
/// </summary>
public class UploadDocumentHandlerTests
{
    private readonly Mock<IKnowledgeBaseService> _mockKnowledgeBaseService;
    private readonly Mock<ILogger<UploadDocumentCommandHandler>> _mockLogger;
    private readonly Guid _testUserId = Guid.NewGuid();

    public UploadDocumentHandlerTests()
    {
        _mockKnowledgeBaseService = new Mock<IKnowledgeBaseService>();
        _mockLogger = new Mock<ILogger<UploadDocumentCommandHandler>>();
    }

    [Fact]
    public async Task UploadDocument_WithValidTextFile_ReturnsSuccess()
    {
        // Arrange
        var content = Encoding.UTF8.GetBytes("This is a test document content.");
        var command = new UploadDocumentCommand
        {
            FileName = "test.txt",
            Content = content,
            FileSize = content.Length,
            ContentType = "text/plain",
            UserId = _testUserId
        };

        KnowledgeEntry? capturedEntry = null;
        _mockKnowledgeBaseService.Setup(x => x.CreateEntryAsync(It.IsAny<KnowledgeEntry>(), It.IsAny<CancellationToken>()))
            .Callback<KnowledgeEntry, CancellationToken>((entry, _) => capturedEntry = entry)
            .ReturnsAsync((KnowledgeEntry entry, CancellationToken _) => entry);

        var handler = new UploadDocumentCommandHandler(_mockKnowledgeBaseService.Object, _mockLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedEntry.Should().NotBeNull();
        capturedEntry!.Title.Should().Be("test.txt");
        capturedEntry.FullContent.Should().Be("This is a test document content.");
        capturedEntry.Content.Should().Be("This is a test document content.");
        capturedEntry.SourceType.Should().Be("Document");
        capturedEntry.UserId.Should().Be(_testUserId);
        capturedEntry.Tags.Should().Contain("txt");
        capturedEntry.FilePath.Should().Be("test.txt");
        capturedEntry.FileSize.Should().Be(content.Length);
        capturedEntry.ContentType.Should().Be("text/plain");
        _mockKnowledgeBaseService.Verify(x => x.CreateEntryAsync(It.IsAny<KnowledgeEntry>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadDocument_WithMarkdownFile_ExtractsTextContent()
    {
        // Arrange
        var markdownContent = "# Test Header\n\nThis is *markdown* content.";
        var content = Encoding.UTF8.GetBytes(markdownContent);
        var command = new UploadDocumentCommand
        {
            FileName = "readme.md",
            Content = content,
            FileSize = content.Length,
            ContentType = "text/markdown",
            UserId = _testUserId
        };

        KnowledgeEntry? capturedEntry = null;
        _mockKnowledgeBaseService.Setup(x => x.CreateEntryAsync(It.IsAny<KnowledgeEntry>(), It.IsAny<CancellationToken>()))
            .Callback<KnowledgeEntry, CancellationToken>((entry, _) => capturedEntry = entry)
            .ReturnsAsync((KnowledgeEntry entry, CancellationToken _) => entry);

        var handler = new UploadDocumentCommandHandler(_mockKnowledgeBaseService.Object, _mockLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedEntry!.FullContent.Should().Be(markdownContent);
        capturedEntry.Tags.Should().Contain("md");
    }

    [Fact]
    public async Task UploadDocument_WithCSVFile_ExtractsTextContent()
    {
        // Arrange
        var csvContent = "Name,Age,Email\nJohn,30,john@test.com\nJane,25,jane@test.com";
        var content = Encoding.UTF8.GetBytes(csvContent);
        var command = new UploadDocumentCommand
        {
            FileName = "data.csv",
            Content = content,
            FileSize = content.Length,
            ContentType = "text/csv",
            UserId = _testUserId
        };

        KnowledgeEntry? capturedEntry = null;
        _mockKnowledgeBaseService.Setup(x => x.CreateEntryAsync(It.IsAny<KnowledgeEntry>(), It.IsAny<CancellationToken>()))
            .Callback<KnowledgeEntry, CancellationToken>((entry, _) => capturedEntry = entry)
            .ReturnsAsync((KnowledgeEntry entry, CancellationToken _) => entry);

        var handler = new UploadDocumentCommandHandler(_mockKnowledgeBaseService.Object, _mockLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedEntry!.FullContent.Should().Be(csvContent);
        capturedEntry.Tags.Should().Contain("csv");
    }

    [Fact]
    public async Task UploadDocument_WithBinaryFile_CreatesPlaceholderContent()
    {
        // Arrange
        var content = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG signature
        var command = new UploadDocumentCommand
        {
            FileName = "image.png",
            Content = content,
            FileSize = content.Length,
            ContentType = "image/png",
            UserId = _testUserId
        };

        KnowledgeEntry? capturedEntry = null;
        _mockKnowledgeBaseService.Setup(x => x.CreateEntryAsync(It.IsAny<KnowledgeEntry>(), It.IsAny<CancellationToken>()))
            .Callback<KnowledgeEntry, CancellationToken>((entry, _) => capturedEntry = entry)
            .ReturnsAsync((KnowledgeEntry entry, CancellationToken _) => entry);

        var handler = new UploadDocumentCommandHandler(_mockKnowledgeBaseService.Object, _mockLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedEntry!.FullContent.Should().Contain("Document uploaded: image.png");
        capturedEntry.FullContent.Should().Contain("image/png");
        capturedEntry.FullContent.Should().Contain($"{content.Length} bytes");
        capturedEntry.Tags.Should().Contain("png");
    }

    [Fact]
    public async Task UploadDocument_WithLargeContent_TruncatesPreviewContent()
    {
        // Arrange
        var longText = new string('A', 600); // 600 characters
        var content = Encoding.UTF8.GetBytes(longText);
        var command = new UploadDocumentCommand
        {
            FileName = "large.txt",
            Content = content,
            FileSize = content.Length,
            ContentType = "text/plain",
            UserId = _testUserId
        };

        KnowledgeEntry? capturedEntry = null;
        _mockKnowledgeBaseService.Setup(x => x.CreateEntryAsync(It.IsAny<KnowledgeEntry>(), It.IsAny<CancellationToken>()))
            .Callback<KnowledgeEntry, CancellationToken>((entry, _) => capturedEntry = entry)
            .ReturnsAsync((KnowledgeEntry entry, CancellationToken _) => entry);

        var handler = new UploadDocumentCommandHandler(_mockKnowledgeBaseService.Object, _mockLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedEntry!.FullContent.Should().HaveLength(600);
        capturedEntry.Content.Should().HaveLength(503); // 500 + "..."
        capturedEntry.Content.Should().EndWith("...");
        capturedEntry.Content.Should().StartWith("AAA");
    }

    [Fact]
    public async Task UploadDocument_WithShortContent_DoesNotTruncate()
    {
        // Arrange
        var shortText = new string('B', 100); // 100 characters
        var content = Encoding.UTF8.GetBytes(shortText);
        var command = new UploadDocumentCommand
        {
            FileName = "short.txt",
            Content = content,
            FileSize = content.Length,
            ContentType = "text/plain",
            UserId = _testUserId
        };

        KnowledgeEntry? capturedEntry = null;
        _mockKnowledgeBaseService.Setup(x => x.CreateEntryAsync(It.IsAny<KnowledgeEntry>(), It.IsAny<CancellationToken>()))
            .Callback<KnowledgeEntry, CancellationToken>((entry, _) => capturedEntry = entry)
            .ReturnsAsync((KnowledgeEntry entry, CancellationToken _) => entry);

        var handler = new UploadDocumentCommandHandler(_mockKnowledgeBaseService.Object, _mockLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedEntry!.Content.Should().HaveLength(100);
        capturedEntry.Content.Should().NotEndWith("...");
        capturedEntry.Content.Should().Be(capturedEntry.FullContent);
    }

    [Fact]
    public async Task UploadDocument_SetsCorrectTags()
    {
        // Arrange
        var content = Encoding.UTF8.GetBytes("Test document");
        var command = new UploadDocumentCommand
        {
            FileName = "document.pdf",
            Content = content,
            FileSize = content.Length,
            ContentType = "application/pdf",
            UserId = _testUserId
        };

        KnowledgeEntry? capturedEntry = null;
        _mockKnowledgeBaseService.Setup(x => x.CreateEntryAsync(It.IsAny<KnowledgeEntry>(), It.IsAny<CancellationToken>()))
            .Callback<KnowledgeEntry, CancellationToken>((entry, _) => capturedEntry = entry)
            .ReturnsAsync((KnowledgeEntry entry, CancellationToken _) => entry);

        var handler = new UploadDocumentCommandHandler(_mockKnowledgeBaseService.Object, _mockLogger.Object);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        capturedEntry!.Tags.Should().Contain("document");
        capturedEntry.Tags.Should().Contain("upload");
        capturedEntry.Tags.Should().Contain("pdf");
        capturedEntry.Tags.Should().HaveCount(3);
    }

    [Fact]
    public async Task UploadDocument_AssignsNewSourceId()
    {
        // Arrange
        var content = Encoding.UTF8.GetBytes("Test");
        var command = new UploadDocumentCommand
        {
            FileName = "test.txt",
            Content = content,
            FileSize = content.Length,
            ContentType = "text/plain",
            UserId = _testUserId
        };

        KnowledgeEntry? capturedEntry = null;
        _mockKnowledgeBaseService.Setup(x => x.CreateEntryAsync(It.IsAny<KnowledgeEntry>(), It.IsAny<CancellationToken>()))
            .Callback<KnowledgeEntry, CancellationToken>((entry, _) => capturedEntry = entry)
            .ReturnsAsync((KnowledgeEntry entry, CancellationToken _) => entry);

        var handler = new UploadDocumentCommandHandler(_mockKnowledgeBaseService.Object, _mockLogger.Object);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        capturedEntry!.SourceId.Should().NotBe(Guid.Empty);
        capturedEntry.SourceType.Should().Be("Document");
    }

    [Fact]
    public async Task UploadDocument_WhenServiceFails_ReturnsFailure()
    {
        // Arrange
        var content = Encoding.UTF8.GetBytes("Test");
        var command = new UploadDocumentCommand
        {
            FileName = "test.txt",
            Content = content,
            FileSize = content.Length,
            ContentType = "text/plain",
            UserId = _testUserId
        };

        _mockKnowledgeBaseService.Setup(x => x.CreateEntryAsync(It.IsAny<KnowledgeEntry>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        var handler = new UploadDocumentCommandHandler(_mockKnowledgeBaseService.Object, _mockLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Upload failed");
        result.Error.Should().Contain("Database connection failed");
    }

    [Fact]
    public async Task UploadDocument_WithFileNameExtensionOnly_ExtractsExtension()
    {
        // Arrange
        var content = Encoding.UTF8.GetBytes("Test");
        var command = new UploadDocumentCommand
        {
            FileName = ".gitignore",
            Content = content,
            FileSize = content.Length,
            ContentType = "text/plain",
            UserId = _testUserId
        };

        KnowledgeEntry? capturedEntry = null;
        _mockKnowledgeBaseService.Setup(x => x.CreateEntryAsync(It.IsAny<KnowledgeEntry>(), It.IsAny<CancellationToken>()))
            .Callback<KnowledgeEntry, CancellationToken>((entry, _) => capturedEntry = entry)
            .ReturnsAsync((KnowledgeEntry entry, CancellationToken _) => entry);

        var handler = new UploadDocumentCommandHandler(_mockKnowledgeBaseService.Object, _mockLogger.Object);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        capturedEntry!.Tags.Should().Contain("gitignore");
        capturedEntry.Title.Should().Be(".gitignore");
    }
}
