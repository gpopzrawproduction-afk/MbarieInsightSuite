using System.Collections.Generic;

namespace MIC.Infrastructure.Identity.TokenStorage;

/// <summary>
/// Abstraction for interacting with the underlying credential manager.
/// </summary>
public interface ICredentialManager
{
    void Write(string targetName, byte[] secret);

    byte[]? Read(string targetName);

    void Delete(string targetName);

    IReadOnlyList<CredentialRecord> Enumerate(string targetPrefix);
}

/// <summary>
/// Represents a credential record payload.
/// </summary>
public sealed record CredentialRecord(string TargetName, byte[] Secret);
