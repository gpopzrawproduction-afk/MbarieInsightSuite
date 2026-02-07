using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MIC.Core.Application.Common.Interfaces;

/// <summary>
/// Provides storage operations for email attachments including deduplication support.
/// </summary>
public interface IAttachmentStorageService
{
    /// <summary>
    /// Stores the attachment on disk and returns the storage result containing the final path and hash.
    /// </summary>
    Task<AttachmentStoreResult> StoreAsync(string fileName, string contentType, byte[] data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens the attachment for reading from the backing store.
    /// </summary>
    Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the attachment from the backing store if it exists.
    /// </summary>
    Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the total size in bytes consumed by attachments within the backing store.
    /// </summary>
    Task<long> GetTotalSizeAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the outcome of storing an attachment.
/// </summary>
public sealed record AttachmentStoreResult(string StoragePath, string ContentHash, bool IsNew);
