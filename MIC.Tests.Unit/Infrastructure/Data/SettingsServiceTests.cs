using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Authentication.Common;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.Data.Persistence;
using MIC.Infrastructure.Data.Services;
using NSubstitute;
using Xunit;

namespace MIC.Tests.Unit.Infrastructure.Data;

/// <summary>
/// Tests for <see cref="SettingsService"/> — the largest untested file in Infrastructure.Data (743 lines, 0% coverage).
/// Uses InMemory EF Core with a real IDbContextFactory wrapper + mocked ISessionService/ISettingsCloudSyncService.
/// </summary>
public class SettingsServiceTests : IDisposable
{
    private readonly string _dbName = $"SettingsSvc_{Guid.NewGuid()}";
    private readonly string _settingsFilePath;
    private readonly ILogger<SettingsService> _logger;
    private readonly ISessionService _session;
    private readonly ISettingsCloudSyncService _cloudSync;
    private readonly Guid _userId = Guid.NewGuid();

    public SettingsServiceTests()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "MIC_Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        _settingsFilePath = Path.Combine(tempDir, "settings.json");
        _logger = Substitute.For<ILogger<SettingsService>>();
        _session = Substitute.For<ISessionService>();
        _cloudSync = Substitute.For<ISettingsCloudSyncService>();

        // Set up authenticated session by default
        _session.IsAuthenticated.Returns(true);
        _session.GetUser().Returns(new UserDto
        {
            Id = _userId,
            Username = "testUser",
            Email = "test@example.com"
        });

        _cloudSync.GetCurrentStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new SettingsSyncStatus(0, 0, null));
    }

    public void Dispose()
    {
        var dir = Path.GetDirectoryName(_settingsFilePath);
        if (dir != null && Directory.Exists(dir))
        {
            try { Directory.Delete(dir, true); } catch { /* cleanup best effort */ }
        }
    }

    private MicDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MicDbContext>()
            .UseInMemoryDatabase(_dbName)
            .Options;
        return new MicDbContext(options);
    }

    private IDbContextFactory<MicDbContext> CreateFactory()
    {
        var factory = Substitute.For<IDbContextFactory<MicDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(CreateContext()));
        return factory;
    }

    private SettingsService CreateService(IDbContextFactory<MicDbContext>? factory = null)
    {
        return new SettingsService(
            factory ?? CreateFactory(),
            _logger,
            _cloudSync,
            _session,
            _settingsFilePath);
    }

    #region Constructor & GetSettings

    [Fact]
    public void Constructor_CreatesSettingsDirectory()
    {
        var svc = CreateService();
        Directory.Exists(Path.GetDirectoryName(_settingsFilePath)).Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithNullPath_UsesAppDataDefault()
    {
        // When settingsFilePath is null, it uses AppData/Roaming/MIC
        var svc = new SettingsService(CreateFactory(), _logger, _cloudSync, _session, null);
        svc.GetSettings().Should().NotBeNull();
    }

    [Fact]
    public void GetSettings_ReturnsDefaultAppSettings_WhenNoSnapshot()
    {
        var svc = CreateService();
        var settings = svc.GetSettings();
        settings.Should().NotBeNull();
        settings.AI.Should().NotBeNull();
        settings.EmailSync.Should().NotBeNull();
        settings.UI.Should().NotBeNull();
    }

    [Fact]
    public void GetSettings_LoadsFromDiskSnapshot_WhenExists()
    {
        // Write a snapshot first
        var json = """{"ai":{"provider":"TestProvider"}}""";
        File.WriteAllText(_settingsFilePath, json);

        var svc = CreateService();
        var settings = svc.GetSettings();
        settings.AI.Provider.Should().Be("TestProvider");
    }

    [Fact]
    public void GetSettings_ReturnsDefault_WhenDiskCorrupted()
    {
        File.WriteAllText(_settingsFilePath, "NOT VALID JSON{{{");
        var svc = CreateService();
        var settings = svc.GetSettings();
        settings.Should().NotBeNull();
        settings.AI.Provider.Should().Be("OpenAI"); // default
    }

    #endregion

    #region SaveSettingsAsync

    [Fact]
    public async Task SaveSettingsAsync_ThrowsOnNull()
    {
        var svc = CreateService();
        await Assert.ThrowsAsync<ArgumentNullException>(() => svc.SaveSettingsAsync(null!));
    }

    [Fact]
    public async Task SaveSettingsAsync_PersistsSnapshotToDisk()
    {
        var svc = CreateService();
        var settings = new AppSettings();
        settings.AI.Provider = "Azure";
        await svc.SaveSettingsAsync(settings);

        File.Exists(_settingsFilePath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(_settingsFilePath);
        content.Should().Contain("Azure");
    }

    [Fact]
    public async Task SaveSettingsAsync_UpdatesCache()
    {
        var svc = CreateService();
        var settings = new AppSettings();
        settings.UI.Theme = "Light";
        await svc.SaveSettingsAsync(settings);

        svc.GetSettings().UI.Theme.Should().Be("Light");
    }

    [Fact]
    public async Task SaveSettingsAsync_PersistsToDatabase_WhenUserAuthenticated()
    {
        var factory = CreateFactory();
        var svc = CreateService(factory);
        var settings = new AppSettings();
        settings.AI.Provider = "Anthropic";
        await svc.SaveSettingsAsync(settings);

        // Verify settings were persisted to DB
        using var ctx = CreateContext();
        var dbSettings = await ctx.Settings.Where(s => s.UserId == _userId).ToListAsync();
        dbSettings.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SaveSettingsAsync_SkipsDb_WhenNoUser()
    {
        _session.IsAuthenticated.Returns(false);
        var svc = CreateService();
        await svc.SaveSettingsAsync(new AppSettings());

        using var ctx = CreateContext();
        var count = await ctx.Settings.CountAsync();
        count.Should().Be(0);
    }

    #endregion

    #region SaveUserSettingsAsync

    [Fact]
    public async Task SaveUserSettingsAsync_ThrowsOnNull()
    {
        var svc = CreateService();
        await Assert.ThrowsAsync<ArgumentNullException>(() => svc.SaveUserSettingsAsync(_userId, null!));
    }

    [Fact]
    public async Task SaveUserSettingsAsync_SkipsForEmptyUserId()
    {
        var svc = CreateService();
        await svc.SaveUserSettingsAsync(Guid.Empty, new AppSettings());

        using var ctx = CreateContext();
        (await ctx.Settings.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task SaveUserSettingsAsync_PersistsForUser()
    {
        var svc = CreateService();
        var settings = new AppSettings();
        settings.General.AutoStart = true;
        await svc.SaveUserSettingsAsync(_userId, settings);

        using var ctx = CreateContext();
        var dbSettings = await ctx.Settings.Where(s => s.UserId == _userId).ToListAsync();
        dbSettings.Should().NotBeEmpty();
    }

    #endregion

    #region LoadUserSettingsAsync

    [Fact]
    public async Task LoadUserSettingsAsync_ReturnsCached_WhenEmptyUserId()
    {
        var svc = CreateService();
        var result = await svc.LoadUserSettingsAsync(Guid.Empty);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadUserSettingsAsync_ReturnsBuiltSettings()
    {
        var svc = CreateService();
        // First save
        var settings = new AppSettings();
        settings.AI.Provider = "Custom";
        await svc.SaveUserSettingsAsync(_userId, settings);

        // Then load
        var loaded = await svc.LoadUserSettingsAsync(_userId);
        loaded.Should().NotBeNull();
    }

    #endregion

    #region GetSettingAsync / SetSettingAsync

    [Fact]
    public async Task GetSettingAsync_ThrowsOnNullCategory()
    {
        var svc = CreateService();
        await Assert.ThrowsAnyAsync<ArgumentException>(() => svc.GetSettingAsync<string>(null!, "key"));
    }

    [Fact]
    public async Task GetSettingAsync_ThrowsOnNullKey()
    {
        var svc = CreateService();
        await Assert.ThrowsAnyAsync<ArgumentException>(() => svc.GetSettingAsync<string>("cat", null!));
    }

    [Fact]
    public async Task GetSettingAsync_ReturnsDefault_WhenNoUser()
    {
        _session.IsAuthenticated.Returns(false);
        var svc = CreateService();
        var result = await svc.GetSettingAsync("cat", "key", "fallback");
        result.Should().Be("fallback");
    }

    [Fact]
    public async Task GetSettingAsync_ReturnsDefault_WhenNotFound()
    {
        var svc = CreateService();
        var result = await svc.GetSettingAsync("cat", "nonexistent", 42);
        result.Should().Be(42);
    }

    [Fact]
    public async Task SetSettingAsync_ThrowsOnNullCategory()
    {
        var svc = CreateService();
        await Assert.ThrowsAnyAsync<ArgumentException>(() => svc.SetSettingAsync(null!, "key", "val"));
    }

    [Fact]
    public async Task SetSettingAsync_ThrowsOnNullKey()
    {
        var svc = CreateService();
        await Assert.ThrowsAnyAsync<ArgumentException>(() => svc.SetSettingAsync("cat", null!, "val"));
    }

    [Fact]
    public async Task SetSettingAsync_SkipsWhenNoUser()
    {
        _session.IsAuthenticated.Returns(false);
        var svc = CreateService();
        await svc.SetSettingAsync("cat", "key", "val");

        using var ctx = CreateContext();
        (await ctx.Settings.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task SetSettingAsync_CreatesNewSetting()
    {
        var svc = CreateService();
        await svc.SetSettingAsync("TestCat", "TestKey", "TestVal");

        using var ctx = CreateContext();
        var setting = await ctx.Settings.FirstOrDefaultAsync(s => s.Key == "TestKey");
        setting.Should().NotBeNull();
        setting!.Value.Should().Be("TestVal");
        setting.ValueType.Should().Be("String");
    }

    [Fact]
    public async Task SetSettingAsync_UpdatesExistingSetting()
    {
        var svc = CreateService();
        await svc.SetSettingAsync("TestCat", "Key1", "First");
        await svc.SetSettingAsync("TestCat", "Key1", "Second");

        using var ctx = CreateContext();
        var settings = await ctx.Settings.Where(s => s.Key == "Key1").ToListAsync();
        settings.Should().HaveCount(1);
        settings[0].Value.Should().Be("Second");
    }

    [Fact]
    public async Task SetSettingAsync_CreatesHistory_OnUpdate()
    {
        var svc = CreateService();
        await svc.SetSettingAsync("TestCat", "HKey", "v1");
        await svc.SetSettingAsync("TestCat", "HKey", "v2");

        using var ctx = CreateContext();
        var history = await ctx.SettingHistory.ToListAsync();
        history.Should().NotBeEmpty();
        history.Should().Contain(h => h.OldValue == "v1" && h.NewValue == "v2");
    }

    [Fact]
    public async Task SetSettingAsync_Skips_WhenValueUnchanged()
    {
        var svc = CreateService();
        await svc.SetSettingAsync("TestCat", "Same", "val");
        await svc.SetSettingAsync("TestCat", "Same", "val");

        using var ctx = CreateContext();
        var history = await ctx.SettingHistory.ToListAsync();
        history.Should().BeEmpty(); // no change = no history entry
    }

    [Fact]
    public async Task SetSettingAsync_SerializesBoolValue()
    {
        var svc = CreateService();
        await svc.SetSettingAsync("TestCat", "BoolKey", true);

        using var ctx = CreateContext();
        var setting = await ctx.Settings.FirstOrDefaultAsync(s => s.Key == "BoolKey");
        setting.Should().NotBeNull();
        setting!.ValueType.Should().Be("Bool");
        setting.Value.Should().Be("true");
    }

    [Fact]
    public async Task SetSettingAsync_SerializesIntValue()
    {
        var svc = CreateService();
        await svc.SetSettingAsync("TestCat", "IntKey", 42);

        using var ctx = CreateContext();
        var setting = await ctx.Settings.FirstOrDefaultAsync(s => s.Key == "IntKey");
        setting.Should().NotBeNull();
        setting!.ValueType.Should().Be("Int");
        setting.Value.Should().Be("42");
    }

    [Fact]
    public async Task SetSettingAsync_SerializesDoubleValue()
    {
        var svc = CreateService();
        await svc.SetSettingAsync("TestCat", "DoubleKey", 3.14);

        using var ctx = CreateContext();
        var setting = await ctx.Settings.FirstOrDefaultAsync(s => s.Key == "DoubleKey");
        setting.Should().NotBeNull();
        setting!.ValueType.Should().Be("Double");
    }

    [Fact]
    public async Task SetSettingAsync_RaisesSettingsChangedEvent()
    {
        var svc = CreateService();
        SettingsChangedEventArgs? eventArgs = null;
        svc.SettingsChanged += (_, args) => eventArgs = args;

        await svc.SetSettingAsync("TestCat", "EventKey", "val");

        eventArgs.Should().NotBeNull();
        eventArgs!.Category.Should().Be("TestCat");
        eventArgs.Key.Should().Be("EventKey");
    }

    #endregion

    #region DeleteSettingAsync

    [Fact]
    public async Task DeleteSettingAsync_ThrowsOnNullCategory()
    {
        var svc = CreateService();
        await Assert.ThrowsAnyAsync<ArgumentException>(() => svc.DeleteSettingAsync(null!, "key"));
    }

    [Fact]
    public async Task DeleteSettingAsync_ReturnsFalse_WhenNoUser()
    {
        _session.IsAuthenticated.Returns(false);
        var svc = CreateService();
        var result = await svc.DeleteSettingAsync("cat", "key");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteSettingAsync_ReturnsFalse_WhenNotFound()
    {
        var svc = CreateService();
        var result = await svc.DeleteSettingAsync("cat", "nonexistent");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteSettingAsync_RemovesSetting()
    {
        var svc = CreateService();
        await svc.SetSettingAsync("TestCat", "DelKey", "val");
        var deleted = await svc.DeleteSettingAsync("TestCat", "DelKey");
        deleted.Should().BeTrue();

        using var ctx = CreateContext();
        var setting = await ctx.Settings.FirstOrDefaultAsync(s => s.Key == "DelKey");
        setting.Should().BeNull();
    }

    [Fact]
    public async Task DeleteSettingAsync_RaisesSettingsChangedEvent()
    {
        var svc = CreateService();
        await svc.SetSettingAsync("TestCat", "DelEvt", "original");
        SettingsChangedEventArgs? eventArgs = null;
        svc.SettingsChanged += (_, args) => eventArgs = args;
        await svc.DeleteSettingAsync("TestCat", "DelEvt");
        eventArgs.Should().NotBeNull();
        eventArgs!.Category.Should().Be("TestCat");
        eventArgs.Key.Should().Be("DelEvt");
    }

    #endregion

    #region GetCategorySettingsAsync

    [Fact]
    public async Task GetCategorySettingsAsync_ThrowsOnNullCategory()
    {
        var svc = CreateService();
        await Assert.ThrowsAnyAsync<ArgumentException>(() => svc.GetCategorySettingsAsync(null!));
    }

    [Fact]
    public async Task GetCategorySettingsAsync_ReturnsEmpty_WhenNoUser()
    {
        _session.IsAuthenticated.Returns(false);
        var svc = CreateService();
        var result = await svc.GetCategorySettingsAsync("cat");
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCategorySettingsAsync_ReturnsSettingsForCategory()
    {
        var svc = CreateService();
        await svc.SetSettingAsync("MyCat", "k1", "v1");
        await svc.SetSettingAsync("MyCat", "k2", "v2");
        await svc.SetSettingAsync("OtherCat", "k3", "v3");

        var result = await svc.GetCategorySettingsAsync("MyCat");
        result.Should().HaveCount(2);
        result.Should().ContainKey("k1");
        result.Should().ContainKey("k2");
    }

    #endregion

    #region SetMultipleSettingsAsync

    [Fact]
    public async Task SetMultipleSettingsAsync_ThrowsOnNullCategory()
    {
        var svc = CreateService();
        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => svc.SetMultipleSettingsAsync(null!, new Dictionary<string, object>()));
    }

    [Fact]
    public async Task SetMultipleSettingsAsync_ThrowsOnNull()
    {
        var svc = CreateService();
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => svc.SetMultipleSettingsAsync("cat", null!));
    }

    [Fact]
    public async Task SetMultipleSettingsAsync_SkipsWhenNoUser()
    {
        _session.IsAuthenticated.Returns(false);
        var svc = CreateService();
        await svc.SetMultipleSettingsAsync("cat", new Dictionary<string, object> { ["k"] = "v" });

        using var ctx = CreateContext();
        (await ctx.Settings.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task SetMultipleSettingsAsync_PersistsAll()
    {
        var svc = CreateService();
        var dict = new Dictionary<string, object>
        {
            ["key1"] = "val1",
            ["key2"] = "val2",
            ["key3"] = "val3"
        };
        await svc.SetMultipleSettingsAsync("Batch", dict);

        using var ctx = CreateContext();
        var settings = await ctx.Settings.Where(s => s.Category == "Batch").ToListAsync();
        settings.Should().HaveCount(3);
    }

    #endregion

    #region GetAllSettingsAsync

    [Fact]
    public async Task GetAllSettingsAsync_ReturnsEmpty_WhenNoUser()
    {
        _session.IsAuthenticated.Returns(false);
        var svc = CreateService();
        var result = await svc.GetAllSettingsAsync();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllSettingsAsync_GroupsByCategory()
    {
        var svc = CreateService();
        await svc.SetSettingAsync("AI", "provider", "TestProv");
        await svc.SetSettingAsync("UI", "theme", "Dark");

        var result = await svc.GetAllSettingsAsync();
        result.Should().ContainKey("AI");
        result.Should().ContainKey("UI");
    }

    #endregion

    #region ExportSettingsAsync / ImportSettingsAsync

    [Fact]
    public async Task ExportSettingsAsync_ReturnsJson()
    {
        var svc = CreateService();
        await svc.SetSettingAsync("Export", "expKey", "expVal");

        var json = await svc.ExportSettingsAsync();
        json.Should().NotBeNullOrWhiteSpace();
        json.Should().Contain("Export");
    }

    [Fact]
    public async Task ImportSettingsAsync_ThrowsOnNull()
    {
        var svc = CreateService();
        await Assert.ThrowsAnyAsync<ArgumentException>(() => svc.ImportSettingsAsync(null!));
    }

    [Fact]
    public async Task ImportSettingsAsync_ImportsCategories()
    {
        var svc = CreateService();
        var json = """{"ImportCat":{"ik1":"iv1","ik2":"iv2"}}""";
        await svc.ImportSettingsAsync(json);

        using var ctx = CreateContext();
        var settings = await ctx.Settings.Where(s => s.Category == "ImportCat").ToListAsync();
        settings.Should().NotBeEmpty();
    }

    #endregion

    #region SyncWithCloudAsync

    [Fact]
    public async Task SyncWithCloudAsync_SkipsWhenNoUser()
    {
        _session.IsAuthenticated.Returns(false);
        var svc = CreateService();
        await svc.SyncWithCloudAsync();

        await _cloudSync.DidNotReceive()
            .SyncSettingsAsync(Arg.Any<IEnumerable<Setting>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncWithCloudAsync_SkipsWhenNoPending()
    {
        var svc = CreateService();
        await svc.SyncWithCloudAsync();

        await _cloudSync.DidNotReceive()
            .SyncSettingsAsync(Arg.Any<IEnumerable<Setting>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncWithCloudAsync_SyncsPending_AndMarksAsSynced()
    {
        // Seed a pending setting directly into DB
        using (var ctx = CreateContext())
        {
            ctx.Settings.Add(new Setting
            {
                UserId = _userId,
                Category = "SyncCat",
                Key = "sk",
                Value = "sv",
                ValueType = "String",
                SyncStatus = Setting.SyncStatuses.Pending
            });
            await ctx.SaveChangesAsync();
        }

        var svc = CreateService();
        await svc.SyncWithCloudAsync();

        await _cloudSync.Received(1)
            .SyncSettingsAsync(Arg.Any<IEnumerable<Setting>>(), Arg.Any<CancellationToken>());

        using var verifyCtx = CreateContext();
        var setting = await verifyCtx.Settings.FirstAsync(s => s.Key == "sk");
        setting.SyncStatus.Should().Be(Setting.SyncStatuses.Synced);
    }

    #endregion

    #region GetSyncStatusAsync

    [Fact]
    public async Task GetSyncStatusAsync_ReturnsZeros_WhenNoUser()
    {
        _session.IsAuthenticated.Returns(false);
        var svc = CreateService();
        var status = await svc.GetSyncStatusAsync();
        status.PendingCount.Should().Be(0);
        status.SyncedCount.Should().Be(0);
    }

    [Fact]
    public async Task GetSyncStatusAsync_ReturnsCounts()
    {
        using (var ctx = CreateContext())
        {
            ctx.Settings.Add(new Setting
            {
                UserId = _userId, Category = "c", Key = "k1", Value = "v", ValueType = "String",
                SyncStatus = Setting.SyncStatuses.Pending
            });
            ctx.Settings.Add(new Setting
            {
                UserId = _userId, Category = "c", Key = "k2", Value = "v", ValueType = "String",
                SyncStatus = Setting.SyncStatuses.Synced
            });
            await ctx.SaveChangesAsync();
        }

        var svc = CreateService();
        var status = await svc.GetSyncStatusAsync();
        status.PendingCount.Should().Be(1);
        status.SyncedCount.Should().Be(1);
    }

    #endregion
}
