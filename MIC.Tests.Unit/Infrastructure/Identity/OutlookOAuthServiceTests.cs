using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Identity.Client;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.Identity.Services;
using Xunit;

namespace MIC.Tests.Unit.Infrastructure.Identity;

public class OutlookOAuthServiceTests
{
    private static IConfiguration CreateConfiguration()
    {
        var values = new Dictionary<string, string?>
        {
            ["OAuth2:Outlook:ClientId"] = "test-client",
            ["OAuth2:Outlook:TenantId"] = "common",
            ["OAuth2:Outlook:RedirectUri"] = "http://localhost"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    [Fact]
    public async Task GetOutlookAccessTokenAsync_ReturnsStoredToken_WhenNotExpiring()
    {
        var storage = new FakeTokenStorageService();
        storage.Seed(new StoredOAuthToken
        {
            Provider = EmailProvider.Outlook,
            AccountId = "user@example.com",
            AccessToken = "stored-token",
            RefreshToken = "refresh-token",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30)
        });

        var service = new TestOutlookService(CreateConfiguration(), storage)
        {
            RefreshHandler = _ => throw new InvalidOperationException("Should not refresh"),
            AuthenticateHandler = _ => throw new InvalidOperationException("Should not authenticate")
        };

        var token = await service.GetOutlookAccessTokenAsync("user@example.com");
        token.Should().Be("stored-token");
    }

    [Fact]
    public async Task GetOutlookAccessTokenAsync_RefreshesToken_WhenExpiring()
    {
        var storage = new FakeTokenStorageService();
        storage.Seed(new StoredOAuthToken
        {
            Provider = EmailProvider.Outlook,
            AccountId = "user@example.com",
            AccessToken = "old-token",
            RefreshToken = "refresh-token",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(1)
        });

        var service = new TestOutlookService(CreateConfiguration(), storage)
        {
            RefreshHandler = async token =>
            {
                var refreshed = token with
                {
                    AccessToken = "refreshed-token",
                    ExpiresAtUtc = DateTime.UtcNow.AddMinutes(45)
                };
                await storage.StoreTokenAsync(refreshed);
                return "refreshed-token";
            },
            AuthenticateHandler = _ => throw new InvalidOperationException("Should not authenticate")
        };

        var token = await service.GetOutlookAccessTokenAsync("user@example.com");
        token.Should().Be("refreshed-token");
        var persisted = await storage.GetTokenAsync(EmailProvider.Outlook, "user@example.com");
        persisted!.AccessToken.Should().Be("refreshed-token");
    }

    [Fact]
    public async Task GetOutlookAccessTokenAsync_Reauthenticates_WhenRefreshFails()
    {
        var storage = new FakeTokenStorageService();
        storage.Seed(new StoredOAuthToken
        {
            Provider = EmailProvider.Outlook,
            AccountId = "user@example.com",
            AccessToken = "old-token",
            RefreshToken = "refresh-token",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(1)
        });

        var service = new TestOutlookService(CreateConfiguration(), storage)
        {
            RefreshHandler = _ => Task.FromResult<string?>(null),
            AuthenticateHandler = _ => Task.FromResult(new OAuthResult
            {
                Success = true,
                AccessToken = "interactive-token",
                EmailAddress = "user@example.com",
                RefreshToken = "new-refresh",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                Provider = EmailProvider.Outlook
            })
        };

        var token = await service.GetOutlookAccessTokenAsync("user@example.com");
        token.Should().Be("interactive-token");
        service.AuthenticationInvocations.Should().Be(1);
    }

    private sealed class TestOutlookService : OutlookOAuthService
    {
        private readonly FakeTokenStorageService _storage;

        public TestOutlookService(IConfiguration configuration, FakeTokenStorageService storage)
            : base(configuration, NullLogger<OutlookOAuthService>.Instance, storage)
        {
            _storage = storage;
        }

        public Func<StoredOAuthToken, Task<string?>>? RefreshHandler { get; set; }

        public Func<CancellationToken, Task<OAuthResult>>? AuthenticateHandler { get; set; }

        public int AuthenticationInvocations { get; private set; }

        public override Task<OAuthResult> AuthenticateOutlookAsync(CancellationToken cancellationToken = default)
        {
            AuthenticationInvocations++;
            if (AuthenticateHandler is null)
            {
                throw new InvalidOperationException("No authentication handler configured for test");
            }

            return AuthenticateHandler(cancellationToken);
        }

        protected override Task<string?> TryRefreshAsync(StoredOAuthToken storedToken, CancellationToken cancellationToken)
        {
            if (RefreshHandler is null)
            {
                throw new InvalidOperationException("No refresh handler configured for test");
            }

            return RefreshHandler(storedToken);
        }

        protected override IPublicClientApplication BuildPublicClientApp()
        {
            throw new InvalidOperationException("Test should not build MSAL client");
        }

        protected override Task PersistTokenAsync(string accountId, AuthenticationResult result, CancellationToken cancellationToken)
        {
            var entry = new StoredOAuthToken
            {
                Provider = EmailProvider.Outlook,
                AccountId = accountId,
                AccessToken = result.AccessToken,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30)
            };
            return _storage.StoreTokenAsync(entry, cancellationToken);
        }

        protected override Task<string?> ResolveUserEmailAsync(string accessToken, CancellationToken cancellationToken)
        {
            return Task.FromResult<string?>("user@example.com");
        }

        protected override Task<AuthenticationResult> AcquireInteractiveAsync(IPublicClientApplication app, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Test should not invoke interactive authentication");
        }
    }
}
