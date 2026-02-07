using System.Security.Cryptography;

namespace MIC.Infrastructure.Identity.TokenStorage;

/// <summary>
/// Uses DPAPI to secure payloads for the current Windows user.
/// </summary>
public sealed class WindowsDpapiDataProtector : IDataProtector
{
    public WindowsDpapiDataProtector()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("DPAPI protector requires Windows.");
        }
    }

    public byte[] Protect(ReadOnlySpan<byte> data)
    {
        return ProtectedData.Protect(data.ToArray(), optionalEntropy: null, scope: DataProtectionScope.CurrentUser);
    }

    public byte[] Unprotect(ReadOnlySpan<byte> protectedData)
    {
        return ProtectedData.Unprotect(protectedData.ToArray(), optionalEntropy: null, scope: DataProtectionScope.CurrentUser);
    }
}
