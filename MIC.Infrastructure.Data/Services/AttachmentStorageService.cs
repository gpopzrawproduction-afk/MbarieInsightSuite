using System.IO;
using System.Security.Cryptography;
using MIC.Core.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MIC.Infrastructure.Data.Services;

/// <summary>
/// File-system backed attachment storage with SHA-256 based deduplication.
/// </summary>
public sealed class AttachmentStorageService : IAttachmentStorageService
{
    private readonly string _basePath;
    private readonly ILogger<AttachmentStorageService> _logger;

    public AttachmentStorageService(IConfiguration configuration, ILogger<AttachmentStorageService> logger)
    {
        _logger = logger;
        var configuredPath = configuration["AttachmentStorage:BasePath"];
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _basePath = Path.Combine(appData, "MIC", "Attachments");
        }
        else
        {
            _basePath = configuredPath;
        }

        Directory.CreateDirectory(_basePath);
    }

    public async Task<AttachmentStoreResult> StoreAsync(string fileName, string contentType, byte[] data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length == 0)
        {
            throw new ArgumentException("Attachment payload cannot be empty.", nameof(data));
        }

        var hash = ComputeHash(data);
        var storagePath = BuildPath(hash, fileName);
        var directory = Path.GetDirectoryName(storagePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var isNew = true;
        if (File.Exists(storagePath))
        {
            var existingLength = new FileInfo(storagePath).Length;
            if (existingLength == data.Length)
            {
                isNew = false;
            }
            else
            {
                // Collision detected; append random suffix to prevent data loss.
                storagePath = BuildPath(hash, fileName, Guid.NewGuid().ToString("N"));
                directory = Path.GetDirectoryName(storagePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
        }

        if (isNew)
        {
            await File.WriteAllBytesAsync(storagePath, data, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Stored attachment {FileName} -> {Path}", fileName, storagePath);
        }
        else
        {
            _logger.LogDebug("Reused attachment {FileName} from {Path}", fileName, storagePath);
        }

        return new AttachmentStoreResult(storagePath, hash, isNew);
    }

    public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storagePath);
        if (!File.Exists(storagePath))
        {
            throw new FileNotFoundException("Attachment not found", storagePath);
        }

        Stream stream = new FileStream(storagePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storagePath);
        if (File.Exists(storagePath))
        {
            File.Delete(storagePath);
            _logger.LogDebug("Deleted attachment {Path}", storagePath);
        }

        return Task.CompletedTask;
    }

    public Task<long> GetTotalSizeAsync(CancellationToken cancellationToken = default)
    {
        long total = 0;
        if (!Directory.Exists(_basePath))
        {
            return Task.FromResult(total);
        }

        foreach (var file in Directory.EnumerateFiles(_basePath, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            total += new FileInfo(file).Length;
        }

        return Task.FromResult(total);
    }

    private string ComputeHash(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(data);
        return Convert.ToHexString(hashBytes);
    }

    private string BuildPath(string hash, string fileName, string? suffix = null)
    {
        var safeName = string.IsNullOrWhiteSpace(fileName) ? "attachment" : fileName;
        var extension = Path.GetExtension(safeName);
        var directory = Path.Combine(_basePath, hash[..2], hash[2..4]);
        var fileBase = suffix == null ? hash : $"{hash}_{suffix}";
        return Path.Combine(directory, fileBase + extension);
    }
}
