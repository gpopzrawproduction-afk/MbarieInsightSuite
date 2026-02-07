using MIC.Core.Domain.Entities;

namespace MIC.Core.Application.Common.Interfaces;

/// <summary>
/// Provides secure persistence for OAuth tokens and related cache data.
/// </summary>
public interface ITokenStorageService
{
    Task StoreTokenAsync(StoredOAuthToken token, CancellationToken cancellationToken = default);

    Task<StoredOAuthToken?> GetTokenAsync(EmailProvider provider, string accountId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StoredOAuthToken>> GetTokensAsync(EmailProvider provider, CancellationToken cancellationToken = default);

    Task RemoveTokenAsync(EmailProvider provider, string accountId, CancellationToken cancellationToken = default);

    Task StorePayloadAsync(string key, byte[] payload, CancellationToken cancellationToken = default);

    Task<byte[]?> GetPayloadAsync(string key, CancellationToken cancellationToken = default);

    Task RemovePayloadAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a persisted OAuth token entry.
/// </summary>
public sealed record StoredOAuthToken
{
    public EmailProvider Provider { get; init; }

    public string AccountId { get; init; } = string.Empty;

    public string AccessToken { get; init; } = string.Empty;

    public string? RefreshToken { get; init; }

    public DateTime ExpiresAtUtc { get; init; }

    public IReadOnlyList<string> Scopes { get; init; } = Array.Empty<string>();

    public DateTime StoredAtUtc { get; init; } = DateTime.UtcNow;
}
