using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;

namespace MIC.Infrastructure.Identity.Services;

public class GmailOAuthService : IEmailOAuth2Service
{
    private static readonly TimeSpan RefreshLeadTime = TimeSpan.FromMinutes(5);

    private readonly IConfiguration _configuration;
    private readonly ILogger<GmailOAuthService> _logger;
    private readonly ITokenStorageService _tokenStorage;
    private readonly string[] _scopes =
    {
        GmailService.Scope.GmailReadonly,
        GmailService.Scope.GmailSend,
        GmailService.Scope.GmailModify
    };

    public GmailOAuthService(
        IConfiguration configuration,
        ILogger<GmailOAuthService> logger,
        ITokenStorageService tokenStorage)
    {
        _configuration = configuration;
        _logger = logger;
        _tokenStorage = tokenStorage;
    }

    public virtual async Task<OAuthResult> AuthenticateGmailAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var flow = CreateAuthorizationFlow();
            var app = new AuthorizationCodeInstalledApp(flow, new LocalServerCodeReceiver());
            var stateKey = Guid.NewGuid().ToString("N");

            _logger.LogInformation("Starting Gmail OAuth flow");
            var credential = await app.AuthorizeAsync(stateKey, cancellationToken).ConfigureAwait(false);
            var profile = await GetProfileAsync(credential, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(profile.EmailAddress))
            {
                return new OAuthResult
                {
                    Success = false,
                    ErrorMessage = "Unable to determine Gmail account email address.",
                    Provider = EmailProvider.Gmail
                };
            }

            await PersistTokenAsync(profile.EmailAddress, credential.Token, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Gmail authentication successful for {Email}", profile.EmailAddress);

            return new OAuthResult
            {
                Success = true,
                EmailAddress = profile.EmailAddress,
                AccessToken = credential.Token.AccessToken,
                RefreshToken = credential.Token.RefreshToken,
                ExpiresAt = CalculateExpiry(credential.Token),
                Provider = EmailProvider.Gmail
            };
        }
        catch (TaskCanceledException)
        {
            return new OAuthResult
            {
                Success = false,
                ErrorMessage = "Gmail authentication timed out or was cancelled.",
                Provider = EmailProvider.Gmail
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gmail authentication failed");
            return new OAuthResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Provider = EmailProvider.Gmail
            };
        }
    }

    public async Task<string?> RefreshGmailTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        try
        {
            var flow = CreateAuthorizationFlow();
            var tokens = await _tokenStorage.GetTokensAsync(EmailProvider.Gmail, cancellationToken).ConfigureAwait(false);
            var entry = tokens.FirstOrDefault(t => t.RefreshToken == refreshToken);

            var response = await flow.RefreshTokenAsync(
                entry?.AccountId ?? "user",
                refreshToken,
                cancellationToken).ConfigureAwait(false);

            if (entry != null)
            {
                await PersistTokenAsync(entry.AccountId, response, cancellationToken, fallbackRefreshToken: refreshToken).ConfigureAwait(false);
            }

            return response.AccessToken;
        }
        catch (TokenResponseException ex) when (string.Equals(ex.Error?.Error, "invalid_grant", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Gmail refresh token invalid. Revoking stored token.");
            await RevokeByRefreshTokenAsync(refreshToken, cancellationToken).ConfigureAwait(false);
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gmail token refresh failed");
            return null;
        }
    }

    public async Task<bool> ValidateTokenAsync(string accessToken, EmailProvider provider)
    {
        if (provider != EmailProvider.Gmail)
        {
            return false;
        }

        try
        {
            var credential = GoogleCredential.FromAccessToken(accessToken);
            var service = new GmailService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Mbarie Intelligence Console"
            });

            var profile = await service.Users.GetProfile("me").ExecuteAsync().ConfigureAwait(false);
            return !string.IsNullOrWhiteSpace(profile.EmailAddress);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate Gmail token");
            return false;
        }
    }

    public Task<OAuthResult> AuthenticateOutlookAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException("AuthenticateOutlookAsync is not supported by GmailOAuthService. Use OutlookOAuthService.");

    public Task<string?> RefreshOutlookTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("RefreshOutlookTokenAsync is not supported by GmailOAuthService. Use OutlookOAuthService.");

    public Task<string> GetOutlookAccessTokenAsync(string userEmail, CancellationToken ct = default)
        => throw new NotSupportedException("GetOutlookAccessTokenAsync is not supported by GmailOAuthService. Use OutlookOAuthService.");

    public Task<bool> AuthorizeOutlookAccountAsync(CancellationToken ct = default)
        => throw new NotSupportedException("AuthorizeOutlookAccountAsync is not supported by GmailOAuthService. Use OutlookOAuthService.");

    public async Task<string> GetGmailAccessTokenAsync(string userEmail, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userEmail))
        {
            throw new ArgumentException("Email address is required", nameof(userEmail));
        }

        var storedToken = await _tokenStorage.GetTokenAsync(EmailProvider.Gmail, userEmail, ct).ConfigureAwait(false);
        if (storedToken is null)
        {
            var auth = await AuthenticateGmailAsync(ct).ConfigureAwait(false);
            if (!auth.Success || string.IsNullOrEmpty(auth.AccessToken))
            {
                throw new InvalidOperationException(auth.ErrorMessage ?? "Failed to acquire Gmail token.");
            }

            return auth.AccessToken;
        }

        if (IsExpiring(storedToken))
        {
            var refreshed = await TryRefreshAsync(storedToken, ct).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(refreshed))
            {
                return refreshed;
            }

            var reauth = await AuthenticateGmailAsync(ct).ConfigureAwait(false);
            if (!reauth.Success || string.IsNullOrEmpty(reauth.AccessToken))
            {
                throw new InvalidOperationException(reauth.ErrorMessage ?? "Failed to renew Gmail token interactively.");
            }

            return reauth.AccessToken;
        }

        return storedToken.AccessToken;
    }

    public async Task<bool> AuthorizeGmailAccountAsync(CancellationToken ct = default)
    {
        var result = await AuthenticateGmailAsync(ct).ConfigureAwait(false);
        return result.Success;
    }

    protected virtual GoogleAuthorizationCodeFlow CreateAuthorizationFlow()
    {
        var clientId = _configuration["OAuth2:Gmail:ClientId"] ?? string.Empty;
        var clientSecret = _configuration["OAuth2:Gmail:ClientSecret"] ?? string.Empty;

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new InvalidOperationException("Gmail OAuth credentials are not configured.");
        }

        return new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            },
            Scopes = _scopes,
            DataStore = new NullDataStore()
        });
    }

    protected virtual async Task<Profile> GetProfileAsync(UserCredential credential, CancellationToken cancellationToken)
    {
        var service = new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Mbarie Intelligence Console"
        });

        var profile = await service.Users.GetProfile("me").ExecuteAsync(cancellationToken).ConfigureAwait(false);
        return profile;
    }

    protected virtual async Task PersistTokenAsync(string accountId, TokenResponse token, CancellationToken cancellationToken, string? fallbackRefreshToken = null)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            _logger.LogWarning("Skipping Gmail token persistence because account identifier is missing.");
            return;
        }

        var issued = token.IssuedUtc != DateTime.MinValue ? token.IssuedUtc : DateTime.UtcNow;
        var refreshToken = string.IsNullOrWhiteSpace(token.RefreshToken) ? fallbackRefreshToken : token.RefreshToken;
        var scopes = !string.IsNullOrWhiteSpace(token.Scope)
            ? token.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            : _scopes;

        var entry = new StoredOAuthToken
        {
            Provider = EmailProvider.Gmail,
            AccountId = accountId,
            AccessToken = token.AccessToken ?? string.Empty,
            RefreshToken = refreshToken,
            ExpiresAtUtc = issued.AddSeconds(token.ExpiresInSeconds ?? 3600),
            Scopes = scopes,
            StoredAtUtc = DateTime.UtcNow
        };

        await _tokenStorage.StoreTokenAsync(entry, cancellationToken).ConfigureAwait(false);
    }

    private static DateTime CalculateExpiry(TokenResponse token)
    {
        var issued = token.IssuedUtc != DateTime.MinValue ? token.IssuedUtc : DateTime.UtcNow;
        return issued.AddSeconds(token.ExpiresInSeconds ?? 3600);
    }

    protected static bool IsExpiring(StoredOAuthToken token)
    {
        return DateTime.UtcNow.Add(RefreshLeadTime) >= token.ExpiresAtUtc;
    }

    protected virtual async Task<string?> TryRefreshAsync(StoredOAuthToken token, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(token.RefreshToken))
        {
            var refreshed = await RefreshGmailTokenAsync(token.RefreshToken, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(refreshed))
            {
                return refreshed;
            }
        }

        _logger.LogWarning("Stored Gmail token for {Account} is expiring and could not be refreshed silently.", token.AccountId);
        return null;
    }

    protected virtual async Task RevokeByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var tokens = await _tokenStorage.GetTokensAsync(EmailProvider.Gmail, cancellationToken).ConfigureAwait(false);
        foreach (var token in tokens)
        {
            if (token.RefreshToken == refreshToken)
            {
                await _tokenStorage.RemoveTokenAsync(EmailProvider.Gmail, token.AccountId, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    protected sealed class NullDataStore : IDataStore
    {
        public Task ClearAsync() => Task.CompletedTask;

        public Task DeleteAsync<T>(string key) => Task.CompletedTask;

        public Task<T> GetAsync<T>(string key) => Task.FromResult(default(T)!);

        public Task StoreAsync<T>(string key, T value) => Task.CompletedTask;
    }
}