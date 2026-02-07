using System.Text;
using System.Text.Json;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;

namespace MIC.Infrastructure.Identity.TokenStorage;

/// <summary>
/// Persists OAuth tokens using Windows Credential Manager with DPAPI encryption.
/// </summary>
public sealed class WindowsCredentialTokenStorageService : ITokenStorageService
{
    private const string TokenPrefix = "MIC_OAUTH_TOKEN_";
    private const string PayloadPrefix = "MIC_OAUTH_PAYLOAD_";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly ICredentialManager _credentialManager;
    private readonly IDataProtector _dataProtector;

    public WindowsCredentialTokenStorageService(
        ICredentialManager credentialManager,
        IDataProtector dataProtector)
    {
        _credentialManager = credentialManager;
        _dataProtector = dataProtector;
    }

    public Task StoreTokenAsync(StoredOAuthToken token, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entry = token with { StoredAtUtc = DateTime.UtcNow };
        var json = JsonSerializer.Serialize(entry, SerializerOptions);
        var encrypted = _dataProtector.Protect(Encoding.UTF8.GetBytes(json));
        _credentialManager.Write(BuildTokenTarget(entry.Provider, entry.AccountId), encrypted);
        return Task.CompletedTask;
    }

    public Task<StoredOAuthToken?> GetTokenAsync(EmailProvider provider, string accountId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var payload = _credentialManager.Read(BuildTokenTarget(provider, accountId));
        if (payload is null)
        {
            return Task.FromResult<StoredOAuthToken?>(null);
        }

        var json = Encoding.UTF8.GetString(_dataProtector.Unprotect(payload));
        var token = JsonSerializer.Deserialize<StoredOAuthToken>(json, SerializerOptions);
        return Task.FromResult(token);
    }

    public Task<IReadOnlyList<StoredOAuthToken>> GetTokensAsync(EmailProvider provider, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var prefix = BuildTokenTarget(provider, string.Empty);
        var records = _credentialManager.Enumerate(prefix);
        var list = new List<StoredOAuthToken>(records.Count);

        foreach (var record in records)
        {
            var decrypted = _dataProtector.Unprotect(record.Secret);
            var json = Encoding.UTF8.GetString(decrypted);
            if (JsonSerializer.Deserialize<StoredOAuthToken>(json, SerializerOptions) is { } token)
            {
                list.Add(token);
            }
        }

        return Task.FromResult((IReadOnlyList<StoredOAuthToken>)list);
    }

    public Task RemoveTokenAsync(EmailProvider provider, string accountId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _credentialManager.Delete(BuildTokenTarget(provider, accountId));
        return Task.CompletedTask;
    }

    public Task StorePayloadAsync(string key, byte[] payload, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var encrypted = _dataProtector.Protect(payload);
        _credentialManager.Write(BuildPayloadTarget(key), encrypted);
        return Task.CompletedTask;
    }

    public Task<byte[]?> GetPayloadAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payload = _credentialManager.Read(BuildPayloadTarget(key));
        if (payload is null)
        {
            return Task.FromResult<byte[]?>(null);
        }

        var decrypted = _dataProtector.Unprotect(payload);
        return Task.FromResult<byte[]?>(decrypted);
    }

    public Task RemovePayloadAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _credentialManager.Delete(BuildPayloadTarget(key));
        return Task.CompletedTask;
    }

    private static string BuildTokenTarget(EmailProvider provider, string accountId)
    {
        return string.IsNullOrWhiteSpace(accountId)
            ? $"{TokenPrefix}{provider.ToString().ToUpperInvariant()}_"
            : $"{TokenPrefix}{provider.ToString().ToUpperInvariant()}_{accountId.ToUpperInvariant()}";
    }

    private static string BuildPayloadTarget(string key)
    {
        return $"{PayloadPrefix}{key.ToUpperInvariant()}";
    }
}
