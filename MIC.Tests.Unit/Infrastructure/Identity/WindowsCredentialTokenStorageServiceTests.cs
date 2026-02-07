using FluentAssertions;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.Identity.TokenStorage;
using Xunit;

namespace MIC.Tests.Unit.Infrastructure.Identity;

public class WindowsCredentialTokenStorageServiceTests
{
    private readonly InMemoryCredentialManager _credentialManager = new();
    private readonly LoopbackProtector _protector = new();

    private WindowsCredentialTokenStorageService CreateService()
    {
        return new WindowsCredentialTokenStorageService(_credentialManager, _protector);
    }

    [Fact]
    public async Task StoreTokenAsync_PersistsEncryptedToken()
    {
        var service = CreateService();
        var token = new StoredOAuthToken
        {
            Provider = EmailProvider.Gmail,
            AccountId = "user@example.com",
            AccessToken = "access-token",
            RefreshToken = "refresh-token",
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1),
            Scopes = new[] { "scope1", "scope2" }
        };

        await service.StoreTokenAsync(token);

        var stored = await service.GetTokenAsync(EmailProvider.Gmail, "user@example.com");
        stored.Should().NotBeNull();
        stored!.AccessToken.Should().Be("access-token");
        stored.RefreshToken.Should().Be("refresh-token");
        stored.Scopes.Should().BeEquivalentTo(new[] { "scope1", "scope2" });

        var rawSecret = _credentialManager.GetRawSecret("MIC_OAUTH_TOKEN_GMAIL_USER@EXAMPLE.COM");
        rawSecret.Should().NotBeNull();
        rawSecret!.Should().NotBeEmpty();
        rawSecret.Should().NotContainInOrder("access-token".Select(c => (byte)c));
    }

    [Fact]
    public async Task GetTokensAsync_FiltersByProvider()
    {
        var service = CreateService();
        await service.StoreTokenAsync(new StoredOAuthToken
        {
            Provider = EmailProvider.Gmail,
            AccountId = "gmail@example.com",
            AccessToken = "gmail",
            RefreshToken = "g-refresh",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30)
        });
        await service.StoreTokenAsync(new StoredOAuthToken
        {
            Provider = EmailProvider.Outlook,
            AccountId = "outlook@example.com",
            AccessToken = "outlook",
            RefreshToken = "o-refresh",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30)
        });

        var gmailTokens = await service.GetTokensAsync(EmailProvider.Gmail);
        gmailTokens.Should().HaveCount(1);
        gmailTokens.Single().AccountId.Should().Be("gmail@example.com");

        var outlookTokens = await service.GetTokensAsync(EmailProvider.Outlook);
        outlookTokens.Should().HaveCount(1);
        outlookTokens.Single().AccountId.Should().Be("outlook@example.com");
    }

    [Fact]
    public async Task StorePayloadAsync_RoundTripsBinary()
    {
        var service = CreateService();
        var payload = new byte[] { 1, 2, 3, 4 };

        await service.StorePayloadAsync("cache-key", payload);
        var restored = await service.GetPayloadAsync("cache-key");

        restored.Should().NotBeNull();
        restored!.Should().Equal(payload);

        await service.RemovePayloadAsync("cache-key");
        var afterDelete = await service.GetPayloadAsync("cache-key");
        afterDelete.Should().BeNull();
    }

    [Fact]
    public async Task RemoveTokenAsync_RemovesCredential()
    {
        var service = CreateService();
        await service.StoreTokenAsync(new StoredOAuthToken
        {
            Provider = EmailProvider.Outlook,
            AccountId = "remove@example.com",
            AccessToken = "access",
            RefreshToken = "refresh",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10)
        });

        await service.RemoveTokenAsync(EmailProvider.Outlook, "remove@example.com");

        var token = await service.GetTokenAsync(EmailProvider.Outlook, "remove@example.com");
        token.Should().BeNull();
    }

    private sealed class InMemoryCredentialManager : ICredentialManager
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public void Write(string targetName, byte[] secret)
        {
            _store[targetName] = secret.ToArray();
        }

        public byte[]? Read(string targetName)
        {
            return _store.TryGetValue(targetName, out var value)
                ? value.ToArray()
                : null;
        }

        public void Delete(string targetName)
        {
            _store.Remove(targetName);
        }

        public IReadOnlyList<CredentialRecord> Enumerate(string targetPrefix)
        {
            return _store
                .Where(pair => pair.Key.StartsWith(targetPrefix, StringComparison.OrdinalIgnoreCase))
                .Select(pair => new CredentialRecord(pair.Key, pair.Value.ToArray()))
                .ToList();
        }

        public byte[]? GetRawSecret(string targetName)
        {
            return _store.TryGetValue(targetName, out var value) ? value : null;
        }
    }

    private sealed class LoopbackProtector : IDataProtector
    {
        public byte[] Protect(ReadOnlySpan<byte> data)
        {
            var buffer = data.ToArray();
            for (var i = 0; i < buffer.Length; i++)
            {
                buffer[i] ^= 0x5A;
            }
            return buffer;
        }

        public byte[] Unprotect(ReadOnlySpan<byte> protectedData)
        {
            return Protect(protectedData);
        }
    }
}
