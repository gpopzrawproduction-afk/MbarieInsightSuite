using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MIC.Infrastructure.Data.Services;

namespace MIC.Tests.Unit.Infrastructure.Data;

/// <summary>
/// Tests for AttachmentStorageService file-system backed attachment storage.
/// </summary>
public class AttachmentStorageServiceExtendedTests : IDisposable
{
    private readonly string _testBasePath;
    private readonly AttachmentStorageService _service;

    public AttachmentStorageServiceExtendedTests()
    {
        _testBasePath = Path.Combine(Path.GetTempPath(), "MIC_AttExt_" + Guid.NewGuid().ToString("N"));
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AttachmentStorage:BasePath"] = _testBasePath
            })
            .Build();
        _service = new AttachmentStorageService(config, NullLogger<AttachmentStorageService>.Instance);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testBasePath))
                Directory.Delete(_testBasePath, true);
        }
        catch { /* best effort cleanup */ }
    }

    #region StoreAsync

    [Fact]
    public async Task StoreAsync_ValidData_ReturnsResult()
    {
        var data = Encoding.UTF8.GetBytes("Hello, World!");
        var result = await _service.StoreAsync("test.txt", "text/plain", data);

        result.Should().NotBeNull();
        result.StoragePath.Should().NotBeNullOrWhiteSpace();
        result.ContentHash.Should().NotBeNullOrWhiteSpace();
        result.IsNew.Should().BeTrue();
    }

    [Fact]
    public async Task StoreAsync_CreatesFile()
    {
        var data = Encoding.UTF8.GetBytes("File content");
        var result = await _service.StoreAsync("file.txt", "text/plain", data);

        File.Exists(result.StoragePath).Should().BeTrue();
        var stored = await File.ReadAllBytesAsync(result.StoragePath);
        stored.Should().BeEquivalentTo(data);
    }

    [Fact]
    public async Task StoreAsync_SameContentTwice_Deduplicates()
    {
        var data = Encoding.UTF8.GetBytes("Same content");
        var first = await _service.StoreAsync("a.txt", "text/plain", data);
        var second = await _service.StoreAsync("a.txt", "text/plain", data);

        second.IsNew.Should().BeFalse();
        first.ContentHash.Should().Be(second.ContentHash);
    }

    [Fact]
    public async Task StoreAsync_NullData_ThrowsArgumentNullException()
    {
        var act = () => _service.StoreAsync("test.txt", "text/plain", null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task StoreAsync_EmptyData_ThrowsArgumentException()
    {
        var act = () => _service.StoreAsync("test.txt", "text/plain", Array.Empty<byte>());
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task StoreAsync_DifferentContent_ProducesDifferentHashes()
    {
        var data1 = Encoding.UTF8.GetBytes("Content A");
        var data2 = Encoding.UTF8.GetBytes("Content B");

        var result1 = await _service.StoreAsync("a.txt", "text/plain", data1);
        var result2 = await _service.StoreAsync("b.txt", "text/plain", data2);

        result1.ContentHash.Should().NotBe(result2.ContentHash);
    }

    [Fact]
    public async Task StoreAsync_PreservesFileExtension()
    {
        var data = Encoding.UTF8.GetBytes("PDF-like content");
        var result = await _service.StoreAsync("report.pdf", "application/pdf", data);

        result.StoragePath.Should().EndWith(".pdf");
    }

    [Fact]
    public async Task StoreAsync_EmptyFileName_UsesDefault()
    {
        var data = Encoding.UTF8.GetBytes("No name content");
        var result = await _service.StoreAsync("", "text/plain", data);

        result.StoragePath.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task StoreAsync_HashIsConsistent()
    {
        var data = Encoding.UTF8.GetBytes("Consistency check");
        var r1 = await _service.StoreAsync("f1.txt", "text/plain", data);
        var r2 = await _service.StoreAsync("f2.txt", "text/plain", data);
        r1.ContentHash.Should().Be(r2.ContentHash);
    }

    [Fact]
    public async Task StoreAsync_LargeFile_StoredSuccessfully()
    {
        var data = new byte[1024 * 100]; // 100KB
        new Random(42).NextBytes(data);
        var result = await _service.StoreAsync("large.bin", "application/octet-stream", data);
        result.IsNew.Should().BeTrue();
        File.Exists(result.StoragePath).Should().BeTrue();
    }

    #endregion

    #region OpenReadAsync

    [Fact]
    public async Task OpenReadAsync_ExistingFile_ReturnsStream()
    {
        var data = Encoding.UTF8.GetBytes("Readable content");
        var stored = await _service.StoreAsync("read.txt", "text/plain", data);

        await using var stream = await _service.OpenReadAsync(stored.StoragePath);
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();

        content.Should().Be("Readable content");
    }

    [Fact]
    public async Task OpenReadAsync_NonExistentFile_ThrowsFileNotFoundException()
    {
        var act = () => _service.OpenReadAsync(Path.Combine(_testBasePath, "nonexistent.txt"));
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task OpenReadAsync_NullPath_ThrowsArgumentException()
    {
        var act = () => _service.OpenReadAsync(null!);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task OpenReadAsync_EmptyPath_ThrowsArgumentException()
    {
        var act = () => _service.OpenReadAsync("");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_ExistingFile_DeletesFile()
    {
        var data = Encoding.UTF8.GetBytes("Delete me");
        var stored = await _service.StoreAsync("del.txt", "text/plain", data);
        File.Exists(stored.StoragePath).Should().BeTrue();

        await _service.DeleteAsync(stored.StoragePath);
        File.Exists(stored.StoragePath).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentFile_DoesNotThrow()
    {
        var act = () => _service.DeleteAsync(Path.Combine(_testBasePath, "missing.txt"));
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteAsync_NullPath_ThrowsArgumentException()
    {
        var act = () => _service.DeleteAsync(null!);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region GetTotalSizeAsync
    
    [Fact]
    public async Task GetTotalSizeAsync_AfterStoringFiles_ReturnsPositiveSize()
    {
        var data = Encoding.UTF8.GetBytes("Size check content here!");
        await _service.StoreAsync("size.txt", "text/plain", data);

        var size = await _service.GetTotalSizeAsync();
        size.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetTotalSizeAsync_AfterDeletion_SizeDecreases()
    {
        var data = Encoding.UTF8.GetBytes("Will be deleted");
        var stored = await _service.StoreAsync("willdelete.txt", "text/plain", data);
        var sizeBefore = await _service.GetTotalSizeAsync();

        await _service.DeleteAsync(stored.StoragePath);
        var sizeAfter = await _service.GetTotalSizeAsync();

        sizeAfter.Should().BeLessThan(sizeBefore);
    }

    #endregion

    #region Constructor

    [Fact]
    public void Constructor_NoConfiguredPath_UsesDefault()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var svc = new AttachmentStorageService(config, NullLogger<AttachmentStorageService>.Instance);
        svc.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ConfiguredPath_UsesProvidedPath()
    {
        var customPath = Path.Combine(Path.GetTempPath(), "MIC_Custom_" + Guid.NewGuid().ToString("N"));
        try
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AttachmentStorage:BasePath"] = customPath
                })
                .Build();
            var svc = new AttachmentStorageService(config, NullLogger<AttachmentStorageService>.Instance);
            Directory.Exists(customPath).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(customPath))
                Directory.Delete(customPath, true);
        }
    }

    #endregion
}
