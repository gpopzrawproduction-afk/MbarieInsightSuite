using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MIC.Infrastructure.Data.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace MIC.Tests.Unit.Infrastructure.Data;

/// <summary>
/// Tests for AttachmentStorageService covering file storage, deduplication, retrieval, deletion, and size computation.
/// </summary>
public class AttachmentStorageServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ILogger<AttachmentStorageService> _logger;
    private readonly AttachmentStorageService _service;

    public AttachmentStorageServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"MIC_AttachmentTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        _logger = Substitute.For<ILogger<AttachmentStorageService>>();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "AttachmentStorage:BasePath", _tempDir }
            })
            .Build();

        _service = new AttachmentStorageService(config, _logger);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true); }
        catch { /* best effort cleanup */ }
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_CreatesBaseDirectory()
    {
        Directory.Exists(_tempDir).Should().BeTrue();
    }

    [Fact]
    public void Constructor_UsesDefaultPath_WhenConfigMissing()
    {
        var config = new ConfigurationBuilder().Build();
        var svc = new AttachmentStorageService(config, _logger);
        svc.Should().NotBeNull();
    }

    #endregion

    #region StoreAsync Tests

    [Fact]
    public async Task StoreAsync_StoresFile_ReturnsResult()
    {
        var data = Encoding.UTF8.GetBytes("Hello, World!");

        var result = await _service.StoreAsync("test.txt", "text/plain", data);

        result.Should().NotBeNull();
        result.StoragePath.Should().NotBeNullOrEmpty();
        result.ContentHash.Should().NotBeNullOrEmpty();
        result.IsNew.Should().BeTrue();
        File.Exists(result.StoragePath).Should().BeTrue();
    }

    [Fact]
    public async Task StoreAsync_DeduplicatesSameContent()
    {
        var data = Encoding.UTF8.GetBytes("Duplicate content");

        var result1 = await _service.StoreAsync("file1.txt", "text/plain", data);
        var result2 = await _service.StoreAsync("file1.txt", "text/plain", data);

        result1.StoragePath.Should().Be(result2.StoragePath);
        result1.ContentHash.Should().Be(result2.ContentHash);
        result1.IsNew.Should().BeTrue();
        result2.IsNew.Should().BeFalse();
    }

    [Fact]
    public async Task StoreAsync_ThrowsOnNullData()
    {
        var act = () => _service.StoreAsync("test.txt", "text/plain", null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task StoreAsync_ThrowsOnEmptyData()
    {
        var act = () => _service.StoreAsync("test.txt", "text/plain", Array.Empty<byte>());
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task StoreAsync_GeneratesConsistentHash()
    {
        var data = Encoding.UTF8.GetBytes("Consistent hash test");

        var result1 = await _service.StoreAsync("a.txt", "text/plain", data);
        var result2 = await _service.StoreAsync("a.txt", "text/plain", data);

        result1.ContentHash.Should().Be(result2.ContentHash);
    }

    [Fact]
    public async Task StoreAsync_DifferentContent_DifferentHash()
    {
        var data1 = Encoding.UTF8.GetBytes("Content A");
        var data2 = Encoding.UTF8.GetBytes("Content B");

        var result1 = await _service.StoreAsync("a.txt", "text/plain", data1);
        var result2 = await _service.StoreAsync("b.txt", "text/plain", data2);

        result1.ContentHash.Should().NotBe(result2.ContentHash);
    }

    #endregion

    #region OpenReadAsync Tests

    [Fact]
    public async Task OpenReadAsync_ReturnsStream_ForStoredFile()
    {
        var data = Encoding.UTF8.GetBytes("Readable content");
        var storeResult = await _service.StoreAsync("read.txt", "text/plain", data);

        using var stream = await _service.OpenReadAsync(storeResult.StoragePath);

        stream.Should().NotBeNull();
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();
        content.Should().Be("Readable content");
    }

    [Fact]
    public async Task OpenReadAsync_ThrowsOnNullPath()
    {
        var act = () => _service.OpenReadAsync(null!);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task OpenReadAsync_ThrowsOnMissingFile()
    {
        var act = () => _service.OpenReadAsync(Path.Combine(_tempDir, "nonexistent.txt"));
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_RemovesStoredFile()
    {
        var data = Encoding.UTF8.GetBytes("Delete me");
        var storeResult = await _service.StoreAsync("delete.txt", "text/plain", data);
        File.Exists(storeResult.StoragePath).Should().BeTrue();

        await _service.DeleteAsync(storeResult.StoragePath);

        File.Exists(storeResult.StoragePath).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_DoesNotThrow_ForMissingFile()
    {
        var act = () => _service.DeleteAsync(Path.Combine(_tempDir, "nonexistent.txt"));
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteAsync_ThrowsOnNullPath()
    {
        var act = () => _service.DeleteAsync(null!);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region GetTotalSizeAsync Tests

    [Fact]
    public async Task GetTotalSizeAsync_ReturnsZero_WhenEmpty()
    {
        var size = await _service.GetTotalSizeAsync();
        size.Should().Be(0);
    }

    [Fact]
    public async Task GetTotalSizeAsync_ReturnsTotalSize_AfterStoring()
    {
        var data = Encoding.UTF8.GetBytes("Size test content");
        await _service.StoreAsync("size.txt", "text/plain", data);

        var size = await _service.GetTotalSizeAsync();

        size.Should().BeGreaterThan(0);
        size.Should().Be(data.Length);
    }

    [Fact]
    public async Task GetTotalSizeAsync_AccumulatesMultipleFiles()
    {
        var data1 = Encoding.UTF8.GetBytes("File one content");
        var data2 = Encoding.UTF8.GetBytes("File two content here");
        await _service.StoreAsync("one.txt", "text/plain", data1);
        await _service.StoreAsync("two.txt", "text/plain", data2);

        var totalSize = await _service.GetTotalSizeAsync();

        totalSize.Should().Be(data1.Length + data2.Length);
    }

    #endregion
}
