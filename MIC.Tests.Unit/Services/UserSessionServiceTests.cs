using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using MIC.Core.Application.Authentication.Common;
using MIC.Desktop.Avalonia.Services;
using Xunit;

using DesktopUserRole = MIC.Desktop.Avalonia.Services.UserRole;
using DomainUserRole = MIC.Core.Domain.Entities.UserRole;

namespace MIC.Tests.Unit.Services;

[CollectionDefinition("UserSessionServiceTests", DisableParallelization = true)]
public sealed class UserSessionServiceTestsCollectionDefinition
{
}

[Collection("UserSessionServiceTests")]
public sealed class UserSessionServiceTests
{
    private static string SessionFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MIC",
        "session.json");

    [Fact]
    public void SetSession_WithBlankDisplayName_RaisesEventAndFormatsName()
    {
        using var scope = new SessionStorageScope(clearSession: true);
        var service = new UserSessionService();

        UserSession? captured = null;
        var raised = false;
        service.OnSessionChanged += session =>
        {
            raised = true;
            captured = session;
        };

        service.SetSession("42", "jane.doe", "jane@example.com", string.Empty, "token-123", "Manager", "Operations");

        Assert.True(raised);
        Assert.NotNull(captured);
        Assert.Equal("Jane Doe", captured!.DisplayName);
        Assert.True(service.IsAuthenticated);
        Assert.Equal("Jane Doe", service.CurrentUserName);
        Assert.Equal(DesktopUserRole.Guest, service.CurrentRole);
    }

    [Fact]
    public void SetUser_WithNoExistingSession_CreatesSessionFromDto()
    {
        using var scope = new SessionStorageScope(clearSession: true);
        var service = new UserSessionService();

        var dto = new UserDto
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            Email = "admin@example.com",
            FullName = null,
            Role = DomainUserRole.Admin,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            JobPosition = "Lead",
            Department = "IT",
            SeniorityLevel = "Senior"
        };

        service.SetUser(dto);

        Assert.NotNull(service.CurrentSession);
        Assert.True(service.IsLoggedIn);
        Assert.Equal(DesktopUserRole.Admin, service.CurrentRole);

        var currentUser = service.CurrentUser;
        Assert.Equal(dto.Id, currentUser.Id);
        Assert.Equal(dto.Username, currentUser.Username);
        Assert.Equal(dto.Email, currentUser.Email);
        Assert.Equal("Admin", service.CurrentUserName);
    }

    [Fact]
    public void Constructor_WithPersistedSession_LoadsSessionAndPreferences()
    {
        using var scope = new SessionStorageScope(clearSession: true);

        var persistedSession = new UserSession
        {
            UserId = Guid.NewGuid().ToString("D"),
            Username = "persisted.user",
            DisplayName = "Persisted User",
            Email = "persisted@example.com",
            Role = DesktopUserRole.User,
            LoginTime = DateTime.Now,
            LastActivity = DateTime.Now,
            Preferences = new UserPreferences
            {
                Settings = new Dictionary<string, object>
                {
                    ["refreshInterval"] = 25,
                    ["theme"] = "light"
                }
            }
        };

        var json = JsonSerializer.Serialize(persistedSession);
        File.WriteAllText(SessionFilePath, json);

        var service = new UserSessionService();

        Assert.True(service.IsLoggedIn);
        Assert.Equal("Persisted User", service.CurrentUserName);
        Assert.Equal(DesktopUserRole.User, service.CurrentRole);
        Assert.Equal("persisted.user", service.CurrentSession?.Username);
        Assert.Equal(25, service.GetPreference("refreshInterval", 5));
        Assert.Equal("light", service.GetPreference("theme", "dark"));
    }

    [Fact]
    public void Constructor_WithExpiredSession_RemovesStoredSession()
    {
        using var scope = new SessionStorageScope(clearSession: true);

        var expiredSession = new UserSession
        {
            UserId = Guid.NewGuid().ToString("D"),
            Username = "stale.user",
            DisplayName = "Stale User",
            Email = "stale@example.com",
            Role = DesktopUserRole.User,
            LoginTime = DateTime.Now.AddDays(-31),
            LastActivity = DateTime.Now.AddDays(-31),
            Preferences = new UserPreferences
            {
                Settings = new Dictionary<string, object>
                {
                    ["refreshInterval"] = 10
                }
            }
        };

        var json = JsonSerializer.Serialize(expiredSession);
        File.WriteAllText(SessionFilePath, json);

        var service = new UserSessionService();

        Assert.False(service.IsLoggedIn);
        Assert.Null(service.CurrentSession);
        Assert.False(File.Exists(SessionFilePath));
    }

    [Fact]
    public async Task SetPreferenceAsync_PersistsValueAndCreatesStorage()
    {
        using var scope = new SessionStorageScope(clearSession: true);
        var service = new UserSessionService();
        service.SetSession("7", "alice", "alice@example.com", "Alice", "token", "Analyst", "Insights");

        await service.SetPreferenceAsync("refreshInterval", 15);

        var retrieved = service.GetPreference("refreshInterval", 5);
        Assert.Equal(15, retrieved);
        Assert.True(File.Exists(SessionFilePath));
    }

    [Fact]
    public async Task Clear_RemovesSessionTokenAndStoredFile()
    {
        using var scope = new SessionStorageScope(clearSession: true);
        var service = new UserSessionService();
        service.SetSession("99", "tester", "tester@example.com", "Tester", "token", "QA", "Engineering");

        await service.SetPreferenceAsync("theme", "dark");
        Assert.True(File.Exists(SessionFilePath));

        var logoutRaised = false;
        service.OnLogout += () => logoutRaised = true;

        service.Clear();

        Assert.False(service.IsLoggedIn);
        Assert.Equal(string.Empty, service.GetToken());
        Assert.True(logoutRaised);
        Assert.False(File.Exists(SessionFilePath));
    }

    [Fact]
    public void HasPermission_RespectsRoleAssignments()
    {
        using var scope = new SessionStorageScope(clearSession: true);
        var service = new UserSessionService();

        service.SetUser(new UserDto
        {
            Id = Guid.NewGuid(),
            Username = "poweruser",
            Email = "user@example.com",
            FullName = "Power User",
            Role = DomainUserRole.User,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        Assert.True(service.HasPermission(Permission.ViewMetrics));
        Assert.False(service.HasPermission(Permission.SystemSettings));

        service.SetUser(new UserDto
        {
            Id = Guid.NewGuid(),
            Username = "root",
            Email = "root@example.com",
            FullName = "Root",
            Role = DomainUserRole.Admin,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        Assert.True(service.HasPermission(Permission.SystemSettings));

        var guestService = new UserSessionService();
        guestService.SetSession("guest", "guest", "guest@example.com", "Guest", token: null);

        Assert.False(guestService.HasPermission(Permission.UseAI));
        Assert.True(guestService.HasPermission(Permission.ViewAlerts));
    }

    [Fact]
    public void GetToken_WithoutSettingToken_ReturnsEmptyString()
    {
        using var scope = new SessionStorageScope(clearSession: true);
        var service = new UserSessionService();

        Assert.Equal(string.Empty, service.GetToken());
    }

    [Fact]
    public void SetToken_PersistsValueUntilCleared()
    {
        using var scope = new SessionStorageScope(clearSession: true);
        var service = new UserSessionService();

        service.SetToken("session-token");
        Assert.Equal("session-token", service.GetToken());

        service.Clear();
        Assert.Equal(string.Empty, service.GetToken());
    }

    [Fact]
    public void GetUser_WhenNoSession_ReturnsEmptyDto()
    {
        using var scope = new SessionStorageScope(clearSession: true);
        var service = new UserSessionService();

        var user = service.GetUser();

        Assert.Equal(Guid.Empty, user.Id);
        Assert.Equal(string.Empty, user.Username);
        Assert.Equal(string.Empty, user.Email);
    }

    [Fact]
    public void SetUser_WhenSessionAlreadyExists_UpdatesSessionProperties()
    {
        using var scope = new SessionStorageScope(clearSession: true);
        var service = new UserSessionService();
        service.SetSession("1", "initial.user", "initial@example.com", "Initial User", null, "Initial", "Ops");

        var updated = new UserDto
        {
            Id = Guid.NewGuid(),
            Username = "updated.user",
            Email = "updated@example.com",
            FullName = "Updated User",
            Role = DomainUserRole.User,
            JobPosition = "Analyst",
            Department = "Metrics",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            IsActive = true
        };

        service.SetUser(updated);

        var session = service.CurrentSession;
        Assert.NotNull(session);
        Assert.Equal(updated.Id.ToString(), session!.UserId);
        Assert.Equal("Updated User", session.DisplayName);
        Assert.Equal("updated@example.com", session.Email);
        Assert.Equal(DesktopUserRole.User, session.Role);
        Assert.Equal("Analyst", session.Position);
        Assert.Equal("Metrics", session.Department);
    }

    [Fact]
    public void GetUser_WhenSessionContainsInvalidGuid_UsesEmptyGuid()
    {
        using var scope = new SessionStorageScope(clearSession: true);
        var service = new UserSessionService();
        service.SetSession("not-a-guid", "someone", "test@example.com", "Test User", token: "token");

        var user = service.GetUser();

        Assert.Equal(Guid.Empty, user.Id);
        Assert.Equal("someone", user.Username);
    }

    [Fact]
    public async Task SetPreferenceAsync_WithNoActiveSession_DoesNothing()
    {
        using var scope = new SessionStorageScope(clearSession: true);
        var service = new UserSessionService();
        service.Clear();

        await service.SetPreferenceAsync("theme", "dark");

        Assert.False(File.Exists(SessionFilePath));
    }

    [Fact]
    public async Task GetPreference_WithJsonElementData_ReturnsTypedValue()
    {
        using var scope = new SessionStorageScope(clearSession: true);
        var initial = new UserSessionService();
        initial.SetSession("7", "json.user", "json@example.com", "Json User", token: "token");

        await initial.SetPreferenceAsync("threshold", 0.85);

        var reloaded = new UserSessionService();
        var value = reloaded.GetPreference("threshold", 0.1);

        Assert.Equal(0.85, value);
    }

    [Fact]
    public void GetPreference_WhenConversionFails_ReturnsDefault()
    {
        using var scope = new SessionStorageScope(clearSession: true);
        var service = new UserSessionService();
        service.SetSession("15", "pref.user", "pref@example.com", "Pref User", token: "token");

        service.CurrentSession!.Preferences!.Settings = new Dictionary<string, object>
        {
            ["refreshInterval"] = "invalid"
        };

        var result = service.GetPreference("refreshInterval", 5);

        Assert.Equal(5, result);
    }

    [Fact]
    public void UpdateActivity_WhenSessionExists_RefreshesTimestamp()
    {
        using var scope = new SessionStorageScope(clearSession: true);
        var service = new UserSessionService();
        service.SetSession("99", "activity.user", "activity@example.com", "Activity User", token: "token");

        var previous = DateTime.Now.AddMinutes(-10);
        service.CurrentSession!.LastActivity = previous;

        service.UpdateActivity();

        Assert.True(service.CurrentSession!.LastActivity > previous);
    }

    [Fact]
    public void CanPerform_WhenNotLoggedIn_ReturnsFalseWithoutUIInteraction()
    {
        using var scope = new SessionStorageScope(clearSession: true);
        var service = new UserSessionService();

        var result = service.CanPerform(Permission.ViewDashboard, showError: false);

        Assert.False(result);
    }

    [Fact]
    public void CanPerform_WhenPermissionDenied_ReturnsFalse()
    {
        using var scope = new SessionStorageScope(clearSession: true);
        var service = new UserSessionService();
        service.SetSession("guest", "guest", "guest@example.com", "Guest User", token: null);

        var result = service.CanPerform(Permission.SystemSettings, showError: false);

        Assert.False(result);
    }

    [Fact]
    public void CanPerform_WhenPermissionGranted_ReturnsTrue()
    {
        using var scope = new SessionStorageScope(clearSession: true);
        var service = new UserSessionService();
        service.SetUser(new UserDto
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            Email = "admin@example.com",
            FullName = "Administrator",
            Role = DomainUserRole.Admin,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            IsActive = true
        });

        var result = service.CanPerform(Permission.SystemSettings, showError: false);

        Assert.True(result);
    }

    private sealed class SessionStorageScope : IDisposable
    {
        private readonly string _sessionPath;
        private readonly string? _backupPath;
        private readonly bool _hadExisting;

        public SessionStorageScope(bool clearSession)
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MIC");
            Directory.CreateDirectory(directory);

            _sessionPath = Path.Combine(directory, "session.json");

            if (File.Exists(_sessionPath))
            {
                _backupPath = Path.Combine(Path.GetTempPath(), $"mic-session-backup-{Guid.NewGuid():N}.json");
                File.Copy(_sessionPath, _backupPath, overwrite: true);
                _hadExisting = true;

                if (clearSession)
                {
                    File.Delete(_sessionPath);
                }
            }
            else if (clearSession && File.Exists(_sessionPath))
            {
                File.Delete(_sessionPath);
            }
        }

        public void Dispose()
        {
            try
            {
                if (_hadExisting && _backupPath is not null)
                {
                    File.Copy(_backupPath, _sessionPath, overwrite: true);
                    File.Delete(_backupPath);
                }
                else
                {
                    if (File.Exists(_sessionPath))
                    {
                        File.Delete(_sessionPath);
                    }
                }
            }
            catch
            {
                // Ignore cleanup failures to avoid masking test results.
            }
        }
    }
}
