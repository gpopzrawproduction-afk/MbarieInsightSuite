using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;

namespace MIC.Infrastructure.Identity.Services;

public class OutlookOAuthService : IEmailOAuth2Service
{
    private const string TokenCacheKey = "OUTLOOK_MSAL_CACHE";
    private static readonly TimeSpan RefreshLeadTime = TimeSpan.FromMinutes(5);

    private readonly IConfiguration _configuration;
    private readonly ILogger<OutlookOAuthService> _logger;
    private readonly ITokenStorageService _tokenStorage;
    private readonly string[] _scopes =
    {
        "https://graph.microsoft.com/Mail.Read",
        "https://graph.microsoft.com/Mail.Send",
        "https://graph.microsoft.com/Mail.ReadWrite",
        "https://graph.microsoft.com/User.Read"
    };

    public OutlookOAuthService(
        IConfiguration configuration,
        ILogger<OutlookOAuthService> logger,
        ITokenStorageService tokenStorage)
    {
        _configuration = configuration;
        _logger = logger;
        _tokenStorage = tokenStorage;
    }

    public Task<OAuthResult> AuthenticateGmailAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException("AuthenticateGmailAsync is not supported by OutlookOAuthService. Use GmailOAuthService.");

    public Task<string?> RefreshGmailTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("RefreshGmailTokenAsync is not supported by OutlookOAuthService. Use GmailOAuthService.");

    public Task<string> GetGmailAccessTokenAsync(string userEmail, CancellationToken ct = default)
        => throw new NotSupportedException("GetGmailAccessTokenAsync is not supported by OutlookOAuthService. Use GmailOAuthService.");

    public Task<bool> AuthorizeGmailAccountAsync(CancellationToken ct = default)
        => throw new NotSupportedException("AuthorizeGmailAccountAsync is not supported by OutlookOAuthService. Use GmailOAuthService.");

    public virtual async Task<OAuthResult> AuthenticateOutlookAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var app = BuildPublicClientApp();
            AuthenticationResult result;

            try
            {
                var accounts = await app.GetAccountsAsync().ConfigureAwait(false);
                var existingAccount = accounts.FirstOrDefault();

                if (existingAccount != null)
                {
                    result = await app.AcquireTokenSilent(_scopes, existingAccount)
                        .ExecuteAsync(cancellationToken)
                        .ConfigureAwait(false);
                    _logger.LogInformation("Outlook silent authentication succeeded for {Account}", existingAccount.Username);
                }
                else
                {
                    result = await AcquireInteractiveAsync(app, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (MsalUiRequiredException)
            {
                result = await AcquireInteractiveAsync(app, cancellationToken).ConfigureAwait(false);
            }

            var emailAddress = await ResolveUserEmailAsync(result.AccessToken, cancellationToken).ConfigureAwait(false)
                               ?? result.Account?.Username
                               ?? string.Empty;

            await PersistTokenAsync(emailAddress, result, cancellationToken).ConfigureAwait(false);

            return new OAuthResult
            {
                Success = true,
                EmailAddress = emailAddress,
                AccessToken = result.AccessToken,
                ExpiresAt = result.ExpiresOn.UtcDateTime,
                Provider = EmailProvider.Outlook
            };
        }
        catch (TaskCanceledException)
        {
            return new OAuthResult
            {
                Success = false,
                ErrorMessage = "Outlook authentication timed out or was cancelled by the user.",
                Provider = EmailProvider.Outlook
            };
        }
        catch (MsalServiceException ex) when (ex.ErrorCode == "user_canceled")
        {
            _logger.LogWarning("Outlook authentication cancelled by user.");
            return new OAuthResult
            {
                Success = false,
                ErrorMessage = "Authentication cancelled by user.",
                Provider = EmailProvider.Outlook
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Outlook authentication failed");
            return new OAuthResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Provider = EmailProvider.Outlook
            };
        }
    }

    public async Task<string?> RefreshOutlookTokenAsync(string refreshTokenOrAccountId, CancellationToken cancellationToken = default)
    {
        try
        {
            var app = BuildPublicClientApp();
            var accounts = await app.GetAccountsAsync().ConfigureAwait(false);

            var accountIdentifier = refreshTokenOrAccountId;
            if (!string.IsNullOrWhiteSpace(refreshTokenOrAccountId))
            {
                var storedTokens = await _tokenStorage.GetTokensAsync(EmailProvider.Outlook, cancellationToken).ConfigureAwait(false);
                var matched = storedTokens.FirstOrDefault(t => string.Equals(t.RefreshToken, refreshTokenOrAccountId, StringComparison.Ordinal));
                if (matched != null)
                {
                    accountIdentifier = matched.AccountId;
                }
            }

            IAccount? account = null;
            if (!string.IsNullOrWhiteSpace(accountIdentifier))
            {
                account = accounts.FirstOrDefault(a =>
                    string.Equals(a.Username, accountIdentifier, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(a.HomeAccountId.Identifier, accountIdentifier, StringComparison.OrdinalIgnoreCase));
            }

            account ??= accounts.FirstOrDefault();

            if (account is null)
            {
                _logger.LogWarning("No cached Outlook account available for silent refresh.");
                return null;
            }

            var result = await app.AcquireTokenSilent(_scopes, account)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            var emailAddress = account.Username
                               ?? await ResolveUserEmailAsync(result.AccessToken, cancellationToken).ConfigureAwait(false)
                               ?? accountIdentifier
                               ?? string.Empty;

            await PersistTokenAsync(emailAddress, result, cancellationToken).ConfigureAwait(false);
            return result.AccessToken;
        }
        catch (MsalUiRequiredException)
        {
            _logger.LogWarning("Silent refresh requires user interaction for account {Account}", refreshTokenOrAccountId);
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh Outlook token");
            return null;
        }
    }

    public async Task<bool> ValidateTokenAsync(string accessToken, EmailProvider provider)
    {
        if (provider != EmailProvider.Outlook)
        {
            return false;
        }

        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await httpClient.GetAsync("https://graph.microsoft.com/v1.0/me").ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate Outlook token");
            return false;
        }
    }

    public async Task<string> GetOutlookAccessTokenAsync(string userEmail, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userEmail))
        {
            throw new ArgumentException("Email address is required", nameof(userEmail));
        }

        var storedToken = await _tokenStorage.GetTokenAsync(EmailProvider.Outlook, userEmail, ct).ConfigureAwait(false);
        if (storedToken is null)
        {
            var authResult = await AuthenticateOutlookAsync(ct).ConfigureAwait(false);
            if (!authResult.Success || string.IsNullOrEmpty(authResult.AccessToken))
            {
                throw new InvalidOperationException(authResult.ErrorMessage ?? "Failed to acquire Outlook token.");
            }

            return authResult.AccessToken;
        }

        if (IsExpiring(storedToken))
        {
            var refreshed = await TryRefreshAsync(storedToken, ct).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(refreshed))
            {
                return refreshed;
            }

            var reauth = await AuthenticateOutlookAsync(ct).ConfigureAwait(false);
            if (!reauth.Success || string.IsNullOrEmpty(reauth.AccessToken))
            {
                throw new InvalidOperationException(reauth.ErrorMessage ?? "Failed to renew Outlook token interactively.");
            }

            return reauth.AccessToken;
        }

        return storedToken.AccessToken;
    }

    public async Task<bool> AuthorizeOutlookAccountAsync(CancellationToken ct = default)
    {
        var result = await AuthenticateOutlookAsync(ct).ConfigureAwait(false);
        return result.Success;
    }

    protected virtual IPublicClientApplication BuildPublicClientApp()
    {
        var clientId = _configuration["OAuth2:Outlook:ClientId"];
        var tenantId = _configuration["OAuth2:Outlook:TenantId"] ?? "common";
        var redirectUri = _configuration["OAuth2:Outlook:RedirectUri"] ?? "http://localhost";

        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new InvalidOperationException("Outlook OAuth ClientId is not configured.");
        }

        var app = PublicClientApplicationBuilder
            .Create(clientId)
            .WithAuthority(AzureCloudInstance.AzurePublic, tenantId)
            .WithRedirectUri(redirectUri)
            .WithLogging((level, message, containsPii) =>
            {
                if (!containsPii)
                {
                    _logger.LogDebug("MSAL[{Level}] {Message}", level, message);
                }
            })
            .Build();

        RegisterTokenCache(app.UserTokenCache);
        return app;
    }

    protected void RegisterTokenCache(ITokenCache tokenCache)
    {
        tokenCache.SetBeforeAccessAsync(async args =>
        {
            var payload = await _tokenStorage.GetPayloadAsync(TokenCacheKey).ConfigureAwait(false);
            if (payload is { Length: > 0 })
            {
                args.TokenCache.DeserializeMsalV3(payload, shouldClearExistingCache: true);
            }
        });

        tokenCache.SetAfterAccessAsync(async args =>
        {
            if (args.HasStateChanged)
            {
                var serialized = args.TokenCache.SerializeMsalV3();
                await _tokenStorage.StorePayloadAsync(TokenCacheKey, serialized).ConfigureAwait(false);
            }
        });
    }

    protected virtual async Task<AuthenticationResult> AcquireInteractiveAsync(IPublicClientApplication app, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting interactive Outlook authentication");
        return await app.AcquireTokenInteractive(_scopes)
            .WithPrompt(Prompt.SelectAccount)
            .WithUseEmbeddedWebView(false)
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    protected virtual async Task PersistTokenAsync(string accountId, AuthenticationResult result, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            _logger.LogWarning("Skipping token persistence because account identifier is missing.");
            return;
        }

        var scopes = result.Scopes?.ToArray() ?? Array.Empty<string>();
        var entry = new StoredOAuthToken
        {
            Provider = EmailProvider.Outlook,
            AccountId = accountId,
            AccessToken = result.AccessToken,
            ExpiresAtUtc = result.ExpiresOn.UtcDateTime,
            Scopes = scopes,
            StoredAtUtc = DateTime.UtcNow
        };

        await _tokenStorage.StoreTokenAsync(entry, cancellationToken).ConfigureAwait(false);
    }

    protected virtual async Task<string?> ResolveUserEmailAsync(string accessToken, CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await httpClient.GetAsync("https://graph.microsoft.com/v1.0/me", cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (document.RootElement.TryGetProperty("mail", out var mailElement) && mailElement.GetString() is { Length: > 0 } mail)
            {
                return mail;
            }

            if (document.RootElement.TryGetProperty("userPrincipalName", out var upnElement))
            {
                return upnElement.GetString();
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve Outlook user email via Microsoft Graph");
            return null;
        }
    }

    private static bool IsExpiring(StoredOAuthToken token)
    {
        return DateTime.UtcNow.Add(RefreshLeadTime) >= token.ExpiresAtUtc;
    }

    protected virtual async Task<string?> TryRefreshAsync(StoredOAuthToken storedToken, CancellationToken cancellationToken)
    {
        var refreshed = await RefreshOutlookTokenAsync(storedToken.AccountId, cancellationToken).ConfigureAwait(false);
        return string.IsNullOrEmpty(refreshed) ? null : refreshed;
    }
}