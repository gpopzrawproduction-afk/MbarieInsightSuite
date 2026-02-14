using FluentAssertions;
using MIC.Core.Domain.Entities;
using Xunit;

namespace MIC.Tests.Unit.Domain;

/// <summary>
/// Tests for <see cref="EmailAttachment"/> entity — constructor guards, type determination,
/// content extraction, AI analysis, knowledge base linking, formatted size, and CanExtractText logic.
/// </summary>
public class EmailAttachmentTests
{
    private static readonly Guid ValidEmailId = Guid.NewGuid();

    private static EmailAttachment Create(
        string fileName = "doc.pdf",
        string contentType = "application/pdf",
        long sizeInBytes = 1024,
        string storagePath = "/store/doc.pdf",
        Guid? emailMessageId = null,
        string? externalId = null,
        string? contentHash = null)
    {
        return new EmailAttachment(
            fileName, contentType, sizeInBytes, storagePath,
            emailMessageId ?? ValidEmailId, externalId, contentHash);
    }

    #region Constructor

    [Fact]
    public void Constructor_SetsProperties()
    {
        var att = Create(fileName: "report.pdf", contentType: "application/pdf", sizeInBytes: 2048,
            storagePath: "/s/report.pdf", contentHash: "abc123");
        att.FileName.Should().Be("report.pdf");
        att.ContentType.Should().Be("application/pdf");
        att.SizeInBytes.Should().Be(2048);
        att.StoragePath.Should().Be("/s/report.pdf");
        att.ContentHash.Should().Be("abc123");
        att.Status.Should().Be(ProcessingStatus.Pending);
    }

    [Fact]
    public void Constructor_NullContentHash_DefaultsToEmpty()
    {
        var att = Create(contentHash: null);
        att.ContentHash.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_ThrowsOnNullFileName()
    {
        var act = () => Create(fileName: null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ThrowsOnEmptyFileName()
    {
        var act = () => Create(fileName: "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ThrowsOnNullContentType()
    {
        var act = () => Create(contentType: null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ThrowsOnZeroSize()
    {
        var act = () => Create(sizeInBytes: 0);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ThrowsOnNegativeSize()
    {
        var act = () => Create(sizeInBytes: -1);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ThrowsOnNullStoragePath()
    {
        var act = () => Create(storagePath: null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ThrowsOnDefaultEmailMessageId()
    {
        var act = () => Create(emailMessageId: Guid.Empty);
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region DetermineType (via constructor)

    [Theory]
    [InlineData("file.pdf", "any", AttachmentType.PDF)]
    [InlineData("file.docx", "any", AttachmentType.Word)]
    [InlineData("file.doc", "any", AttachmentType.Word)]
    [InlineData("file.xlsx", "any", AttachmentType.Excel)]
    [InlineData("file.xls", "any", AttachmentType.Excel)]
    [InlineData("file.csv", "any", AttachmentType.Excel)]
    [InlineData("file.pptx", "any", AttachmentType.PowerPoint)]
    [InlineData("file.ppt", "any", AttachmentType.PowerPoint)]
    [InlineData("file.jpg", "any", AttachmentType.Image)]
    [InlineData("file.jpeg", "any", AttachmentType.Image)]
    [InlineData("file.png", "any", AttachmentType.Image)]
    [InlineData("file.gif", "any", AttachmentType.Image)]
    [InlineData("file.bmp", "any", AttachmentType.Image)]
    [InlineData("file.webp", "any", AttachmentType.Image)]
    [InlineData("file.txt", "any", AttachmentType.Text)]
    [InlineData("file.md", "any", AttachmentType.Text)]
    [InlineData("file.rtf", "any", AttachmentType.Text)]
    [InlineData("file.zip", "any", AttachmentType.Archive)]
    [InlineData("file.rar", "any", AttachmentType.Archive)]
    [InlineData("file.7z", "any", AttachmentType.Archive)]
    [InlineData("file.tar", "any", AttachmentType.Archive)]
    [InlineData("file.gz", "any", AttachmentType.Archive)]
    [InlineData("file.mp3", "any", AttachmentType.Audio)]
    [InlineData("file.wav", "any", AttachmentType.Audio)]
    [InlineData("file.m4a", "any", AttachmentType.Audio)]
    [InlineData("file.mp4", "any", AttachmentType.Video)]
    [InlineData("file.avi", "any", AttachmentType.Video)]
    [InlineData("file.mov", "any", AttachmentType.Video)]
    [InlineData("file.mkv", "any", AttachmentType.Video)]
    [InlineData("file.eml", "any", AttachmentType.Email)]
    [InlineData("file.msg", "any", AttachmentType.Email)]
    [InlineData("file.ics", "any", AttachmentType.Calendar)]
    [InlineData("file.vcf", "any", AttachmentType.Calendar)]
    public void DetermineType_ByExtension(string fileName, string contentType, AttachmentType expected)
    {
        var att = Create(fileName: fileName, contentType: contentType);
        att.Type.Should().Be(expected);
    }

    [Theory]
    [InlineData("file.xyz", "image/png", AttachmentType.Image)]
    [InlineData("file.xyz", "audio/mpeg", AttachmentType.Audio)]
    [InlineData("file.xyz", "video/mp4", AttachmentType.Video)]
    [InlineData("file.xyz", "application/pdf", AttachmentType.PDF)]
    [InlineData("file.xyz", "application/msword", AttachmentType.Word)]
    [InlineData("file.xyz", "application/vnd.ms-excel", AttachmentType.Excel)]
    [InlineData("file.xyz", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", AttachmentType.Excel)]
    [InlineData("file.xyz", "application/vnd.ms-powerpoint", AttachmentType.PowerPoint)]
    [InlineData("file.xyz", "application/vnd.openxmlformats-officedocument.presentationml.presentation", AttachmentType.PowerPoint)]
    [InlineData("file.xyz", "text/plain", AttachmentType.Text)]
    [InlineData("file.xyz", "application/octet-stream", AttachmentType.Other)]
    public void DetermineType_FallbackToContentType(string fileName, string contentType, AttachmentType expected)
    {
        var att = Create(fileName: fileName, contentType: contentType);
        att.Type.Should().Be(expected);
    }

    #endregion

    #region SetExtractedContent

    [Fact]
    public void SetExtractedContent_SetsAllProperties()
    {
        var att = Create();
        att.SetExtractedContent("Hello world content here", pageCount: 3);

        att.ExtractedText.Should().Be("Hello world content here");
        att.IsProcessed.Should().BeTrue();
        att.ProcessedAt.Should().NotBeNull();
        att.PageCount.Should().Be(3);
        att.WordCount.Should().Be(4);
        att.Status.Should().Be(ProcessingStatus.Completed);
        att.ProcessingError.Should().BeNull();
    }

    [Fact]
    public void SetExtractedContent_EmptyText_WordCountZero()
    {
        var att = Create();
        att.SetExtractedContent("", pageCount: null);
        att.WordCount.Should().Be(0);
    }

    [Fact]
    public void SetExtractedContent_NullText_WordCountZero()
    {
        var att = Create();
        att.SetExtractedContent(null!, pageCount: null);
        att.WordCount.Should().Be(0);
    }

    [Fact]
    public void SetExtractedContent_ClearsProcessingError()
    {
        var att = Create();
        att.SetProcessingFailed("err");
        att.SetExtractedContent("recovered");
        att.ProcessingError.Should().BeNull();
        att.Status.Should().Be(ProcessingStatus.Completed);
    }

    #endregion

    #region SetProcessingFailed

    [Fact]
    public void SetProcessingFailed_SetsStatus()
    {
        var att = Create();
        att.SetProcessingFailed("timeout");
        att.Status.Should().Be(ProcessingStatus.Failed);
        att.ProcessingError.Should().Be("timeout");
        att.ProcessedAt.Should().NotBeNull();
    }

    #endregion

    #region SetAIAnalysis

    [Fact]
    public void SetAIAnalysis_SetsAllFields()
    {
        var att = Create();
        var keywords = new List<string> { "contract", "legal" };
        att.SetAIAnalysis("Contract summary", keywords, DocumentCategory.Contract, 0.95);

        att.AISummary.Should().Be("Contract summary");
        att.ExtractedKeywords.Should().BeEquivalentTo(keywords);
        att.DocumentCategory.Should().Be(DocumentCategory.Contract);
        att.ClassificationConfidence.Should().Be(0.95);
    }

    [Fact]
    public void SetAIAnalysis_NullKeywords_KeepsExisting()
    {
        var att = Create();
        att.SetAIAnalysis("summary", new List<string> { "first" }, null, null);
        att.SetAIAnalysis("updated", null, null, null);
        att.ExtractedKeywords.Should().Contain("first");
    }

    [Fact]
    public void SetAIAnalysis_NullSummary()
    {
        var att = Create();
        att.SetAIAnalysis(null, null, null, null);
        att.AISummary.Should().BeNull();
    }

    #endregion

    #region LinkToKnowledgeBase

    [Fact]
    public void LinkToKnowledgeBase_SetsProperties()
    {
        var att = Create();
        var kbId = Guid.NewGuid();
        att.LinkToKnowledgeBase(kbId, "emb-123");

        att.KnowledgeEntryId.Should().Be(kbId);
        att.EmbeddingId.Should().Be("emb-123");
        att.IsIndexed.Should().BeTrue();
        att.IndexedAt.Should().NotBeNull();
    }

    [Fact]
    public void LinkToKnowledgeBase_WithoutEmbedding()
    {
        var att = Create();
        att.LinkToKnowledgeBase(Guid.NewGuid());
        att.EmbeddingId.Should().BeNull();
        att.IsIndexed.Should().BeTrue();
    }

    #endregion

    #region GetFormattedSize

    [Theory]
    [InlineData(500, "500")]
    [InlineData(1024, "1")]
    [InlineData(1536, "1.5")]
    public void GetFormattedSize_ReturnsReadable(long bytes, string expectedPrefix)
    {
        var att = Create(sizeInBytes: bytes);
        att.GetFormattedSize().Should().StartWith(expectedPrefix);
    }

    [Fact]
    public void GetFormattedSize_LargeFile()
    {
        var att = Create(sizeInBytes: 1024L * 1024 * 1024); // 1 GB
        att.GetFormattedSize().Should().Contain("GB");
    }

    [Fact]
    public void GetFormattedSize_MegabyteFile()
    {
        var att = Create(sizeInBytes: 1024L * 1024 * 5); // 5 MB
        att.GetFormattedSize().Should().Contain("MB");
    }

    #endregion

    #region CanExtractText

    [Theory]
    [InlineData("file.pdf", true)]
    [InlineData("file.docx", true)]
    [InlineData("file.xlsx", true)]
    [InlineData("file.pptx", true)]
    [InlineData("file.txt", true)]
    [InlineData("file.jpg", false)]
    [InlineData("file.zip", false)]
    [InlineData("file.mp3", false)]
    [InlineData("file.mp4", false)]
    [InlineData("file.eml", false)]
    [InlineData("file.ics", false)]
    public void CanExtractText_DependsOnType(string fileName, bool expected)
    {
        var att = Create(fileName: fileName);
        att.CanExtractText().Should().Be(expected);
    }

    #endregion

    #region Initial State

    [Fact]
    public void InitialState_NotProcessed()
    {
        var att = Create();
        att.IsProcessed.Should().BeFalse();
        att.ProcessedAt.Should().BeNull();
        att.ExtractedText.Should().BeNull();
        att.WordCount.Should().BeNull();
        att.PageCount.Should().BeNull();
    }

    [Fact]
    public void InitialState_NotIndexed()
    {
        var att = Create();
        att.IsIndexed.Should().BeFalse();
        att.IndexedAt.Should().BeNull();
        att.KnowledgeEntryId.Should().BeNull();
    }

    [Fact]
    public void InitialState_EmptyKeywords()
    {
        var att = Create();
        att.ExtractedKeywords.Should().NotBeNull().And.BeEmpty();
    }

    #endregion
}
