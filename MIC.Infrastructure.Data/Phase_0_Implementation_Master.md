# PHASE 0: FOUNDATION STABILIZATION
## Complete Implementation Guide & Technical Specifications

**Project:** MBARIE Email Intelligence Platform  
**Phase:** 0 - Foundation Stabilization  
**Status:** APPROVED - Ready for Execution  
**Estimated Duration:** 2 Weeks  
**Target Completion:** Week 2, Day 5 + Validation  

---

## 📋 EXECUTIVE SUMMARY

This document provides complete technical specifications for Phase 0 implementation. Phase 0 focuses on stabilizing the existing codebase by completing partial implementations, removing all NotImplementedException placeholders, and establishing a robust testing foundation.

**Phase 0 Goals:**
1. ✅ Complete OAuth authentication for Outlook and Gmail
2. ✅ Implement production-ready settings persistence system
3. ✅ Finish email sync including attachment storage
4. ✅ Complete notification center functionality
5. ✅ Establish comprehensive testing infrastructure (80% coverage target)
6. ✅ Standardize error handling and logging across all services

**Critical Success Factors:**
- NO mock data in any implementation
- NO NotImplementedException in final codebase
- 80% minimum test coverage for all Phase 0 code
- All implementations must follow existing architecture patterns (DI, CQRS, Clean Architecture)

---

## 🎯 WORK PACKAGE OVERVIEW

| WP | Title | Priority | Duration | Dependencies |
|---|---|---|---|---|
| WP-0.1 | OAuth Implementation | CRITICAL | 2 days | None |
| WP-0.2 | Settings Persistence | CRITICAL | 2 days | None |
| WP-0.3 | Email Sync Completion | CRITICAL | 2 days | WP-0.1 |
| WP-0.4 | Testing Infrastructure | CRITICAL | 1 day | None |
| WP-0.5 | Notification Center | HIGH | 2 days | WP-0.2 |
| WP-0.6 | Error Handling | HIGH | 1 day | All above |

**Total Implementation Time:** 10 days  
**Validation & Testing:** 3 days  
**Total Phase Duration:** 13 days (2 weeks + 1 day buffer)

---

## 📦 WP-0.1: OAUTH IMPLEMENTATION COMPLETION

### Overview
Complete OAuth2 authentication flows for Outlook and Gmail, removing all NotImplementedException placeholders and implementing production-ready token management.

### Current State Analysis
**Files Affected:**
- `MIC.Infrastructure.Services/Email/OutlookOAuthService.cs` (lines 214-272)
- `MIC.Infrastructure.Services/Email/GmailOAuthService.cs` (lines 188-250)

**Current Issues:**
- Methods throw `NotImplementedException`
- Token storage not implemented
- No token refresh logic
- Missing error handling

### Technical Specifications

#### 1. Outlook OAuth Implementation (MSAL)

**Required NuGet Packages:**
```xml
<PackageReference Include="Microsoft.Identity.Client" Version="4.59.0" />
<PackageReference Include="Microsoft.Graph" Version="5.40.0" />
```

**Configuration Settings:**
```json
{
  "OutlookOAuth": {
    "ClientId": "your-azure-app-client-id",
    "TenantId": "common",
    "RedirectUri": "http://localhost:5000/oauth/callback",
    "Scopes": [
      "https://graph.microsoft.com/Mail.Read",
      "https://graph.microsoft.com/Mail.ReadWrite",
      "https://graph.microsoft.com/Mail.Send",
      "offline_access"
    ]
  }
}
```

**Implementation Pattern:**
```csharp
public class OutlookOAuthService : IOAuthService
{
    private readonly IPublicClientApplication _msalClient;
    private readonly ITokenStorageService _tokenStorage;
    private readonly ILogger<OutlookOAuthService> _logger;
    private readonly string[] _scopes;

    public OutlookOAuthService(
        IConfiguration configuration,
        ITokenStorageService tokenStorage,
        ILogger<OutlookOAuthService> logger)
    {
        _tokenStorage = tokenStorage;
        _logger = logger;
        
        var config = configuration.GetSection("OutlookOAuth");
        _scopes = config.GetSection("Scopes").Get<string[]>();
        
        _msalClient = PublicClientApplicationBuilder
            .Create(config["ClientId"])
            .WithAuthority(AzureCloudInstance.AzurePublic, config["TenantId"])
            .WithRedirectUri(config["RedirectUri"])
            .Build();
            
        ConfigureTokenCache();
    }

    public async Task<AuthenticationResult> AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting Outlook OAuth authentication");
            
            // Try silent authentication first
            var accounts = await _msalClient.GetAccountsAsync();
            if (accounts.Any())
            {
                try
                {
                    var result = await _msalClient
                        .AcquireTokenSilent(_scopes, accounts.FirstOrDefault())
                        .ExecuteAsync(cancellationToken);
                        
                    await StoreTokenAsync(result);
                    return result;
                }
                catch (MsalUiRequiredException)
                {
                    _logger.LogInformation("Silent auth failed, prompting user");
                }
            }
            
            // Interactive authentication
            var interactiveResult = await _msalClient
                .AcquireTokenInteractive(_scopes)
                .WithPrompt(Prompt.SelectAccount)
                .ExecuteAsync(cancellationToken);
                
            await StoreTokenAsync(interactiveResult);
            
            _logger.LogInformation("Outlook OAuth authentication successful for {Account}", 
                interactiveResult.Account.Username);
                
            return interactiveResult;
        }
        catch (MsalException ex)
        {
            _logger.LogError(ex, "Outlook OAuth authentication failed");
            throw new EmailAuthException("Failed to authenticate with Outlook", ex);
        }
    }

    public async Task<string> GetAccessTokenAsync(string accountId, CancellationToken cancellationToken = default)
    {
        try
        {
            var accounts = await _msalClient.GetAccountsAsync();
            var account = accounts.FirstOrDefault(a => a.HomeAccountId.Identifier == accountId);
            
            if (account == null)
            {
                throw new EmailAuthException($"No account found with ID: {accountId}");
            }
            
            var result = await _msalClient
                .AcquireTokenSilent(_scopes, account)
                .ExecuteAsync(cancellationToken);
                
            await StoreTokenAsync(result);
            return result.AccessToken;
        }
        catch (MsalUiRequiredException)
        {
            _logger.LogWarning("Token refresh requires user interaction for account {AccountId}", accountId);
            throw new EmailAuthException("Token refresh requires user re-authentication");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get access token for account {AccountId}", accountId);
            throw new EmailAuthException("Failed to retrieve access token", ex);
        }
    }

    public async Task RevokeTokenAsync(string accountId, CancellationToken cancellationToken = default)
    {
        try
        {
            var accounts = await _msalClient.GetAccountsAsync();
            var account = accounts.FirstOrDefault(a => a.HomeAccountId.Identifier == accountId);
            
            if (account != null)
            {
                await _msalClient.RemoveAsync(account);
                await _tokenStorage.DeleteTokenAsync(accountId);
                _logger.LogInformation("Successfully revoked token for account {AccountId}", accountId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke token for account {AccountId}", accountId);
            throw;
        }
    }

    private void ConfigureTokenCache()
    {
        // Implement persistent token cache using MSAL token cache serialization
        // Store in encrypted format using _tokenStorage service
    }

    private async Task StoreTokenAsync(AuthenticationResult result)
    {
        var tokenData = new StoredToken
        {
            AccountId = result.Account.HomeAccountId.Identifier,
            AccessToken = result.AccessToken,
            RefreshToken = null, // MSAL handles refresh internally
            ExpiresAt = result.ExpiresOn.UtcDateTime,
            Scopes = string.Join(",", _scopes),
            Provider = "Outlook"
        };
        
        await _tokenStorage.StoreTokenAsync(tokenData);
    }
}
```

#### 2. Gmail OAuth Implementation

**Required NuGet Packages:**
```xml
<PackageReference Include="Google.Apis.Auth" Version="1.64.0" />
<PackageReference Include="Google.Apis.Gmail.v1" Version="1.64.0.3238" />
```

**Configuration Settings:**
```json
{
  "GmailOAuth": {
    "ClientId": "your-google-client-id.apps.googleusercontent.com",
    "ClientSecret": "your-google-client-secret",
    "RedirectUri": "http://localhost:5000/oauth/callback",
    "Scopes": [
      "https://www.googleapis.com/auth/gmail.readonly",
      "https://www.googleapis.com/auth/gmail.send",
      "https://www.googleapis.com/auth/gmail.modify"
    ]
  }
}
```

**Implementation Pattern:**
```csharp
public class GmailOAuthService : IOAuthService
{
    private readonly ITokenStorageService _tokenStorage;
    private readonly ILogger<GmailOAuthService> _logger;
    private readonly ClientSecrets _clientSecrets;
    private readonly string[] _scopes;

    public GmailOAuthService(
        IConfiguration configuration,
        ITokenStorageService tokenStorage,
        ILogger<GmailOAuthService> logger)
    {
        _tokenStorage = tokenStorage;
        _logger = logger;
        
        var config = configuration.GetSection("GmailOAuth");
        _scopes = config.GetSection("Scopes").Get<string[]>();
        
        _clientSecrets = new ClientSecrets
        {
            ClientId = config["ClientId"],
            ClientSecret = config["ClientSecret"]
        };
    }

    public async Task<UserCredential> AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting Gmail OAuth authentication");
            
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                _clientSecrets,
                _scopes,
                "user",
                cancellationToken,
                new TokenStorageDataStore(_tokenStorage));
                
            await StoreTokenAsync(credential);
            
            _logger.LogInformation("Gmail OAuth authentication successful");
            return credential;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gmail OAuth authentication failed");
            throw new EmailAuthException("Failed to authenticate with Gmail", ex);
        }
    }

    public async Task<string> GetAccessTokenAsync(string accountId, CancellationToken cancellationToken = default)
    {
        try
        {
            var storedToken = await _tokenStorage.GetTokenAsync(accountId);
            
            if (storedToken == null)
            {
                throw new EmailAuthException($"No stored token found for account: {accountId}");
            }
            
            // Check if token needs refresh
            if (storedToken.ExpiresAt <= DateTime.UtcNow.AddMinutes(5))
            {
                _logger.LogInformation("Token expired, refreshing for account {AccountId}", accountId);
                
                var tokenResponse = await RefreshTokenAsync(storedToken.RefreshToken, cancellationToken);
                
                storedToken.AccessToken = tokenResponse.AccessToken;
                storedToken.ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresInSeconds ?? 3600);
                
                await _tokenStorage.StoreTokenAsync(storedToken);
            }
            
            return storedToken.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get access token for account {AccountId}", accountId);
            throw new EmailAuthException("Failed to retrieve access token", ex);
        }
    }

    private async Task<TokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var request = new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = _clientSecrets
        }.Build();
        
        var token = new TokenResponse
        {
            RefreshToken = refreshToken
        };
        
        return await token.RefreshTokenAsync(request, cancellationToken);
    }

    public async Task RevokeTokenAsync(string accountId, CancellationToken cancellationToken = default)
    {
        try
        {
            var storedToken = await _tokenStorage.GetTokenAsync(accountId);
            
            if (storedToken != null)
            {
                // Revoke with Google
                await RevokeGoogleTokenAsync(storedToken.AccessToken, cancellationToken);
                
                // Delete from storage
                await _tokenStorage.DeleteTokenAsync(accountId);
                
                _logger.LogInformation("Successfully revoked token for account {AccountId}", accountId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke token for account {AccountId}", accountId);
            throw;
        }
    }

    private async Task RevokeGoogleTokenAsync(string token, CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.PostAsync(
            $"https://oauth2.googleapis.com/revoke?token={token}",
            null,
            cancellationToken);
            
        response.EnsureSuccessStatusCode();
    }

    private async Task StoreTokenAsync(UserCredential credential)
    {
        var token = credential.Token;
        var tokenData = new StoredToken
        {
            AccountId = "user", // Should be email address
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken,
            ExpiresAt = token.IssuedUtc.AddSeconds(token.ExpiresInSeconds ?? 3600),
            Scopes = string.Join(",", _scopes),
            Provider = "Gmail"
        };
        
        await _tokenStorage.StoreTokenAsync(tokenData);
    }
}
```

#### 3. Token Storage Service

**Implementation:**
```csharp
public class TokenStorageService : ITokenStorageService
{
    private readonly IDbContextFactory<MICDbContext> _contextFactory;
    private readonly ILogger<TokenStorageService> _logger;

    public async Task StoreTokenAsync(StoredToken token)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var existing = await context.StoredTokens
            .FirstOrDefaultAsync(t => t.AccountId == token.AccountId);
            
        if (existing != null)
        {
            // Update existing
            existing.AccessToken = EncryptToken(token.AccessToken);
            existing.RefreshToken = token.RefreshToken != null ? EncryptToken(token.RefreshToken) : null;
            existing.ExpiresAt = token.ExpiresAt;
            existing.LastUpdated = DateTime.UtcNow;
        }
        else
        {
            // Create new
            token.AccessToken = EncryptToken(token.AccessToken);
            token.RefreshToken = token.RefreshToken != null ? EncryptToken(token.RefreshToken) : null;
            token.CreatedAt = DateTime.UtcNow;
            token.LastUpdated = DateTime.UtcNow;
            context.StoredTokens.Add(token);
        }
        
        await context.SaveChangesAsync();
        _logger.LogInformation("Token stored for account {AccountId}", token.AccountId);
    }

    public async Task<StoredToken> GetTokenAsync(string accountId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var token = await context.StoredTokens
            .FirstOrDefaultAsync(t => t.AccountId == accountId);
            
        if (token != null)
        {
            token.AccessToken = DecryptToken(token.AccessToken);
            token.RefreshToken = token.RefreshToken != null ? DecryptToken(token.RefreshToken) : null;
        }
        
        return token;
    }

    public async Task DeleteTokenAsync(string accountId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var token = await context.StoredTokens
            .FirstOrDefaultAsync(t => t.AccountId == accountId);
            
        if (token != null)
        {
            context.StoredTokens.Remove(token);
            await context.SaveChangesAsync();
            _logger.LogInformation("Token deleted for account {AccountId}", accountId);
        }
    }

    private string EncryptToken(string token)
    {
        // Use Windows DPAPI for encryption
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var encryptedBytes = ProtectedData.Protect(
            tokenBytes,
            null,
            DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encryptedBytes);
    }

    private string DecryptToken(string encryptedToken)
    {
        var encryptedBytes = Convert.FromBase64String(encryptedToken);
        var decryptedBytes = ProtectedData.Unprotect(
            encryptedBytes,
            null,
            DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(decryptedBytes);
    }
}
```

#### Database Schema

**Migration:**
```csharp
public partial class AddStoredTokens : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "StoredTokens",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                AccountId = table.Column<string>(maxLength: 255, nullable: false),
                Provider = table.Column<string>(maxLength: 50, nullable: false),
                AccessToken = table.Column<string>(nullable: false),
                RefreshToken = table.Column<string>(nullable: true),
                ExpiresAt = table.Column<DateTime>(nullable: false),
                Scopes = table.Column<string>(nullable: true),
                CreatedAt = table.Column<DateTime>(nullable: false),
                LastUpdated = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StoredTokens", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_StoredTokens_AccountId",
            table: "StoredTokens",
            column: "AccountId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "StoredTokens");
    }
}
```

### Unit Testing Requirements

**Test File:** `MIC.Tests.Unit/Infrastructure/Services/OutlookOAuthServiceTests.cs`

```csharp
public class OutlookOAuthServiceTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ITokenStorageService> _mockTokenStorage;
    private readonly Mock<ILogger<OutlookOAuthService>> _mockLogger;
    
    [Fact]
    public async Task AuthenticateAsync_FirstTime_ShouldPromptUserAndStoreToken()
    {
        // Arrange
        var service = CreateService();
        
        // Act
        var result = await service.AuthenticateAsync();
        
        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.AccessToken);
        _mockTokenStorage.Verify(x => x.StoreTokenAsync(It.IsAny<StoredToken>()), Times.Once);
    }
    
    [Fact]
    public async Task GetAccessTokenAsync_ValidToken_ShouldReturnToken()
    {
        // Arrange
        var accountId = "test-account";
        var storedToken = new StoredToken
        {
            AccountId = accountId,
            AccessToken = "valid-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        
        _mockTokenStorage
            .Setup(x => x.GetTokenAsync(accountId))
            .ReturnsAsync(storedToken);
            
        var service = CreateService();
        
        // Act
        var token = await service.GetAccessTokenAsync(accountId);
        
        // Assert
        Assert.Equal("valid-token", token);
    }
    
    [Fact]
    public async Task GetAccessTokenAsync_ExpiredToken_ShouldRefreshAndReturnNewToken()
    {
        // Test token refresh logic
    }
    
    [Fact]
    public async Task RevokeTokenAsync_ValidAccount_ShouldDeleteToken()
    {
        // Test token revocation
    }
    
    // Additional tests for error scenarios
}
```

### Acceptance Criteria Checklist

- [ ] User can authenticate with Outlook using OAuth2
- [ ] User can authenticate with Gmail using OAuth2
- [ ] Tokens are encrypted and stored securely
- [ ] Tokens automatically refresh before expiration
- [ ] Multi-account support works correctly
- [ ] Token revocation removes all stored data
- [ ] All error scenarios handled with appropriate exceptions
- [ ] Unit tests achieve 80%+ coverage
- [ ] Integration tests pass for both providers
- [ ] No NotImplementedException in codebase

---

## 📦 WP-0.2: SETTINGS PERSISTENCE SYSTEM

### Overview
Implement a complete, production-ready settings persistence system with database storage, cloud sync capability, and migration support.

### Technical Specifications

#### 1. Settings Data Model

```csharp
public class Setting
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string Category { get; set; }
    public string Key { get; set; }
    public string Value { get; set; }
    public string ValueType { get; set; } // String, Int, Bool, Json
    public DateTime LastModified { get; set; }
    public string SyncStatus { get; set; } // Local, Synced, Pending
}

public class SettingHistory
{
    public int Id { get; set; }
    public int SettingId { get; set; }
    public string OldValue { get; set; }
    public string NewValue { get; set; }
    public DateTime ChangedAt { get; set; }
    public string ChangedBy { get; set; }
    
    public Setting Setting { get; set; }
}
```

#### 2. Settings Service Interface

```csharp
public interface ISettingsService
{
    // Basic CRUD
    Task<T> GetSettingAsync<T>(string category, string key, T defaultValue = default);
    Task SetSettingAsync<T>(string category, string key, T value);
    Task<bool> DeleteSettingAsync(string category, string key);
    Task<Dictionary<string, object>> GetCategorySettingsAsync(string category);
    
    // Bulk operations
    Task SetMultipleSettingsAsync(Dictionary<string, object> settings, string category);
    Task<Dictionary<string, Dictionary<string, object>>> GetAllSettingsAsync();
    
    // Import/Export
    Task<string> ExportSettingsAsync();
    Task ImportSettingsAsync(string json);
    
    // Cloud sync
    Task SyncWithCloudAsync();
    Task<SyncStatus> GetSyncStatusAsync();
    
    // Events
    event EventHandler<SettingChangedEventArgs> SettingChanged;
}
```

#### 3. Settings Service Implementation

```csharp
public class SettingsService : ISettingsService
{
    private readonly IDbContextFactory<MICDbContext> _contextFactory;
    private readonly ILogger<SettingsService> _logger;
    private readonly ICloudSyncService _cloudSync;
    private readonly string _userId;
    
    public event EventHandler<SettingChangedEventArgs> SettingChanged;

    public async Task<T> GetSettingAsync<T>(string category, string key, T defaultValue = default)
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var setting = await context.Settings
                .FirstOrDefaultAsync(s => 
                    s.UserId == _userId && 
                    s.Category == category && 
                    s.Key == key);
                    
            if (setting == null)
            {
                return defaultValue;
            }
            
            return DeserializeValue<T>(setting.Value, setting.ValueType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get setting {Category}.{Key}", category, key);
            return defaultValue;
        }
    }

    public async Task SetSettingAsync<T>(string category, string key, T value)
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var setting = await context.Settings
                .FirstOrDefaultAsync(s => 
                    s.UserId == _userId && 
                    s.Category == category && 
                    s.Key == key);
                    
            var (serializedValue, valueType) = SerializeValue(value);
            var now = DateTime.UtcNow;
            
            if (setting != null)
            {
                // Store history
                context.SettingHistory.Add(new SettingHistory
                {
                    SettingId = setting.Id,
                    OldValue = setting.Value,
                    NewValue = serializedValue,
                    ChangedAt = now,
                    ChangedBy = _userId
                });
                
                // Update setting
                setting.Value = serializedValue;
                setting.ValueType = valueType;
                setting.LastModified = now;
                setting.SyncStatus = "Pending";
            }
            else
            {
                // Create new setting
                setting = new Setting
                {
                    UserId = _userId,
                    Category = category,
                    Key = key,
                    Value = serializedValue,
                    ValueType = valueType,
                    LastModified = now,
                    SyncStatus = "Pending"
                };
                context.Settings.Add(setting);
            }
            
            await context.SaveChangesAsync();
            
            _logger.LogInformation("Setting saved: {Category}.{Key}", category, key);
            
            // Raise event
            SettingChanged?.Invoke(this, new SettingChangedEventArgs(category, key, value));
            
            // Queue for cloud sync
            await QueueForSyncAsync(setting);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set setting {Category}.{Key}", category, key);
            throw new SettingsException($"Failed to save setting {category}.{key}", ex);
        }
    }

    public async Task<string> ExportSettingsAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var settings = await context.Settings
            .Where(s => s.UserId == _userId)
            .ToListAsync();
            
        var grouped = settings
            .GroupBy(s => s.Category)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(
                    s => s.Key,
                    s => DeserializeValue<object>(s.Value, s.ValueType)
                )
            );
            
        return JsonSerializer.Serialize(grouped, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
    }

    public async Task ImportSettingsAsync(string json)
    {
        var settings = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(json);
        
        foreach (var category in settings)
        {
            foreach (var setting in category.Value)
            {
                await SetSettingAsync(category.Key, setting.Key, setting.Value);
            }
        }
    }

    public async Task SyncWithCloudAsync()
    {
        // Implementation for cloud synchronization
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var pendingSettings = await context.Settings
                .Where(s => s.UserId == _userId && s.SyncStatus == "Pending")
                .ToListAsync();
                
            if (!pendingSettings.Any())
            {
                _logger.LogInformation("No pending settings to sync");
                return;
            }
            
            await _cloudSync.SyncSettingsAsync(pendingSettings);
            
            // Update sync status
            foreach (var setting in pendingSettings)
            {
                setting.SyncStatus = "Synced";
            }
            
            await context.SaveChangesAsync();
            
            _logger.LogInformation("Synced {Count} settings to cloud", pendingSettings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cloud sync failed");
            throw new SettingsException("Failed to sync settings with cloud", ex);
        }
    }

    private (string value, string type) SerializeValue<T>(T value)
    {
        var type = typeof(T);
        
        if (type == typeof(string))
        {
            return (value?.ToString() ?? string.Empty, "String");
        }
        else if (type == typeof(int) || type == typeof(long))
        {
            return (value?.ToString() ?? "0", "Int");
        }
        else if (type == typeof(bool))
        {
            return (value?.ToString()?.ToLower() ?? "false", "Bool");
        }
        else if (type == typeof(double) || type == typeof(decimal))
        {
            return (value?.ToString() ?? "0.0", "Double");
        }
        else
        {
            return (JsonSerializer.Serialize(value), "Json");
        }
    }

    private T DeserializeValue<T>(string value, string valueType)
    {
        try
        {
            return valueType switch
            {
                "String" => (T)(object)value,
                "Int" => (T)(object)int.Parse(value),
                "Bool" => (T)(object)bool.Parse(value),
                "Double" => (T)(object)double.Parse(value),
                "Json" => JsonSerializer.Deserialize<T>(value),
                _ => default(T)
            };
        }
        catch
        {
            return default(T);
        }
    }

    private async Task QueueForSyncAsync(Setting setting)
    {
        // Queue setting for background sync
        // This is called after saving to database
    }
}
```

#### 4. Database Schema

**Migration:**
```csharp
public partial class AddSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Settings",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<string>(maxLength: 255, nullable: false),
                Category = table.Column<string>(maxLength: 100, nullable: false),
                Key = table.Column<string>(maxLength: 100, nullable: false),
                Value = table.Column<string>(nullable: false),
                ValueType = table.Column<string>(maxLength: 20, nullable: false),
                LastModified = table.Column<DateTime>(nullable: false),
                SyncStatus = table.Column<string>(maxLength: 20, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Settings", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Settings_UserId_Category_Key",
            table: "Settings",
            columns: new[] { "UserId", "Category", "Key" },
            unique: true);

        migrationBuilder.CreateTable(
            name: "SettingHistory",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                SettingId = table.Column<int>(nullable: false),
                OldValue = table.Column<string>(nullable: true),
                NewValue = table.Column<string>(nullable: false),
                ChangedAt = table.Column<DateTime>(nullable: false),
                ChangedBy = table.Column<string>(maxLength: 255, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SettingHistory", x => x.Id);
                table.ForeignKey(
                    name: "FK_SettingHistory_Settings_SettingId",
                    column: x => x.SettingId,
                    principalTable: "Settings",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SettingHistory_SettingId",
            table: "SettingHistory",
            column: "SettingId");

        migrationBuilder.CreateTable(
            name: "SettingsSchema",
            columns: table => new
            {
                Version = table.Column<int>(nullable: false),
                AppliedAt = table.Column<DateTime>(nullable: false),
                Description = table.Column<string>(maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SettingsSchema", x => x.Version);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "SettingHistory");
        migrationBuilder.DropTable(name: "Settings");
        migrationBuilder.DropTable(name: "SettingsSchema");
    }
}
```

### Pre-defined Setting Categories

**Implementation:**
```csharp
public static class SettingCategories
{
    public const string UserPreferences = "UserPreferences";
    public const string EmailSettings = "EmailSettings";
    public const string NotificationSettings = "NotificationSettings";
    public const string SecuritySettings = "SecuritySettings";
    public const string AISettings = "AISettings";
    public const string PerformanceSettings = "PerformanceSettings";
}

public static class UserPreferenceKeys
{
    public const string Theme = "Theme";
    public const string Language = "Language";
    public const string DateFormat = "DateFormat";
    public const string TimeFormat = "TimeFormat";
    public const string StartupView = "StartupView";
}

public static class EmailSettingKeys
{
    public const string DefaultAccount = "DefaultAccount";
    public const string SignatureHtml = "SignatureHtml";
    public const string AutoBcc = "AutoBcc";
    public const string SendAndArchive = "SendAndArchive";
    public const string SyncInterval = "SyncInterval"; // minutes
}

public static class NotificationSettingKeys
{
    public const string EnableDesktopNotifications = "EnableDesktopNotifications";
    public const string EnableSounds = "EnableSounds";
    public const string DndEnabled = "DndEnabled";
    public const string DndStartTime = "DndStartTime";
    public const string DndEndTime = "DndEndTime";
    public const string VipSenders = "VipSenders"; // JSON array
}
```

### Unit Testing Requirements

```csharp
public class SettingsServiceTests
{
    [Fact]
    public async Task GetSettingAsync_ExistingSetting_ShouldReturnValue()
    {
        // Arrange
        var service = CreateService();
        await service.SetSettingAsync(SettingCategories.UserPreferences, "TestKey", "TestValue");
        
        // Act
        var value = await service.GetSettingAsync<string>(SettingCategories.UserPreferences, "TestKey");
        
        // Assert
        Assert.Equal("TestValue", value);
    }
    
    [Fact]
    public async Task GetSettingAsync_NonExistentSetting_ShouldReturnDefault()
    {
        // Arrange
        var service = CreateService();
        
        // Act
        var value = await service.GetSettingAsync<string>("Category", "NonExistent", "Default");
        
        // Assert
        Assert.Equal("Default", value);
    }
    
    [Fact]
    public async Task SetSettingAsync_NewSetting_ShouldCreateAndRaiseEvent()
    {
        // Arrange
        var service = CreateService();
        var eventRaised = false;
        service.SettingChanged += (s, e) => eventRaised = true;
        
        // Act
        await service.SetSettingAsync("Category", "Key", "Value");
        
        // Assert
        Assert.True(eventRaised);
    }
    
    [Fact]
    public async Task SetSettingAsync_ExistingSetting_ShouldUpdateAndCreateHistory()
    {
        // Test update with history tracking
    }
    
    [Fact]
    public async Task ExportSettingsAsync_ShouldReturnJsonWithAllSettings()
    {
        // Test export functionality
    }
    
    [Fact]
    public async Task ImportSettingsAsync_ValidJson_ShouldImportAllSettings()
    {
        // Test import functionality
    }
    
    // Additional tests for complex types, sync, etc.
}
```

### Acceptance Criteria Checklist

- [ ] All setting types (string, int, bool, json) supported
- [ ] Settings persist across application restarts
- [ ] Setting changes trigger events for reactive UI
- [ ] History tracking works for all changes
- [ ] Export produces valid JSON
- [ ] Import correctly restores settings
- [ ] Concurrent access handled safely
- [ ] Unit tests achieve 80%+ coverage
- [ ] Integration tests pass
- [ ] Performance acceptable (< 10ms for reads)

---

*(Continued in next sections: WP-0.3 through WP-0.6, Testing Requirements, and Execution Schedule)*

---

## 📦 WP-0.3: EMAIL SYNC SERVICE COMPLETION

### Overview
Complete the historical email sync service with attachment storage, sync state management, and robust error handling.

### Current State Analysis
**File:** `MIC.Infrastructure.Data/Services/RealEmailSyncService.Historical.cs`

**Issues:**
- Attachment storage not implemented
- Sync state not persisted
- No resume capability
- Missing duplicate detection
- Error handling incomplete

### Technical Specifications

#### 1. Enhanced Email Sync Service

```csharp
public class EmailSyncService : IEmailSyncService
{
    private readonly IEmailRepository _emailRepository;
    private readonly IAttachmentStorageService _attachmentStorage;
    private readonly ISyncStateRepository _syncStateRepository;
    private readonly ILogger<EmailSyncService> _logger;
    private readonly SemaphoreSlim _syncLock = new(3); // Max 3 concurrent folder syncs

    public async Task<SyncResult> SyncAccountAsync(
        string accountId, 
        SyncOptions options,
        IProgress<SyncProgress> progress = null,
        CancellationToken cancellationToken = default)
    {
        var result = new SyncResult { AccountId = accountId, StartTime = DateTime.UtcNow };
        
        try
        {
            _logger.LogInformation("Starting sync for account {AccountId}", accountId);
            
            var folders = await GetFoldersToSyncAsync(accountId, options);
            var totalFolders = folders.Count;
            var completedFolders = 0;
            
            foreach (var folder in folders)
            {
                await _syncLock.WaitAsync(cancellationToken);
                
                try
                {
                    var folderResult = await SyncFolderAsync(
                        accountId, 
                        folder, 
                        options, 
                        cancellationToken);
                    
                    result.FolderResults.Add(folderResult);
                    completedFolders++;
                    
                    progress?.Report(new SyncProgress
                    {
                        TotalFolders = totalFolders,
                        CompletedFolders = completedFolders,
                        CurrentFolder = folder.Name,
                        TotalEmails = folderResult.TotalEmails,
                        SyncedEmails = folderResult.SyncedEmails
                    });
                }
                finally
                {
                    _syncLock.Release();
                }
            }
            
            result.EndTime = DateTime.UtcNow;
            result.Status = SyncStatus.Completed;
            
            _logger.LogInformation(
                "Sync completed for account {AccountId}. Synced {Count} emails", 
                accountId, 
                result.TotalEmailsSynced);
                
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync failed for account {AccountId}", accountId);
            result.Status = SyncStatus.Failed;
            result.Error = ex.Message;
            return result;
        }
    }

    private async Task<FolderSyncResult> SyncFolderAsync(
        string accountId,
        EmailFolder folder,
        SyncOptions options,
        CancellationToken cancellationToken)
    {
        var result = new FolderSyncResult
        {
            FolderId = folder.Id,
            FolderName = folder.Name,
            StartTime = DateTime.UtcNow
        };
        
        try
        {
            // Get sync state for resume capability
            var syncState = await _syncStateRepository.GetSyncStateAsync(accountId, folder.Id);
            var syncToken = syncState?.LastSyncToken;
            
            // Fetch emails from provider (incremental if token exists)
            var emails = await FetchEmailsFromProviderAsync(
                accountId, 
                folder.Id, 
                syncToken,
                options.BatchSize,
                cancellationToken);
            
            result.TotalEmails = emails.Count;
            
            foreach (var email in emails)
            {
                try
                {
                    // Check for duplicates
                    if (await IsDuplicateEmailAsync(email.MessageId))
                    {
                        result.SkippedEmails++;
                        continue;
                    }
                    
                    // Store email
                    await StoreEmailAsync(email, accountId, folder.Id, cancellationToken);
                    
                    // Store attachments
                    if (email.Attachments?.Any() == true)
                    {
                        await StoreAttachmentsAsync(email, accountId, cancellationToken);
                    }
                    
                    result.SyncedEmails++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to sync email {MessageId}", email.MessageId);
                    result.FailedEmails++;
                    
                    await LogSyncErrorAsync(accountId, email.MessageId, ex.Message);
                }
            }
            
            // Update sync state
            await UpdateSyncStateAsync(accountId, folder.Id, result);
            
            result.EndTime = DateTime.UtcNow;
            result.Status = SyncStatus.Completed;
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Folder sync failed for {FolderName}", folder.Name);
            result.Status = SyncStatus.Failed;
            result.Error = ex.Message;
            return result;
        }
    }

    private async Task StoreEmailAsync(
        EmailMessage email, 
        string accountId, 
        string folderId,
        CancellationToken cancellationToken)
    {
        var entity = new Email
        {
            MessageId = email.MessageId,
            AccountId = accountId,
            FolderId = folderId,
            Subject = email.Subject,
            From = email.From,
            To = string.Join(";", email.To),
            Cc = email.Cc != null ? string.Join(";", email.Cc) : null,
            Bcc = email.Bcc != null ? string.Join(";", email.Bcc) : null,
            Date = email.Date,
            Body = email.Body,
            BodyPreview = email.Body?.Substring(0, Math.Min(200, email.Body.Length)),
            IsRead = email.IsRead,
            IsFlagged = email.IsFlagged,
            HasAttachments = email.Attachments?.Any() == true,
            ConversationId = email.ConversationId,
            InReplyTo = email.InReplyTo,
            References = email.References,
            ReceivedAt = DateTime.UtcNow
        };
        
        await _emailRepository.AddAsync(entity, cancellationToken);
    }

    private async Task StoreAttachmentsAsync(
        EmailMessage email,
        string accountId,
        CancellationToken cancellationToken)
    {
        foreach (var attachment in email.Attachments)
        {
            try
            {
                // Download attachment data
                var attachmentData = await DownloadAttachmentDataAsync(
                    accountId, 
                    email.MessageId, 
                    attachment.Id,
                    cancellationToken);
                
                // Calculate hash for deduplication
                var hash = ComputeSha256Hash(attachmentData);
                
                // Check if attachment already exists
                var existingPath = await _attachmentStorage.FindByHashAsync(hash);
                
                string storagePath;
                if (existingPath != null)
                {
                    // Reuse existing attachment (deduplication)
                    storagePath = existingPath;
                    _logger.LogInformation(
                        "Attachment {Name} already exists, reusing", 
                        attachment.Name);
                }
                else
                {
                    // Store new attachment
                    storagePath = await _attachmentStorage.StoreAsync(
                        attachmentData,
                        attachment.Name,
                        attachment.ContentType,
                        hash,
                        cancellationToken);
                }
                
                // Store attachment metadata
                var attachmentEntity = new EmailAttachment
                {
                    MessageId = email.MessageId,
                    Name = attachment.Name,
                    ContentType = attachment.ContentType,
                    Size = attachment.Size,
                    StoragePath = storagePath,
                    Hash = hash,
                    UploadedAt = DateTime.UtcNow
                };
                
                await _emailRepository.AddAttachmentAsync(attachmentEntity, cancellationToken);
                
                _logger.LogInformation(
                    "Stored attachment {Name} ({Size} bytes)", 
                    attachment.Name, 
                    attachment.Size);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex, 
                    "Failed to store attachment {Name} for email {MessageId}", 
                    attachment.Name, 
                    email.MessageId);
                // Continue with other attachments
            }
        }
    }

    private async Task<bool> IsDuplicateEmailAsync(string messageId)
    {
        return await _emailRepository.ExistsByMessageIdAsync(messageId);
    }

    private string ComputeSha256Hash(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(data);
        return Convert.ToBase64String(hashBytes);
    }

    private async Task UpdateSyncStateAsync(
        string accountId, 
        string folderId, 
        FolderSyncResult result)
    {
        var syncState = new EmailSyncState
        {
            AccountId = accountId,
            FolderId = folderId,
            LastSyncDate = DateTime.UtcNow,
            LastSyncToken = result.NewSyncToken,
            TotalEmails = result.TotalEmails,
            SyncedEmails = result.SyncedEmails,
            FailedEmails = result.FailedEmails,
            Status = result.Status.ToString()
        };
        
        await _syncStateRepository.UpsertAsync(syncState);
    }

    private async Task LogSyncErrorAsync(string accountId, string messageId, string error)
    {
        var syncError = new SyncError
        {
            AccountId = accountId,
            EmailId = messageId,
            ErrorMessage = error,
            ErrorDate = DateTime.UtcNow,
            Retries = 0
        };
        
        await _syncStateRepository.AddErrorAsync(syncError);
    }
}
```

#### 2. Attachment Storage Service

```csharp
public class AttachmentStorageService : IAttachmentStorageService
{
    private readonly string _storageBasePath;
    private readonly ILogger<AttachmentStorageService> _logger;
    private readonly IDbContextFactory<MICDbContext> _contextFactory;

    public AttachmentStorageService(
        IConfiguration configuration,
        ILogger<AttachmentStorageService> logger,
        IDbContextFactory<MICDbContext> contextFactory)
    {
        _storageBasePath = configuration["AttachmentStorage:BasePath"] 
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MBARIE", "Attachments");
        
        _logger = logger;
        _contextFactory = contextFactory;
        
        EnsureStorageDirectoryExists();
    }

    public async Task<string> StoreAsync(
        byte[] data,
        string fileName,
        string contentType,
        string hash,
        CancellationToken cancellationToken)
    {
        // Create folder structure: BasePath/[First2CharsOfHash]/[Next2Chars]/[Hash]
        var folder1 = hash.Substring(0, 2);
        var folder2 = hash.Substring(2, 2);
        var directory = Path.Combine(_storageBasePath, folder1, folder2);
        
        Directory.CreateDirectory(directory);
        
        // Use hash as filename with original extension
        var extension = Path.GetExtension(fileName);
        var storagePath = Path.Combine(directory, $"{hash}{extension}");
        
        await File.WriteAllBytesAsync(storagePath, data, cancellationToken);
        
        _logger.LogInformation(
            "Stored attachment {FileName} at {Path} ({Size} bytes)", 
            fileName, 
            storagePath, 
            data.Length);
        
        return storagePath;
    }

    public async Task<byte[]> RetrieveAsync(string storagePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(storagePath))
        {
            throw new FileNotFoundException($"Attachment not found: {storagePath}");
        }
        
        return await File.ReadAllBytesAsync(storagePath, cancellationToken);
    }

    public async Task<string> FindByHashAsync(string hash)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var attachment = await context.EmailAttachments
            .FirstOrDefaultAsync(a => a.Hash == hash);
        
        return attachment?.StoragePath;
    }

    public async Task DeleteAsync(string storagePath)
    {
        if (File.Exists(storagePath))
        {
            File.Delete(storagePath);
            _logger.LogInformation("Deleted attachment at {Path}", storagePath);
        }
    }

    public async Task<long> GetTotalStorageSizeAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var totalSize = await context.EmailAttachments
            .SumAsync(a => a.Size);
        
        return totalSize;
    }

    private void EnsureStorageDirectoryExists()
    {
        if (!Directory.Exists(_storageBasePath))
        {
            Directory.CreateDirectory(_storageBasePath);
            _logger.LogInformation("Created attachment storage directory: {Path}", _storageBasePath);
        }
    }
}
```

#### 3. Database Schema

**Migrations:**
```csharp
public partial class AddEmailSyncTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Email sync state table
        migrationBuilder.CreateTable(
            name: "EmailSyncState",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                AccountId = table.Column<string>(maxLength: 255, nullable: false),
                FolderId = table.Column<string>(maxLength: 255, nullable: false),
                LastSyncToken = table.Column<string>(nullable: true),
                LastSyncDate = table.Column<DateTime>(nullable: false),
                TotalEmails = table.Column<int>(nullable: false),
                SyncedEmails = table.Column<int>(nullable: false),
                FailedEmails = table.Column<int>(nullable: false),
                Status = table.Column<string>(maxLength: 50, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EmailSyncState", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_EmailSyncState_AccountId_FolderId",
            table: "EmailSyncState",
            columns: new[] { "AccountId", "FolderId" },
            unique: true);

        // Sync errors table
        migrationBuilder.CreateTable(
            name: "SyncErrors",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                AccountId = table.Column<string>(maxLength: 255, nullable: false),
                EmailId = table.Column<string>(maxLength: 255, nullable: true),
                ErrorMessage = table.Column<string>(nullable: false),
                ErrorDate = table.Column<DateTime>(nullable: false),
                Retries = table.Column<int>(nullable: false, defaultValue: 0)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SyncErrors", x => x.Id);
            });

        // Email attachments table
        migrationBuilder.CreateTable(
            name: "EmailAttachments",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                MessageId = table.Column<string>(maxLength: 255, nullable: false),
                Name = table.Column<string>(maxLength: 500, nullable: false),
                ContentType = table.Column<string>(maxLength: 100, nullable: false),
                Size = table.Column<long>(nullable: false),
                StoragePath = table.Column<string>(maxLength: 1000, nullable: false),
                Hash = table.Column<string>(maxLength: 100, nullable: false),
                UploadedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EmailAttachments", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_EmailAttachments_MessageId",
            table: "EmailAttachments",
            column: "MessageId");

        migrationBuilder.CreateIndex(
            name: "IX_EmailAttachments_Hash",
            table: "EmailAttachments",
            column: "Hash");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "EmailSyncState");
        migrationBuilder.DropTable(name: "SyncErrors");
        migrationBuilder.DropTable(name: "EmailAttachments");
    }
}
```

### Acceptance Criteria Checklist

- [ ] Historical emails sync completely with all metadata
- [ ] Attachments download and store locally
- [ ] Attachment deduplication works (same file not stored twice)
- [ ] Sync state persists and enables resume
- [ ] Interrupted syncs can resume from checkpoint
- [ ] Duplicate emails not stored
- [ ] Network errors handled with retry logic
- [ ] Progress reporting works correctly
- [ ] Unit tests achieve 80%+ coverage
- [ ] Integration tests with real email providers pass

---

## 📦 WP-0.4: TESTING INFRASTRUCTURE SETUP

### Overview
Establish comprehensive testing infrastructure with proper test organization, real test data generation (no mocks), and CI/CD integration.

### Test Project Structure

```
MIC.Tests.Unit/
├── Application/
│   ├── Commands/
│   │   ├── Email/
│   │   └── Settings/
│   ├── Queries/
│   │   ├── Email/
│   │   └── Settings/
│   └── Handlers/
├── Domain/
│   ├── Entities/
│   └── ValueObjects/
├── Infrastructure/
│   ├── Services/
│   │   ├── EmailSyncServiceTests.cs
│   │   ├── OutlookOAuthServiceTests.cs
│   │   ├── GmailOAuthServiceTests.cs
│   │   ├── SettingsServiceTests.cs
│   │   └── AttachmentStorageServiceTests.cs
│   └── Repositories/
└── Presentation/
    └── ViewModels/

MIC.Tests.Integration/
├── Email/
│   ├── OAuthFlowTests.cs
│   ├── EmailSyncTests.cs
│   └── AttachmentStorageTests.cs
├── Database/
│   ├── SettingsPersistenceTests.cs
│   └── EmailRepositoryTests.cs
└── Infrastructure/

MIC.Tests.E2E/
└── Scenarios/
    ├── EmailWorkflowTests.cs
    └── SettingsWorkflowTests.cs
```

### Test Configuration

**Test project files:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xUnit" Version="2.6.4" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.6" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="NSubstitute" Version="5.1.0" />
    <PackageReference Include="Moq" Version="4.20.70" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.0" />
    <PackageReference Include="coverlet.collector" Version="6.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\MIC.Application\MIC.Application.csproj" />
    <ProjectReference Include="..\MIC.Infrastructure\MIC.Infrastructure.csproj" />
    <ProjectReference Include="..\MIC.Presentation\MIC.Presentation.csproj" />
  </ItemGroup>
</Project>
```

### Test Data Builders (NO MOCK DATA)

**EmailTestDataBuilder.cs:**
```csharp
public class EmailTestDataBuilder
{
    private string _messageId = $"<{Guid.NewGuid()}@example.com>";
    private string _subject = "Test Email Subject";
    private string _from = "sender@example.com";
    private List<string> _to = new() { "recipient@example.com" };
    private DateTime _date = DateTime.UtcNow;
    private string _body = "This is a test email body with real content.";
    private bool _isRead = false;
    private bool _isFlagged = false;
    private List<AttachmentTestData> _attachments = new();

    public EmailTestDataBuilder WithMessageId(string messageId)
    {
        _messageId = messageId;
        return this;
    }

    public EmailTestDataBuilder WithSubject(string subject)
    {
        _subject = subject;
        return this;
    }

    public EmailTestDataBuilder WithFrom(string from)
    {
        _from = from;
        return this;
    }

    public EmailTestDataBuilder WithRecipient(string recipient)
    {
        _to.Add(recipient);
        return this;
    }

    public EmailTestDataBuilder WithAttachment(string name, byte[] data, string contentType)
    {
        _attachments.Add(new AttachmentTestData
        {
            Name = name,
            Data = data,
            ContentType = contentType
        });
        return this;
    }

    public EmailTestDataBuilder AsRead()
    {
        _isRead = true;
        return this;
    }

    public EmailTestDataBuilder AsFlagged()
    {
        _isFlagged = true;
        return this;
    }

    public EmailMessage Build()
    {
        return new EmailMessage
        {
            MessageId = _messageId,
            Subject = _subject,
            From = _from,
            To = _to,
            Date = _date,
            Body = _body,
            IsRead = _isRead,
            IsFlagged = _isFlagged,
            Attachments = _attachments.Select(a => new EmailAttachment
            {
                Name = a.Name,
                Size = a.Data.Length,
                ContentType = a.ContentType
            }).ToList()
        };
    }

    public static EmailTestDataBuilder Create() => new();
    
    public static EmailMessage CreateDefault()
    {
        return new EmailTestDataBuilder().Build();
    }
}
```

### Unit Test Examples

**SettingsServiceTests.cs (Complete):**
```csharp
public class SettingsServiceTests : IDisposable
{
    private readonly IDbContextFactory<MICDbContext> _contextFactory;
    private readonly SettingsService _sut;
    private readonly string _testUserId = "test-user-123";

    public SettingsServiceTests()
    {
        // Setup in-memory database for testing
        var options = new DbContextOptionsBuilder<MICDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
            
        var context = new MICDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();

        var mockFactory = Substitute.For<IDbContextFactory<MICDbContext>>();
        mockFactory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(context));
        
        _contextFactory = mockFactory;
        
        var mockLogger = Substitute.For<ILogger<SettingsService>>();
        var mockCloudSync = Substitute.For<ICloudSyncService>();
        
        _sut = new SettingsService(_contextFactory, mockLogger, mockCloudSync, _testUserId);
    }

    [Fact]
    public async Task GetSettingAsync_NonExistentSetting_ReturnsDefaultValue()
    {
        // Arrange
        var defaultValue = "default";
        
        // Act
        var result = await _sut.GetSettingAsync("Category", "Key", defaultValue);
        
        // Assert
        result.Should().Be(defaultValue);
    }

    [Fact]
    public async Task SetSettingAsync_NewSetting_CreatesSettingInDatabase()
    {
        // Arrange
        var category = "TestCategory";
        var key = "TestKey";
        var value = "TestValue";
        
        // Act
        await _sut.SetSettingAsync(category, key, value);
        
        // Assert
        var retrieved = await _sut.GetSettingAsync<string>(category, key);
        retrieved.Should().Be(value);
    }

    [Fact]
    public async Task SetSettingAsync_ExistingSetting_UpdatesAndCreatesHistory()
    {
        // Arrange
        var category = "TestCategory";
        var key = "TestKey";
        var originalValue = "Original";
        var newValue = "Updated";
        
        await _sut.SetSettingAsync(category, key, originalValue);
        
        // Act
        await _sut.SetSettingAsync(category, key, newValue);
        
        // Assert
        var retrieved = await _sut.GetSettingAsync<string>(category, key);
        retrieved.Should().Be(newValue);
        
        // Verify history was created
        using var context = await _contextFactory.CreateDbContextAsync();
        var history = await context.SettingHistory.ToListAsync();
        history.Should().HaveCount(1);
        history[0].OldValue.Should().Contain("Original");
        history[0].NewValue.Should().Contain("Updated");
    }

    [Fact]
    public async Task SetSettingAsync_RaisesSettingChangedEvent()
    {
        // Arrange
        var category = "TestCategory";
        var key = "TestKey";
        var value = "TestValue";
        var eventRaised = false;
        
        _sut.SettingChanged += (s, e) =>
        {
            eventRaised = true;
            e.Category.Should().Be(category);
            e.Key.Should().Be(key);
        };
        
        // Act
        await _sut.SetSettingAsync(category, key, value);
        
        // Assert
        eventRaised.Should().BeTrue();
    }

    [Theory]
    [InlineData("StringValue", "String")]
    [InlineData(42, "Int")]
    [InlineData(true, "Bool")]
    [InlineData(3.14, "Double")]
    public async Task SetAndGetSetting_VariousTypes_WorksCorrectly<T>(T value, string expectedType)
    {
        // Arrange
        var category = "TestCategory";
        var key = $"Key{expectedType}";
        
        // Act
        await _sut.SetSettingAsync(category, key, value);
        var retrieved = await _sut.GetSettingAsync<T>(category, key);
        
        // Assert
        retrieved.Should().Be(value);
    }

    [Fact]
    public async Task SetSettingAsync_ComplexObject_SerializesAsJson()
    {
        // Arrange
        var category = "TestCategory";
        var key = "ComplexKey";
        var value = new TestComplexObject
        {
            Property1 = "Value1",
            Property2 = 123,
            NestedObject = new TestNestedObject { Data = "NestedData" }
        };
        
        // Act
        await _sut.SetSettingAsync(category, key, value);
        var retrieved = await _sut.GetSettingAsync<TestComplexObject>(category, key);
        
        // Assert
        retrieved.Should().NotBeNull();
        retrieved.Property1.Should().Be(value.Property1);
        retrieved.Property2.Should().Be(value.Property2);
        retrieved.NestedObject.Data.Should().Be(value.NestedObject.Data);
    }

    [Fact]
    public async Task ExportSettingsAsync_WithMultipleSettings_ReturnsValidJson()
    {
        // Arrange
        await _sut.SetSettingAsync("Cat1", "Key1", "Value1");
        await _sut.SetSettingAsync("Cat1", "Key2", "Value2");
        await _sut.SetSettingAsync("Cat2", "Key3", 123);
        
        // Act
        var json = await _sut.ExportSettingsAsync();
        
        // Assert
        json.Should().NotBeNullOrEmpty();
        
        var deserialized = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(json);
        deserialized.Should().ContainKey("Cat1");
        deserialized.Should().ContainKey("Cat2");
        deserialized["Cat1"].Should().ContainKey("Key1");
        deserialized["Cat1"].Should().ContainKey("Key2");
        deserialized["Cat2"].Should().ContainKey("Key3");
    }

    [Fact]
    public async Task ImportSettingsAsync_ValidJson_ImportsAllSettings()
    {
        // Arrange
        var json = @"{
            ""Category1"": {
                ""Key1"": ""Value1"",
                ""Key2"": ""Value2""
            },
            ""Category2"": {
                ""Key3"": 123
            }
        }";
        
        // Act
        await _sut.ImportSettingsAsync(json);
        
        // Assert
        var value1 = await _sut.GetSettingAsync<string>("Category1", "Key1");
        var value2 = await _sut.GetSettingAsync<string>("Category1", "Key2");
        var value3 = await _sut.GetSettingAsync<int>("Category2", "Key3");
        
        value1.Should().Be("Value1");
        value2.Should().Be("Value2");
        value3.Should().Be(123);
    }

    [Fact]
    public async Task DeleteSettingAsync_ExistingSetting_RemovesSetting()
    {
        // Arrange
        var category = "TestCategory";
        var key = "TestKey";
        await _sut.SetSettingAsync(category, key, "Value");
        
        // Act
        var result = await _sut.DeleteSettingAsync(category, key);
        
        // Assert
        result.Should().BeTrue();
        
        var retrieved = await _sut.GetSettingAsync<string>(category, key, "default");
        retrieved.Should().Be("default");
    }

    [Fact]
    public async Task GetCategorySettingsAsync_MultipleSettings_ReturnsAllInCategory()
    {
        // Arrange
        await _sut.SetSettingAsync("Cat1", "Key1", "Value1");
        await _sut.SetSettingAsync("Cat1", "Key2", "Value2");
        await _sut.SetSettingAsync("Cat2", "Key3", "Value3");
        
        // Act
        var settings = await _sut.GetCategorySettingsAsync("Cat1");
        
        // Assert
        settings.Should().HaveCount(2);
        settings.Should().ContainKey("Key1");
        settings.Should().ContainKey("Key2");
        settings.Should().NotContainKey("Key3");
    }

    public void Dispose()
    {
        // Cleanup
    }
}

public class TestComplexObject
{
    public string Property1 { get; set; }
    public int Property2 { get; set; }
    public TestNestedObject NestedObject { get; set; }
}

public class TestNestedObject
{
    public string Data { get; set; }
}
```

### Integration Test Example

**EmailSyncIntegrationTests.cs:**
```csharp
public class EmailSyncIntegrationTests : IDisposable
{
    private readonly TestEmailServer _emailServer;
    private readonly EmailSyncService _sut;
    private readonly string _testAccountId = "test-account";

    public EmailSyncIntegrationTests()
    {
        // Setup test email server (MailHog or similar)
        _emailServer = new TestEmailServer();
        _emailServer.Start();
        
        // Setup service with test dependencies
        var dbContext = CreateTestDatabase();
        var emailRepository = new EmailRepository(dbContext);
        var attachmentStorage = new AttachmentStorageService(/* test config */);
        var syncStateRepository = new SyncStateRepository(dbContext);
        var logger = Substitute.For<ILogger<EmailSyncService>>();
        
        _sut = new EmailSyncService(
            emailRepository,
            attachmentStorage,
            syncStateRepository,
            logger);
    }

    [Fact]
    public async Task SyncAccountAsync_WithRealEmails_SyncsSuccessfully()
    {
        // Arrange
        await _emailServer.SendTestEmailAsync(new EmailMessage
        {
            MessageId = "<test1@example.com>",
            From = "sender@example.com",
            To = new List<string> { "recipient@example.com" },
            Subject = "Integration Test Email",
            Body = "This is a real integration test email.",
            Date = DateTime.UtcNow
        });
        
        var options = new SyncOptions { BatchSize = 50 };
        var progress = new Progress<SyncProgress>();
        
        // Act
        var result = await _sut.SyncAccountAsync(_testAccountId, options, progress);
        
        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(SyncStatus.Completed);
        result.TotalEmailsSynced.Should().BeGreaterThan(0);
        result.FolderResults.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task SyncAccountAsync_WithAttachments_StoresAttachmentsCorrectly()
    {
        // Arrange
        var testImageData = GenerateTestImageData();
        
        await _emailServer.SendTestEmailAsync(new EmailMessage
        {
            MessageId = "<test-with-attachment@example.com>",
            From = "sender@example.com",
            To = new List<string> { "recipient@example.com" },
            Subject = "Email with Attachment",
            Body = "This email has an attachment.",
            Attachments = new List<AttachmentData>
            {
                new AttachmentData
                {
                    Name = "test-image.png",
                    Data = testImageData,
                    ContentType = "image/png"
                }
            }
        });
        
        var options = new SyncOptions { BatchSize = 50 };
        
        // Act
        var result = await _sut.SyncAccountAsync(_testAccountId, options);
        
        // Assert
        result.Status.Should().Be(SyncStatus.Completed);
        
        // Verify attachment was stored
        var emails = await GetStoredEmailsAsync();
        emails.Should().HaveCount(1);
        emails[0].HasAttachments.Should().BeTrue();
        
        var attachments = await GetEmailAttachmentsAsync(emails[0].MessageId);
        attachments.Should().HaveCount(1);
        attachments[0].Name.Should().Be("test-image.png");
        attachments[0].ContentType.Should().Be("image/png");
        attachments[0].Size.Should().Be(testImageData.Length);
    }

    [Fact]
    public async Task SyncAccountAsync_InterruptedAndResumed_ContinuesFromCheckpoint()
    {
        // Arrange - send multiple emails
        for (int i = 0; i < 10; i++)
        {
            await _emailServer.SendTestEmailAsync(
                EmailTestDataBuilder.Create()
                    .WithMessageId($"<test{i}@example.com>")
                    .WithSubject($"Test Email {i}")
                    .Build());
        }
        
        var options = new SyncOptions { BatchSize = 5 };
        var cancellationTokenSource = new CancellationTokenSource();
        
        // Act 1 - Start sync and cancel midway
        var task = _sut.SyncAccountAsync(_testAccountId, options, null, cancellationTokenSource.Token);
        await Task.Delay(100); // Let it start
        cancellationTokenSource.Cancel();
        
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        
        // Act 2 - Resume sync
        var result = await _sut.SyncAccountAsync(_testAccountId, options);
        
        // Assert
        result.Status.Should().Be(SyncStatus.Completed);
        result.TotalEmailsSynced.Should().Be(10);
    }

    private byte[] GenerateTestImageData()
    {
        // Generate a simple 1x1 PNG image
        return new byte[] { /* PNG header and data */ };
    }

    public void Dispose()
    {
        _emailServer?.Stop();
        _emailServer?.Dispose();
    }
}
```

### CI/CD Integration

**.github/workflows/test.yml:**
```yaml
name: Test Suite

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main, develop ]

jobs:
  test:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore --configuration Release
    
    - name: Run Unit Tests
      run: dotnet test MIC.Tests.Unit --no-build --configuration Release --logger "trx;LogFileName=unit-test-results.trx" --collect:"XPlat Code Coverage"
    
    - name: Run Integration Tests
      run: dotnet test MIC.Tests.Integration --no-build --configuration Release --logger "trx;LogFileName=integration-test-results.trx" --collect:"XPlat Code Coverage"
    
    - name: Generate Coverage Report
      run: |
        dotnet tool install -g dotnet-reportgenerator-globaltool
        reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coveragereport -reporttypes:Html
    
    - name: Upload Coverage to Codecov
      uses: codecov/codecov-action@v3
      with:
        files: '**/coverage.cobertura.xml'
        fail_ci_if_error: true
    
    - name: Check Coverage Threshold
      run: |
        # Script to ensure coverage is above 80%
        dotnet tool install -g dotnet-coverage
        dotnet-coverage check-threshold --threshold 80 --file coverage.cobertura.xml
```

### Acceptance Criteria Checklist

- [ ] All test projects compile and run
- [ ] Unit test coverage ≥ 80% for Phase 0 code
- [ ] Integration tests use real data (no mocks)
- [ ] Test data builders generate realistic data
- [ ] CI pipeline runs all tests automatically
- [ ] Coverage reports generated and uploaded
- [ ] Build fails if coverage drops below threshold
- [ ] All tests are deterministic (no flaky tests)
- [ ] Test execution time < 5 minutes for full suite

---

## 📦 WP-0.5 & WP-0.6: NOTIFICATION CENTER & ERROR HANDLING

*(Due to length constraints, I'll create separate detailed specification documents for these)*

---

## ⏱️ PHASE 0 EXECUTION SCHEDULE

### Week 1: Core Services

**Day 1 (Monday):**
- [ ] Morning: Setup development environment, review architecture
- [ ] Afternoon: Start WP-0.1 - Implement OutlookOAuthService
- [ ] End of day: OutlookOAuthService basic authentication working

**Day 2 (Tuesday):**
- [ ] Morning: Complete OutlookOAuthService with token refresh
- [ ] Afternoon: Implement GmailOAuthService
- [ ] End of day: Both OAuth services complete with unit tests

**Day 3 (Wednesday):**
- [ ] Morning: Implement TokenStorageService with encryption
- [ ] Afternoon: Start WP-0.2 - Settings Service implementation
- [ ] End of day: Basic settings CRUD operations working

**Day 4 (Thursday):**
- [ ] Morning: Complete SettingsService with all features
- [ ] Afternoon: Implement settings history and sync
- [ ] End of day: Settings service fully functional with tests

**Day 5 (Friday):**
- [ ] Morning: Setup WP-0.4 - Testing infrastructure
- [ ] Afternoon: Create test projects and CI pipeline
- [ ] End of day: Test infrastructure complete, coverage reporting working

### Week 2: Data & Quality

**Day 6 (Monday):**
- [ ] Morning: Start WP-0.3 - Email sync service completion
- [ ] Afternoon: Implement attachment storage service
- [ ] End of day: Attachments storing and retrieving correctly

**Day 7 (Tuesday):**
- [ ] Morning: Complete email sync with state management
- [ ] Afternoon: Implement resume/checkpoint functionality
- [ ] End of day: Email sync fully functional with tests

**Day 8 (Wednesday):**
- [ ] Morning: Start WP-0.5 - Notification center
- [ ] Afternoon: Complete notification service and UI
- [ ] End of day: Notifications working end-to-end

**Day 9 (Thursday):**
- [ ] Morning: Implement WP-0.6 - Error handling standardization
- [ ] Afternoon: Add logging and retry policies throughout
- [ ] End of day: Error handling consistent across all services

**Day 10 (Friday):**
- [ ] Morning: Final integration testing
- [ ] Afternoon: Bug fixes and polish
- [ ] End of day: All WPs complete, ready for validation

### Week 2 (Days 11-13): Validation

**Day 11-12:**
- [ ] Run full test suite
- [ ] Manual testing of all features
- [ ] Performance profiling
- [ ] Security review

**Day 13:**
- [ ] Final bug fixes
- [ ] Documentation updates
- [ ] Prepare Phase 0 completion report
- [ ] Get approval to proceed to Phase 1

---

## ✅ PHASE 0 COMPLETION CHECKLIST

### Code Quality
- [ ] Zero NotImplementedException in codebase
- [ ] All compiler warnings resolved
- [ ] Code follows C# conventions
- [ ] XML documentation on all public APIs
- [ ] No hardcoded credentials or secrets

### Functionality
- [ ] OAuth works for both Outlook and Gmail
- [ ] Tokens persist and refresh automatically
- [ ] Settings save and load correctly
- [ ] Settings sync with cloud (if enabled)
- [ ] Email sync completes with attachments
- [ ] Sync can resume after interruption
- [ ] Notifications display correctly
- [ ] Error handling consistent

### Testing
- [ ] Unit test coverage ≥ 80%
- [ ] Integration tests pass
- [ ] No flaky tests
- [ ] CI pipeline green
- [ ] Coverage reports generated

### Performance
- [ ] Application startup < 3 seconds
- [ ] Settings load < 10ms
- [ ] Email sync 1000 emails < 30 seconds
- [ ] Memory usage < 300MB idle

### Documentation
- [ ] README updated
- [ ] API documentation complete
- [ ] Architecture diagrams current
- [ ] Changelog updated

---

## 🚀 NEXT STEPS AFTER PHASE 0

Once all acceptance criteria are met and validation passes:

1. **Phase 0 Completion Report**
   - Summary of all completed work
   - Test coverage metrics
   - Performance benchmarks
   - Known issues and technical debt

2. **Phase 1 Planning**
   - Critical feature completion (email send, KB RAG, predictions)
   - Detailed technical specifications
   - Resource allocation
   - Timeline estimation

3. **Approval Gate**
   - Stakeholder review
   - Go/no-go decision for Phase 1
   - Priority adjustments if needed

---

## 📞 SUPPORT & ESCALATION

**For Questions:**
- Review this document first
- Check existing architecture documentation
- Review similar implementations in codebase

**For Blockers:**
- Document the blocker clearly
- Identify dependencies affected
- Propose potential solutions
- Escalate immediately

**For Changes:**
- No changes to scope without approval
- Document all deviations
- Update acceptance criteria if needed
- Communicate impact to timeline

---

**END OF PHASE 0 IMPLEMENTATION MASTER DOCUMENT**

*This document should be treated as the single source of truth for Phase 0 implementation. All work should reference and follow the specifications provided here.*