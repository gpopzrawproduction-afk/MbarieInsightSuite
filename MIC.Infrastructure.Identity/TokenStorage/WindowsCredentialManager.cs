using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace MIC.Infrastructure.Identity.TokenStorage;

/// <summary>
/// Windows Credential Manager implementation backed by Win32 Cred APIs.
/// </summary>
public sealed class WindowsCredentialManager : ICredentialManager
{
    private const int MaxCredentialBlobSize = 5120;

    public WindowsCredentialManager()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Windows Credential Manager is only available on Windows platforms.");
        }
    }

    public void Write(string targetName, byte[] secret)
    {
        ArgumentNullException.ThrowIfNull(targetName);
        ArgumentNullException.ThrowIfNull(secret);

        if (secret.Length > MaxCredentialBlobSize)
        {
            throw new ArgumentOutOfRangeException(nameof(secret), "Credential payload exceeds Windows Credential Manager size limits.");
        }

        var credential = new NativeCredential
        {
            AttributeCount = 0,
            Attributes = IntPtr.Zero,
            Comment = null,
            TargetAlias = null,
            Type = CredentialType.Generic,
            Persist = (uint)CredentialPersistence.LocalMachine,
            CredentialBlobSize = (uint)secret.Length,
            TargetName = targetName,
            UserName = Environment.UserName,
            CredentialBlob = Marshal.AllocHGlobal(secret.Length)
        };

        try
        {
            Marshal.Copy(secret, 0, credential.CredentialBlob, secret.Length);

            if (!NativeMethods.CredWrite(ref credential, 0))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to write credential for target '{targetName}'.");
            }
        }
        finally
        {
            if (credential.CredentialBlob != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(credential.CredentialBlob);
            }
        }
    }

    public byte[]? Read(string targetName)
    {
        ArgumentNullException.ThrowIfNull(targetName);

        if (!NativeMethods.CredRead(targetName, CredentialType.Generic, 0, out var credPtr))
        {
            var error = Marshal.GetLastWin32Error();
            if (error == NativeMethods.ErrorNotFound)
            {
                return null;
            }

            throw new Win32Exception(error, $"Failed to read credential for target '{targetName}'.");
        }

        try
        {
            var credential = Marshal.PtrToStructure<NativeCredential>(credPtr);
            if (credential.CredentialBlob == IntPtr.Zero || credential.CredentialBlobSize == 0)
            {
                return Array.Empty<byte>();
            }

            var buffer = new byte[credential.CredentialBlobSize];
            Marshal.Copy(credential.CredentialBlob, buffer, 0, buffer.Length);
            return buffer;
        }
        finally
        {
            NativeMethods.CredFree(credPtr);
        }
    }

    public void Delete(string targetName)
    {
        ArgumentNullException.ThrowIfNull(targetName);

        if (!NativeMethods.CredDelete(targetName, CredentialType.Generic, 0))
        {
            var error = Marshal.GetLastWin32Error();
            if (error == NativeMethods.ErrorNotFound)
            {
                return;
            }

            throw new Win32Exception(error, $"Failed to delete credential for target '{targetName}'.");
        }
    }

    public IReadOnlyList<CredentialRecord> Enumerate(string targetPrefix)
    {
        ArgumentNullException.ThrowIfNull(targetPrefix);

        if (!NativeMethods.CredEnumerate(targetPrefix + "*", 0, out var count, out var ptr))
        {
            var error = Marshal.GetLastWin32Error();
            if (error == NativeMethods.ErrorNotFound)
            {
                return Array.Empty<CredentialRecord>();
            }

            throw new Win32Exception(error, $"Failed to enumerate credentials for prefix '{targetPrefix}'.");
        }

        try
        {
            var records = new List<CredentialRecord>((int)count);

            for (var i = 0; i < count; i++)
            {
                var credentialPtr = Marshal.ReadIntPtr(ptr, i * IntPtr.Size);
                var credential = Marshal.PtrToStructure<NativeCredential>(credentialPtr);
                if (credential.TargetName is null)
                {
                    continue;
                }

                var length = (int)credential.CredentialBlobSize;
                var data = length > 0 ? new byte[length] : Array.Empty<byte>();
                if (length > 0)
                {
                    Marshal.Copy(credential.CredentialBlob, data, 0, length);
                }

                records.Add(new CredentialRecord(credential.TargetName, data));
            }

            return records;
        }
        finally
        {
            NativeMethods.CredFree(ptr);
        }
    }

    private enum CredentialType : uint
    {
        Generic = 1
    }

    private enum CredentialPersistence : uint
    {
        Session = 1,
        LocalMachine,
        Enterprise
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NativeCredential
    {
        public uint Flags;
        public CredentialType Type;
        public string? TargetName;
        public string? Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string? TargetAlias;
        public string? UserName;
    }

    private static class NativeMethods
    {
        public const int ErrorNotFound = 1168;

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CredWrite(ref NativeCredential userCredential, uint flags);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CredRead(string target, CredentialType type, int reservedFlag, out IntPtr credentialPtr);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool CredDelete(string target, CredentialType type, int flags);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CredEnumerate(string filter, int flags, out uint count, out IntPtr credentials);

        [DllImport("advapi32.dll", SetLastError = false)]
        public static extern void CredFree(IntPtr buffer);
    }
}
