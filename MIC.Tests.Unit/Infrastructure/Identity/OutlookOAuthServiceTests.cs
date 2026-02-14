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

    #region Additional OAuth Edge Cases

    [Fact]
    public async Task GetOutlookAccessTokenAsync_NoStoredToken_Authenticates()
    {
        var storage = new FakeTokenStorageService();
        // No token seeded

        var service = new TestOutlookService(CreateConfiguration(), storage)
        {
            AuthenticateHandler = _ => Task.FromResult(new OAuthResult
            {
                Success = true,
                AccessToken = "fresh-token",
                EmailAddress = "new@example.com",
                RefreshToken = "new-refresh",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                Provider = EmailProvider.Outlook
            })
        };

        var token = await service.GetOutlookAccessTokenAsync("new@example.com");

        token.Should().Be("fresh-token");
        service.AuthenticationInvocations.Should().Be(1);
    }

    [Fact]
    public async Task GetOutlookAccessTokenAsync_AuthenticationFails_ThrowsInvalidOperation()
    {
        var storage = new FakeTokenStorageService();

        var service = new TestOutlookService(CreateConfiguration(), storage)
        {
            AuthenticateHandler = _ => Task.FromResult(new OAuthResult
            {
                Success = false,
                ErrorMessage = "User cancelled"
            })
        };

        var act = () => service.GetOutlookAccessTokenAsync("user@example.com");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*User cancelled*");
    }

    [Fact]
    public async Task GetOutlookAccessTokenAsync_AuthenticationThrows_PropagatesException()
    {
        var storage = new FakeTokenStorageService();

        var service = new TestOutlookService(CreateConfiguration(), storage)
        {
            AuthenticateHandler = _ => throw new TaskCanceledException("User cancelled")
        };

        var act = () => service.GetOutlookAccessTokenAsync("user@example.com");

        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    [Fact]
    public async Task GetOutlookAccessTokenAsync_RefreshSucceeds_DoesNotAuthenticate()
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
                    AccessToken = "refreshed-successfully",
                    ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
                };
                await storage.StoreTokenAsync(refreshed);
                return "refreshed-successfully";
            },
            AuthenticateHandler = _ => throw new InvalidOperationException("Should NOT authenticate")
        };

        var token = await service.GetOutlookAccessTokenAsync("user@example.com");

        token.Should().Be("refreshed-successfully");
        service.AuthenticationInvocations.Should().Be(0);
    }

    [Fact]
    public void Constructor_ReadsConfigurationValues()
    {
        var storage = new FakeTokenStorageService();
        var config = CreateConfiguration();

        var service = new TestOutlookService(config, storage);

        service.Should().NotBeNull();
    }

    [Fact]
    public async Task GetOutlookAccessTokenAsync_ExpiredNoRefreshToken_Authenticates()
    {
        var storage = new FakeTokenStorageService();
        storage.Seed(new StoredOAuthToken
        {
            Provider = EmailProvider.Outlook,
            AccountId = "user@example.com",
            AccessToken = "expired-token",
            RefreshToken = null, // No refresh token
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-10) // Expired
        });

        var service = new TestOutlookService(CreateConfiguration(), storage)
        {
            RefreshHandler = _ => Task.FromResult<string?>(null),
            AuthenticateHandler = _ => Task.FromResult(new OAuthResult
            {
                Success = true,
                AccessToken = "new-token",
                EmailAddress = "user@example.com",
                RefreshToken = "new-refresh",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                Provider = EmailProvider.Outlook
            })
        };

        var token = await service.GetOutlookAccessTokenAsync("user@example.com");

        token.Should().Be("new-token");
    }

    #endregion

    #region NotSupportedException Methods

    [Fact]
    public async Task AuthenticateGmailAsync_ThrowsNotSupported()
    {
        var storage = new FakeTokenStorageService();
        var service = new TestOutlookService(CreateConfiguration(), storage);

        var act = () => service.AuthenticateGmailAsync();

        await act.Should().ThrowAsync<NotSupportedException>();
    }

    [Fact]
    public async Task GetGmailAccessTokenAsync_ThrowsNotSupported()
    {
        var storage = new FakeTokenStorageService();
        var service = new TestOutlookService(CreateConfiguration(), storage);

        var act = () => service.GetGmailAccessTokenAsync("user@example.com");

        await act.Should().ThrowAsync<NotSupportedException>();
    }

    [Fact]
    public async Task AuthorizeGmailAccountAsync_ThrowsNotSupported()
    {
        var storage = new FakeTokenStorageService();
        var service = new TestOutlookService(CreateConfiguration(), storage);

        var act = () => service.AuthorizeGmailAccountAsync();

        await act.Should().ThrowAsync<NotSupportedException>();
    }

    #endregion

    #region Input Validation

    [Fact]
    public async Task GetOutlookAccessTokenAsync_EmptyEmail_ThrowsArgumentException()
    {
        var storage = new FakeTokenStorageService();
        var service = new TestOutlookService(CreateConfiguration(), storage);

        var act = () => service.GetOutlookAccessTokenAsync("");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetOutlookAccessTokenAsync_NullEmail_ThrowsArgumentException()
    {
        var storage = new FakeTokenStorageService();
        var service = new TestOutlookService(CreateConfiguration(), storage);

        var act = () => service.GetOutlookAccessTokenAsync(null!);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

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
