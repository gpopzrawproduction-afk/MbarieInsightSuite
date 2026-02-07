namespace MIC.Infrastructure.Identity.TokenStorage;

/// <summary>
/// Encrypts and decrypts sensitive payloads for storage.
/// </summary>
public interface IDataProtector
{
    byte[] Protect(ReadOnlySpan<byte> data);

    byte[] Unprotect(ReadOnlySpan<byte> protectedData);
}
