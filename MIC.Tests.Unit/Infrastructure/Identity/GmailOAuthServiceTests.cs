using FluentAssertions;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.Identity.Services;
using Xunit;

namespace MIC.Tests.Unit.Infrastructure.Identity;

public class GmailOAuthServiceTests
{
    private static IConfiguration CreateConfiguration()
    {
        var values = new Dictionary<string, string?>
        {
            ["OAuth2:Gmail:ClientId"] = "test-client",
            ["OAuth2:Gmail:ClientSecret"] = "test-secret"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    [Fact]
    public async Task GetGmailAccessTokenAsync_ReturnsStoredToken_WhenNotExpiring()
    {
        var storage = new FakeTokenStorageService();
        storage.Seed(new StoredOAuthToken
        {
            Provider = EmailProvider.Gmail,
            AccountId = "user@example.com",
            AccessToken = "stored-token",
            RefreshToken = "refresh-token",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30)
        });

        var service = new TestGmailService(CreateConfiguration(), storage)
        {
            RefreshHandler = (_, _) => throw new InvalidOperationException("Should not refresh"),
            AuthenticateHandler = _ => throw new InvalidOperationException("Should not authenticate")
        };

        var token = await service.GetGmailAccessTokenAsync("user@example.com");

        token.Should().Be("stored-token");
    }

    [Fact]
    public async Task GetGmailAccessTokenAsync_RefreshesToken_WhenExpiring()
    {
        var storage = new FakeTokenStorageService();
        storage.Seed(new StoredOAuthToken
        {
            Provider = EmailProvider.Gmail,
            AccountId = "user@example.com",
            AccessToken = "old-token",
            RefreshToken = "refresh-token",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(1)
        });

        var service = new TestGmailService(CreateConfiguration(), storage)
        {
            RefreshHandler = async (token, ct) =>
            {
                var refreshed = token with
                {
                    AccessToken = "refreshed-token",
                    ExpiresAtUtc = DateTime.UtcNow.AddMinutes(45)
                };
                await storage.StoreTokenAsync(refreshed, ct);
                return "refreshed-token";
            },
            AuthenticateHandler = _ => throw new InvalidOperationException("Should not authenticate")
        };

        var token = await service.GetGmailAccessTokenAsync("user@example.com");

        token.Should().Be("refreshed-token");
        var persisted = await storage.GetTokenAsync(EmailProvider.Gmail, "user@example.com");
        persisted!.AccessToken.Should().Be("refreshed-token");
    }

    [Fact]
    public async Task GetGmailAccessTokenAsync_Reauthenticates_WhenRefreshFails()
    {
        var storage = new FakeTokenStorageService();
        storage.Seed(new StoredOAuthToken
        {
            Provider = EmailProvider.Gmail,
            AccountId = "user@example.com",
            AccessToken = "old-token",
            RefreshToken = "refresh-token",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(1)
        });

        var service = new TestGmailService(CreateConfiguration(), storage)
        {
            RefreshHandler = (_, _) => Task.FromResult<string?>(null),
            AuthenticateHandler = _ => Task.FromResult(new OAuthResult
            {
                Success = true,
                AccessToken = "interactive-token",
                EmailAddress = "user@example.com",
                RefreshToken = "new-refresh",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                Provider = EmailProvider.Gmail
            })
        };

        var token = await service.GetGmailAccessTokenAsync("user@example.com");

        token.Should().Be("interactive-token");
        service.AuthenticateInvocations.Should().Be(1);
    }

    #region Additional OAuth Edge Cases

    [Fact]
    public async Task GetGmailAccessTokenAsync_NoStoredToken_Authenticates()
    {
        var storage = new FakeTokenStorageService();
        // No token seeded

        var service = new TestGmailService(CreateConfiguration(), storage)
        {
            AuthenticateHandler = _ => Task.FromResult(new OAuthResult
            {
                Success = true,
                AccessToken = "fresh-token",
                EmailAddress = "new@example.com",
                RefreshToken = "new-refresh",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                Provider = EmailProvider.Gmail
            })
        };

        var token = await service.GetGmailAccessTokenAsync("new@example.com");

        token.Should().Be("fresh-token");
        service.AuthenticateInvocations.Should().Be(1);
    }

    [Fact]
    public async Task GetGmailAccessTokenAsync_AuthenticationFails_ThrowsInvalidOperation()
    {
        var storage = new FakeTokenStorageService();

        var service = new TestGmailService(CreateConfiguration(), storage)
        {
            AuthenticateHandler = _ => Task.FromResult(new OAuthResult
            {
                Success = false,
                ErrorMessage = "User cancelled"
            })
        };

        var act = () => service.GetGmailAccessTokenAsync("user@example.com");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*User cancelled*");
    }

    [Fact]
    public async Task GetGmailAccessTokenAsync_AuthenticationThrows_PropagatesException()
    {
        var storage = new FakeTokenStorageService();

        var service = new TestGmailService(CreateConfiguration(), storage)
        {
            AuthenticateHandler = _ => throw new TaskCanceledException("User cancelled")
        };

        var act = () => service.GetGmailAccessTokenAsync("user@example.com");

        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    [Fact]
    public async Task GetGmailAccessTokenAsync_RefreshSucceeds_DoesNotAuthenticate()
    {
        var storage = new FakeTokenStorageService();
        storage.Seed(new StoredOAuthToken
        {
            Provider = EmailProvider.Gmail,
            AccountId = "user@example.com",
            AccessToken = "old-token",
            RefreshToken = "refresh-token",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(1) // Expiring soon
        });

        var service = new TestGmailService(CreateConfiguration(), storage)
        {
            RefreshHandler = async (token, ct) =>
            {
                var refreshed = token with
                {
                    AccessToken = "refreshed-successfully",
                    ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
                };
                await storage.StoreTokenAsync(refreshed, ct);
                return "refreshed-successfully";
            },
            AuthenticateHandler = _ => throw new InvalidOperationException("Should NOT authenticate")
        };

        var token = await service.GetGmailAccessTokenAsync("user@example.com");

        token.Should().Be("refreshed-successfully");
        service.AuthenticateInvocations.Should().Be(0);
    }

    [Fact]
    public void Constructor_ReadsConfigurationValues()
    {
        var storage = new FakeTokenStorageService();
        var config = CreateConfiguration();

        var service = new TestGmailService(config, storage);

        service.Should().NotBeNull();
    }

    [Fact]
    public async Task GetGmailAccessTokenAsync_ExpiredNoRefreshToken_Authenticates()
    {
        var storage = new FakeTokenStorageService();
        storage.Seed(new StoredOAuthToken
        {
            Provider = EmailProvider.Gmail,
            AccountId = "user@example.com",
            AccessToken = "expired-token",
            RefreshToken = null, // No refresh token
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-10) // Expired
        });

        var service = new TestGmailService(CreateConfiguration(), storage)
        {
            RefreshHandler = (_, _) => Task.FromResult<string?>(null),
            AuthenticateHandler = _ => Task.FromResult(new OAuthResult
            {
                Success = true,
                AccessToken = "new-token",
                EmailAddress = "user@example.com",
                RefreshToken = "new-refresh",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                Provider = EmailProvider.Gmail
            })
        };

        var token = await service.GetGmailAccessTokenAsync("user@example.com");

        token.Should().Be("new-token");
    }

    #endregion

    #region NotSupportedException Methods

    [Fact]
    public async Task AuthenticateOutlookAsync_ThrowsNotSupported()
    {
        var storage = new FakeTokenStorageService();
        var service = new TestGmailService(CreateConfiguration(), storage);

        var act = () => service.AuthenticateOutlookAsync();

        await act.Should().ThrowAsync<NotSupportedException>();
    }

    [Fact]
    public async Task GetOutlookAccessTokenAsync_ThrowsNotSupported()
    {
        var storage = new FakeTokenStorageService();
        var service = new TestGmailService(CreateConfiguration(), storage);

        var act = () => service.GetOutlookAccessTokenAsync("user@example.com");

        await act.Should().ThrowAsync<NotSupportedException>();
    }

    [Fact]
    public async Task AuthorizeOutlookAccountAsync_ThrowsNotSupported()
    {
        var storage = new FakeTokenStorageService();
        var service = new TestGmailService(CreateConfiguration(), storage);

        var act = () => service.AuthorizeOutlookAccountAsync();

        await act.Should().ThrowAsync<NotSupportedException>();
    }

    #endregion

    #region Input Validation

    [Fact]
    public async Task GetGmailAccessTokenAsync_EmptyEmail_ThrowsArgumentException()
    {
        var storage = new FakeTokenStorageService();
        var service = new TestGmailService(CreateConfiguration(), storage);

        var act = () => service.GetGmailAccessTokenAsync("");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetGmailAccessTokenAsync_NullEmail_ThrowsArgumentException()
    {
        var storage = new FakeTokenStorageService();
        var service = new TestGmailService(CreateConfiguration(), storage);

        var act = () => service.GetGmailAccessTokenAsync(null!);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    private sealed class TestGmailService : GmailOAuthService
    {
        private readonly FakeTokenStorageService _storage;

        public TestGmailService(IConfiguration configuration, FakeTokenStorageService storage)
            : base(configuration, NullLogger<GmailOAuthService>.Instance, storage)
        {
            _storage = storage;
        }

        public Func<StoredOAuthToken, CancellationToken, Task<string?>>? RefreshHandler { get; set; }

        public Func<CancellationToken, Task<OAuthResult>>? AuthenticateHandler { get; set; }

        public int AuthenticateInvocations { get; private set; }

        public override Task<OAuthResult> AuthenticateGmailAsync(CancellationToken cancellationToken = default)
        {
            AuthenticateInvocations++;
            if (AuthenticateHandler is null)
            {
                throw new InvalidOperationException("No authentication handler configured for test");
            }

            return AuthenticateHandler(cancellationToken);
        }

        protected override Task<string?> TryRefreshAsync(StoredOAuthToken token, CancellationToken cancellationToken)
        {
            if (RefreshHandler is null)
            {
                throw new InvalidOperationException("No refresh handler configured for test");
            }

            return RefreshHandler(token, cancellationToken);
        }

        protected override GoogleAuthorizationCodeFlow CreateAuthorizationFlow()
        {
            throw new InvalidOperationException("Test should not invoke Google authorization flow");
        }

        protected override Task<Profile> GetProfileAsync(UserCredential credential, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Test should not invoke Gmail profile lookup");
        }

        protected override Task PersistTokenAsync(string accountId, TokenResponse token, CancellationToken cancellationToken, string? fallbackRefreshToken = null)
        {
            var entry = new StoredOAuthToken
            {
                Provider = EmailProvider.Gmail,
                AccountId = accountId,
                AccessToken = token.AccessToken ?? string.Empty,
                RefreshToken = string.IsNullOrEmpty(token.RefreshToken) ? fallbackRefreshToken : token.RefreshToken,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30)
            };
            return _storage.StoreTokenAsync(entry, cancellationToken);
        }

        protected override Task RevokeByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

}
