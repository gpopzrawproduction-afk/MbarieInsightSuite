using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;

namespace MIC.Tests.Unit.Infrastructure.Identity;

internal sealed class FakeTokenStorageService : ITokenStorageService
{
    private readonly Dictionary<EmailProvider, Dictionary<string, StoredOAuthToken>> _tokens = new();
    private readonly Dictionary<string, byte[]> _payloads = new();

    public Task StoreTokenAsync(StoredOAuthToken token, CancellationToken cancellationToken = default)
    {
        if (!_tokens.TryGetValue(token.Provider, out var map))
        {
            map = new Dictionary<string, StoredOAuthToken>(StringComparer.OrdinalIgnoreCase);
            _tokens[token.Provider] = map;
        }

        map[token.AccountId] = token;
        return Task.CompletedTask;
    }

    public Task<StoredOAuthToken?> GetTokenAsync(EmailProvider provider, string accountId, CancellationToken cancellationToken = default)
    {
        if (_tokens.TryGetValue(provider, out var map) && map.TryGetValue(accountId, out var token))
        {
            return Task.FromResult<StoredOAuthToken?>(token);
        }

        return Task.FromResult<StoredOAuthToken?>(null);
    }

    public Task<IReadOnlyList<StoredOAuthToken>> GetTokensAsync(EmailProvider provider, CancellationToken cancellationToken = default)
    {
        if (_tokens.TryGetValue(provider, out var map))
        {
            return Task.FromResult<IReadOnlyList<StoredOAuthToken>>(map.Values.ToList());
        }

        return Task.FromResult<IReadOnlyList<StoredOAuthToken>>(Array.Empty<StoredOAuthToken>());
    }

    public Task RemoveTokenAsync(EmailProvider provider, string accountId, CancellationToken cancellationToken = default)
    {
        if (_tokens.TryGetValue(provider, out var map))
        {
            map.Remove(accountId);
        }

        return Task.CompletedTask;
    }

    public Task StorePayloadAsync(string key, byte[] payload, CancellationToken cancellationToken = default)
    {
        _payloads[key] = payload.ToArray();
        return Task.CompletedTask;
    }

    public Task<byte[]?> GetPayloadAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_payloads.TryGetValue(key, out var payload)
            ? payload.ToArray()
            : null);
    }

    public Task RemovePayloadAsync(string key, CancellationToken cancellationToken = default)
    {
        _payloads.Remove(key);
        return Task.CompletedTask;
    }

    public void Seed(StoredOAuthToken token)
    {
        if (!_tokens.TryGetValue(token.Provider, out var map))
        {
            map = new Dictionary<string, StoredOAuthToken>(StringComparer.OrdinalIgnoreCase);
            _tokens[token.Provider] = map;
        }

        map[token.AccountId] = token;
    }
}
