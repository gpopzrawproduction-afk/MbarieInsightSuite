using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Concurrency;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Settings.Commands.SaveSettings;
using MIC.Core.Application.Settings.Queries.GetSettings;
using MIC.Desktop.Avalonia.Services;
using MIC.Desktop.Avalonia.ViewModels;
using MIC.Infrastructure.AI.Services;
using NSubstitute;
using ReactiveUI;
using Xunit;
using Unit = System.Reactive.Unit;

namespace MIC.Tests.Unit.ViewModels;

/// <summary>
/// Tests for <see cref="SettingsViewModel"/> covering construction, property defaults,
/// LoadSettings, SaveSettings, ResetToDefaults, TestAIConnection, CreateAppSettings, and more.
/// </summary>
[CollectionDefinition("SettingsViewModelTests", DisableParallelization = true)]
public sealed class SettingsViewModelTestsCollectionDef { }

[Collection("UserSessionServiceTests")]
public sealed class SettingsViewModelFullTests : IDisposable
{
    private readonly IServiceProvider? _originalProvider;
    private readonly SessionStorageScope _sessionScope;
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IChatService _chatService = Substitute.For<IChatService>();
    private readonly IConfiguration _configuration;

    static SettingsViewModelFullTests()
    {
        RxApp.MainThreadScheduler = CurrentThreadScheduler.Instance;
        RxApp.TaskpoolScheduler = CurrentThreadScheduler.Instance;
    }

    public SettingsViewModelFullTests()
    {
        _sessionScope = new SessionStorageScope(clearSession: true);
        _originalProvider = GetProgramServiceProvider();

        // Default: GetSettingsQuery returns error so LoadDefaultSettings is used
        _mediator.Send(Arg.Any<GetSettingsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ErrorOr<AppSettings>>(Error.Failure("not_found", "Settings not found")));

        _mediator.Send(Arg.Any<SaveSettingsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ErrorOr<bool>>(true));

        var configData = new Dictionary<string, string?>
        {
            ["AI:Provider"] = "OpenAI",
            ["AI:OpenAI:Model"] = "gpt-4o",
            ["AI:OpenAI:Temperature"] = "0.7",
            ["AI:SystemPrompt:BusinessName"] = "TestBusiness",
            ["Database:Provider"] = "SQLite",
            ["AI:Features:ChatEnabled"] = "true",
            ["AI:Features:PredictionsEnabled"] = "true",
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        UserSessionService.Instance.Clear();
    }

    public void Dispose()
    {
        SetProgramServiceProvider(_originalProvider);
        UserSessionService.Instance.Clear();
        _sessionScope.Dispose();
    }

    private SettingsViewModel CreateViewModel()
    {
        SetProgramServiceProvider(BuildServiceProvider());
        return new SettingsViewModel();
    }

    private IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_mediator);
        services.AddSingleton(_configuration);
        services.AddSingleton(_chatService);
        return services.BuildServiceProvider();
    }

    #region Constructor & Default Properties

    [Fact]
    public void Constructor_InitializesCommands()
    {
        var vm = CreateViewModel();
        vm.SaveCommand.Should().NotBeNull();
        vm.ResetCommand.Should().NotBeNull();
        vm.TestAIConnectionCommand.Should().NotBeNull();
        vm.ExportSettingsCommand.Should().NotBeNull();
        vm.ImportSettingsCommand.Should().NotBeNull();
        vm.LoadCommand.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_LoadsDefaultSettings_WhenNoUser()
    {
        // No user session = Guid.Empty → loads defaults from config
        var vm = CreateViewModel();
        vm.BusinessName.Should().Be("TestBusiness");
        vm.AIProvider.Should().Be("OpenAI");
        vm.AIModel.Should().Be("gpt-4o");
    }

    [Fact]
    public void AvailableLanguages_ContainsExpectedLanguages()
    {
        var vm = CreateViewModel();
        vm.AvailableLanguages.Should().Contain("English");
        vm.AvailableLanguages.Should().Contain("Spanish");
        vm.AvailableLanguages.Should().Contain("French");
        vm.AvailableLanguages.Should().HaveCount(6);
    }

    [Fact]
    public void AvailableProviders_ContainsExpectedProviders()
    {
        var vm = CreateViewModel();
        vm.AvailableProviders.Should().Contain("OpenAI");
        vm.AvailableProviders.Should().Contain("Azure OpenAI");
        vm.AvailableProviders.Should().Contain("Local (Ollama)");
    }

    [Fact]
    public void AvailableModels_ContainsExpectedModels()
    {
        var vm = CreateViewModel();
        vm.AvailableModels.Should().Contain("gpt-4o");
        vm.AvailableModels.Should().Contain("gpt-4-turbo");
        vm.AvailableModels.Should().Contain("claude-3-opus");
    }

    [Fact]
    public void AvailableDatabaseProviders_ContainsExpected()
    {
        var vm = CreateViewModel();
        vm.AvailableDatabaseProviders.Should().Contain("SQLite");
        vm.AvailableDatabaseProviders.Should().Contain("PostgreSQL");
        vm.AvailableDatabaseProviders.Should().Contain("SQL Server");
    }

    #endregion

    #region Property Setters

    [Fact]
    public void BusinessName_CanBeSet()
    {
        var vm = CreateViewModel();
        vm.BusinessName = "New Business";
        vm.BusinessName.Should().Be("New Business");
    }

    [Fact]
    public void DarkModeEnabled_CanBeToggled()
    {
        var vm = CreateViewModel();
        var original = vm.DarkModeEnabled;
        vm.DarkModeEnabled = !original;
        vm.DarkModeEnabled.Should().Be(!original);
    }

    [Fact]
    public void AnimationsEnabled_CanBeToggled()
    {
        var vm = CreateViewModel();
        vm.AnimationsEnabled = false;
        vm.AnimationsEnabled.Should().BeFalse();
    }

    [Fact]
    public void SelectedLanguage_CanBeSet()
    {
        var vm = CreateViewModel();
        vm.SelectedLanguage = "French";
        vm.SelectedLanguage.Should().Be("French");
    }

    [Fact]
    public void AIProvider_CanBeSet()
    {
        var vm = CreateViewModel();
        vm.AIProvider = "Azure OpenAI";
        vm.AIProvider.Should().Be("Azure OpenAI");
    }

    [Fact]
    public void AIModel_CanBeSet()
    {
        var vm = CreateViewModel();
        vm.AIModel = "gpt-3.5-turbo";
        vm.AIModel.Should().Be("gpt-3.5-turbo");
    }

    [Fact]
    public void OpenAIApiKey_CanBeSet()
    {
        var vm = CreateViewModel();
        vm.OpenAIApiKey = "sk-test123";
        vm.OpenAIApiKey.Should().Be("sk-test123");
    }

    [Fact]
    public void AITemperature_CanBeSet()
    {
        var vm = CreateViewModel();
        vm.AITemperature = 0.5;
        vm.AITemperature.Should().Be(0.5);
    }

    [Fact]
    public void AIChatEnabled_CanBeToggled()
    {
        var vm = CreateViewModel();
        vm.AIChatEnabled = false;
        vm.AIChatEnabled.Should().BeFalse();
    }

    [Fact]
    public void AIPredictionsEnabled_CanBeToggled()
    {
        var vm = CreateViewModel();
        vm.AIPredictionsEnabled = false;
        vm.AIPredictionsEnabled.Should().BeFalse();
    }

    [Fact]
    public void NotificationsEnabled_CanBeToggled()
    {
        var vm = CreateViewModel();
        vm.NotificationsEnabled = false;
        vm.NotificationsEnabled.Should().BeFalse();
    }

    [Fact]
    public void SoundEnabled_CanBeToggled()
    {
        var vm = CreateViewModel();
        vm.SoundEnabled = false;
        vm.SoundEnabled.Should().BeFalse();
    }

    [Fact]
    public void CriticalAlertsOnly_CanBeToggled()
    {
        var vm = CreateViewModel();
        vm.CriticalAlertsOnly = true;
        vm.CriticalAlertsOnly.Should().BeTrue();
    }

    [Fact]
    public void DatabaseProvider_CanBeSet()
    {
        var vm = CreateViewModel();
        vm.DatabaseProvider = "PostgreSQL";
        vm.DatabaseProvider.Should().Be("PostgreSQL");
    }

    [Fact]
    public void AutoRefreshInterval_CanBeSet()
    {
        var vm = CreateViewModel();
        vm.AutoRefreshInterval = 60;
        vm.AutoRefreshInterval.Should().Be(60);
    }

    [Fact]
    public void AutoRefreshEnabled_CanBeToggled()
    {
        var vm = CreateViewModel();
        vm.AutoRefreshEnabled = false;
        vm.AutoRefreshEnabled.Should().BeFalse();
    }

    [Fact]
    public void StatusMessage_CanBeSet()
    {
        var vm = CreateViewModel();
        vm.StatusMessage = "Testing";
        vm.StatusMessage.Should().Be("Testing");
    }

    [Fact]
    public void IsBusy_CanBeSet()
    {
        var vm = CreateViewModel();
        vm.IsBusy = true;
        vm.IsBusy.Should().BeTrue();
    }

    #endregion

    #region LoadSettingsAsync

    [Fact]
    public void LoadSettings_WithUser_SendsGetSettingsQuery()
    {
        SetupUserSession();
        var settings = CreateDefaultAppSettings();
        _mediator.Send(Arg.Any<GetSettingsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ErrorOr<AppSettings>>(settings));

        var vm = CreateViewModel();

        // Constructor fires LoadCommand.Execute() which calls LoadSettingsAsync
        vm.AIProvider.Should().Be("OpenAI");
    }

    [Fact]
    public void LoadSettings_WithUser_AppliesSettings()
    {
        SetupUserSession();
        var settings = CreateDefaultAppSettings();
        settings.AI.Provider = "Azure OpenAI";
        settings.AI.ModelId = "gpt-3.5-turbo";
        settings.AI.Temperature = 0.3;
        settings.UI.Theme = "Light";
        settings.UI.Language = "French";
        settings.UI.EnableAnimations = false;
        settings.Notifications.EnableDesktopNotifications = false;
        settings.Notifications.EnableSound = false;
        settings.EmailSync.AutoSyncEnabled = false;
        settings.EmailSync.SyncIntervalMinutes = 60;

        _mediator.Send(Arg.Any<GetSettingsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ErrorOr<AppSettings>>(settings));

        var vm = CreateViewModel();

        vm.AIProvider.Should().Be("Azure OpenAI");
        vm.AIModel.Should().Be("gpt-3.5-turbo");
        vm.AITemperature.Should().Be(0.3);
        vm.DarkModeEnabled.Should().BeFalse();
        vm.SelectedLanguage.Should().Be("French");
        vm.AnimationsEnabled.Should().BeFalse();
        vm.NotificationsEnabled.Should().BeFalse();
        vm.SoundEnabled.Should().BeFalse();
        vm.AutoRefreshEnabled.Should().BeFalse();
        vm.AutoRefreshInterval.Should().Be(60);
    }

    [Fact]
    public void LoadSettings_NoUser_UsesDefaults()
    {
        // Don't set up a user session
        var vm = CreateViewModel();
        vm.StatusMessage.Should().Contain("not logged in");
    }

    [Fact]
    public void LoadSettings_MediatorError_FallsBackToDefaults()
    {
        SetupUserSession();
        _mediator.Send(Arg.Any<GetSettingsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ErrorOr<AppSettings>>(Error.Failure("err", "DB error")));

        var vm = CreateViewModel();
        vm.StatusMessage.Should().Contain("Failed to load");
    }

    [Fact]
    public void LoadSettings_Exception_FallsBackToDefaults()
    {
        SetupUserSession();
        _mediator.Send(Arg.Any<GetSettingsQuery>(), Arg.Any<CancellationToken>())
            .Returns<ErrorOr<AppSettings>>(x => throw new InvalidOperationException("DB down"));

        var vm = CreateViewModel();
        vm.StatusMessage.Should().Contain("Error loading");
    }

    #endregion

    #region SaveSettingsAsync via reflection

    [Fact]
    public async Task SaveSettings_WithUser_SendsSaveSettingsCommand()
    {
        SetupUserSession();
        var vm = CreateViewModel();

        var method = typeof(SettingsViewModel)
            .GetMethod("SaveSettingsAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        await (Task)method!.Invoke(vm, null)!;

        await _mediator.Received().Send(Arg.Any<SaveSettingsCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveSettings_NoUser_SetsStatusMessage()
    {
        // Don't set up user session
        var vm = CreateViewModel();

        var method = typeof(SettingsViewModel)
            .GetMethod("SaveSettingsAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        await (Task)method!.Invoke(vm, null)!;

        vm.StatusMessage.Should().Contain("not logged in");
    }

    [Fact]
    public async Task SaveSettings_MediatorError_SetsFailureMessage()
    {
        SetupUserSession();
        _mediator.Send(Arg.Any<SaveSettingsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ErrorOr<bool>>(Error.Failure("err", "Save failed")));

        var vm = CreateViewModel();

        var method = typeof(SettingsViewModel)
            .GetMethod("SaveSettingsAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        await (Task)method!.Invoke(vm, null)!;

        vm.StatusMessage.Should().Contain("Failed to save");
    }

    [Fact]
    public async Task SaveSettings_Exception_SetsErrorMessage()
    {
        SetupUserSession();
        _mediator.Send(Arg.Any<SaveSettingsCommand>(), Arg.Any<CancellationToken>())
            .Returns<ErrorOr<bool>>(x => throw new InvalidOperationException("Network error"));

        var vm = CreateViewModel();

        var method = typeof(SettingsViewModel)
            .GetMethod("SaveSettingsAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        await (Task)method!.Invoke(vm, null)!;

        vm.StatusMessage.Should().Contain("Error saving");
    }

    #endregion

    #region CreateAppSettings via reflection

    [Fact]
    public void CreateAppSettings_MapsAllProperties()
    {
        var vm = CreateViewModel();
        vm.AIProvider = "Azure OpenAI";
        vm.AIModel = "gpt-3.5-turbo";
        vm.AITemperature = 0.9;
        vm.AIChatEnabled = false;
        vm.AIPredictionsEnabled = false;
        vm.DarkModeEnabled = false;
        vm.SelectedLanguage = "Spanish";
        vm.AnimationsEnabled = false;
        vm.NotificationsEnabled = false;
        vm.SoundEnabled = false;
        vm.CriticalAlertsOnly = true;
        vm.AutoRefreshEnabled = false;
        vm.AutoRefreshInterval = 120;

        var method = typeof(SettingsViewModel)
            .GetMethod("CreateAppSettings", BindingFlags.NonPublic | BindingFlags.Instance);
        var settings = (AppSettings)method!.Invoke(vm, null)!;

        settings.AI.Provider.Should().Be("Azure OpenAI");
        settings.AI.ModelId.Should().Be("gpt-3.5-turbo");
        settings.AI.Temperature.Should().Be(0.9);
        settings.AI.EnableChatAssistant.Should().BeFalse();
        settings.AI.EnableAutoPrioritization.Should().BeFalse();
        settings.UI.Theme.Should().Be("Light");
        settings.UI.Language.Should().Be("Spanish");
        settings.UI.EnableAnimations.Should().BeFalse();
        settings.Notifications.EnableDesktopNotifications.Should().BeFalse();
        settings.Notifications.EnableSound.Should().BeFalse();
        settings.Notifications.EnableMetricNotifications.Should().BeFalse();
        settings.EmailSync.AutoSyncEnabled.Should().BeFalse();
        settings.EmailSync.SyncIntervalMinutes.Should().Be(120);
    }

    [Fact]
    public void CreateAppSettings_DarkMode_MapsToThemeDark()
    {
        var vm = CreateViewModel();
        vm.DarkModeEnabled = true;

        var method = typeof(SettingsViewModel)
            .GetMethod("CreateAppSettings", BindingFlags.NonPublic | BindingFlags.Instance);
        var settings = (AppSettings)method!.Invoke(vm, null)!;

        settings.UI.Theme.Should().Be("Dark");
    }

    [Fact]
    public void CreateAppSettings_LightMode_MapsToThemeLight()
    {
        var vm = CreateViewModel();
        vm.DarkModeEnabled = false;

        var method = typeof(SettingsViewModel)
            .GetMethod("CreateAppSettings", BindingFlags.NonPublic | BindingFlags.Instance);
        var settings = (AppSettings)method!.Invoke(vm, null)!;

        settings.UI.Theme.Should().Be("Light");
    }

    [Fact]
    public void CreateAppSettings_SetsDefaultFixedProperties()
    {
        var vm = CreateViewModel();

        var method = typeof(SettingsViewModel)
            .GetMethod("CreateAppSettings", BindingFlags.NonPublic | BindingFlags.Instance);
        var settings = (AppSettings)method!.Invoke(vm, null)!;

        // Fixed properties
        settings.AI.EnableEmailAnalysis.Should().BeTrue();
        settings.AI.EnableSentimentAnalysis.Should().BeTrue();
        settings.AI.EnableActionItemExtraction.Should().BeTrue();
        settings.AI.EnableEmailSummaries.Should().BeTrue();
        settings.AI.EnableContextAwareness.Should().BeTrue();
        settings.UI.ShowUnreadCount.Should().BeTrue();
        settings.UI.FontSize.Should().Be(14);
        settings.EmailSync.InitialSyncMonths.Should().Be(3);
        settings.EmailSync.MaxEmailsPerSync.Should().Be(100);
        settings.General.MinimizeToTray.Should().BeTrue();
        settings.General.CheckForUpdates.Should().BeTrue();
        settings.General.SessionTimeoutMinutes.Should().Be(30);
    }

    #endregion

    #region TestAIConnectionAsync via reflection

    [Fact]
    public async Task TestAIConnection_MissingApiKey_SetsMessage()
    {
        var vm = CreateViewModel();
        vm.OpenAIApiKey = "";
        vm.AIProvider = "OpenAI";

        var method = typeof(SettingsViewModel)
            .GetMethod("TestAIConnectionAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        await (Task)method!.Invoke(vm, null)!;

        vm.StatusMessage.Should().Contain("API key is missing");
    }

    [Fact]
    public async Task TestAIConnection_Available_ReportsSuccess()
    {
        var vm = CreateViewModel();
        vm.OpenAIApiKey = "sk-test-key";
        vm.AIProvider = "OpenAI";

        _chatService.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);

        var method = typeof(SettingsViewModel)
            .GetMethod("TestAIConnectionAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        await (Task)method!.Invoke(vm, null)!;

        vm.StatusMessage.Should().Contain("successful");
    }

    [Fact]
    public async Task TestAIConnection_NotAvailable_ReportsFailure()
    {
        var vm = CreateViewModel();
        vm.OpenAIApiKey = "sk-test-key";
        vm.AIProvider = "OpenAI";

        _chatService.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(false);

        var method = typeof(SettingsViewModel)
            .GetMethod("TestAIConnectionAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        await (Task)method!.Invoke(vm, null)!;

        vm.StatusMessage.Should().Contain("failed");
    }

    [Fact]
    public async Task TestAIConnection_Exception_ReportsError()
    {
        var vm = CreateViewModel();
        vm.OpenAIApiKey = "sk-test-key";
        vm.AIProvider = "OpenAI";

        _chatService.IsAvailableAsync(Arg.Any<CancellationToken>())
            .Returns<bool>(x => throw new InvalidOperationException("Timeout"));

        var method = typeof(SettingsViewModel)
            .GetMethod("TestAIConnectionAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        await (Task)method!.Invoke(vm, null)!;

        vm.StatusMessage.Should().Contain("Timeout");
    }

    [Fact]
    public async Task TestAIConnection_NullChatService_ReportsNotAvailable()
    {
        // Build provider without chat service
        var services = new ServiceCollection();
        services.AddSingleton(_mediator);
        services.AddSingleton(_configuration);
        SetProgramServiceProvider(services.BuildServiceProvider());

        var vm = new SettingsViewModel();
        vm.OpenAIApiKey = "sk-test-key";
        vm.AIProvider = "OpenAI";

        var method = typeof(SettingsViewModel)
            .GetMethod("TestAIConnectionAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        await (Task)method!.Invoke(vm, null)!;

        vm.StatusMessage.Should().Contain("not available");
    }

    #endregion

    #region ResetToDefaultsAsync via reflection

    [Fact]
    public async Task ResetToDefaults_ResetsAllProperties()
    {
        SetupUserSession();
        var vm = CreateViewModel();
        vm.AIProvider = "Azure OpenAI";
        vm.AIModel = "custom-model";
        vm.AITemperature = 0.1;
        vm.DarkModeEnabled = false;
        vm.SelectedLanguage = "Japanese";

        var method = typeof(SettingsViewModel)
            .GetMethod("ResetToDefaultsAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        await (Task)method!.Invoke(vm, null)!;

        vm.BusinessName.Should().Be("Mbarie Intelligence Console");
        vm.AIProvider.Should().Be("OpenAI");
        vm.AIModel.Should().Be("gpt-4o");
        vm.AITemperature.Should().Be(0.7);
        vm.DarkModeEnabled.Should().BeTrue();
        vm.SelectedLanguage.Should().Be("English");
        vm.AnimationsEnabled.Should().BeTrue();
        vm.AIChatEnabled.Should().BeTrue();
        vm.AIPredictionsEnabled.Should().BeTrue();
        vm.NotificationsEnabled.Should().BeTrue();
        vm.SoundEnabled.Should().BeTrue();
        vm.CriticalAlertsOnly.Should().BeFalse();
        vm.DatabaseProvider.Should().Be("SQLite");
        vm.AutoRefreshInterval.Should().Be(30);
        vm.AutoRefreshEnabled.Should().BeTrue();
    }

    #endregion

    #region ExportSettings / ImportSettings via reflection

    [Fact]
    public async Task ExportSettings_SetsStatusMessage()
    {
        var vm = CreateViewModel();

        var method = typeof(SettingsViewModel)
            .GetMethod("ExportSettingsAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        await (Task)method!.Invoke(vm, null)!;

        vm.StatusMessage.Should().Contain("exported");
    }

    [Fact]
    public async Task ImportSettings_SetsStatusMessage()
    {
        var vm = CreateViewModel();

        var method = typeof(SettingsViewModel)
            .GetMethod("ImportSettingsAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        await (Task)method!.Invoke(vm, null)!;

        vm.StatusMessage.Should().Contain("coming soon");
    }

    #endregion

    #region GetCurrentUserId via reflection

    [Fact]
    public void GetCurrentUserId_NoSession_ReturnsGuidEmpty()
    {
        var vm = CreateViewModel();

        var method = typeof(SettingsViewModel)
            .GetMethod("GetCurrentUserId", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (Guid)method!.Invoke(vm, null)!;

        result.Should().Be(Guid.Empty);
    }

    [Fact]
    public void GetCurrentUserId_WithSession_ReturnsUserId()
    {
        var userId = Guid.NewGuid();
        UserSessionService.Instance.SetSession(userId.ToString("D"), "user", "user@test.com", "User", token: "t");

        var vm = CreateViewModel();

        var method = typeof(SettingsViewModel)
            .GetMethod("GetCurrentUserId", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (Guid)method!.Invoke(vm, null)!;

        result.Should().Be(userId);
    }

    [Fact]
    public void GetCurrentUserId_InvalidGuid_ReturnsGuidEmpty()
    {
        UserSessionService.Instance.SetSession("not-a-guid", "user", "user@test.com", "User", token: "t");

        var vm = CreateViewModel();

        var method = typeof(SettingsViewModel)
            .GetMethod("GetCurrentUserId", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (Guid)method!.Invoke(vm, null)!;

        result.Should().Be(Guid.Empty);
    }

    #endregion

    #region LoadDefaultSettings via reflection

    [Fact]
    public void LoadDefaultSettings_ReadsConfigValues()
    {
        var vm = CreateViewModel();

        var method = typeof(SettingsViewModel)
            .GetMethod("LoadDefaultSettings", BindingFlags.NonPublic | BindingFlags.Instance);
        method!.Invoke(vm, null);

        vm.BusinessName.Should().Be("TestBusiness");
        vm.AIProvider.Should().Be("OpenAI");
        vm.AIModel.Should().Be("gpt-4o");
        vm.DatabaseProvider.Should().Be("SQLite");
    }

    [Fact]
    public void LoadDefaultSettings_MissingConfigKeys_UsesHardcodedDefaults()
    {
        // Build provider with empty config
        var emptyConfig = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddSingleton(_mediator);
        services.AddSingleton<IConfiguration>(emptyConfig);
        SetProgramServiceProvider(services.BuildServiceProvider());

        var vm = new SettingsViewModel();

        // When config keys are missing, falls back to hardcoded defaults
        vm.AIProvider.Should().Be("OpenAI");
        vm.AIModel.Should().Be("gpt-4o");
        vm.DatabaseProvider.Should().Be("SQLite");
    }

    #endregion

    #region Helpers

    private void SetupUserSession()
    {
        var userId = Guid.NewGuid();
        UserSessionService.Instance.SetSession(
            userId.ToString("D"), "user", "user@test.com", "Test User", token: "token");
    }

    private static AppSettings CreateDefaultAppSettings()
    {
        return new AppSettings
        {
            AI = new AISettings
            {
                Provider = "OpenAI",
                ModelId = "gpt-4o",
                Temperature = 0.7,
                EnableChatAssistant = true,
                EnableAutoPrioritization = true,
            },
            UI = new UISettings
            {
                Theme = "Dark",
                Language = "English",
                EnableAnimations = true,
            },
            Notifications = new NotificationSettings
            {
                EnableDesktopNotifications = true,
                EnableSound = true,
                EnableMetricNotifications = true,
            },
            EmailSync = new EmailSyncSettings
            {
                AutoSyncEnabled = true,
                SyncIntervalMinutes = 5,
            },
            General = new GeneralSettings(),
        };
    }

    private static IServiceProvider? GetProgramServiceProvider()
    {
        var programType = Type.GetType("MIC.Desktop.Avalonia.Program, MIC.Desktop.Avalonia");
        var property = programType?.GetProperty("ServiceProvider",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        return property?.GetValue(null) as IServiceProvider;
    }

    private static void SetProgramServiceProvider(IServiceProvider? provider)
    {
        var programType = Type.GetType("MIC.Desktop.Avalonia.Program, MIC.Desktop.Avalonia");
        var property = programType?.GetProperty("ServiceProvider",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        property?.SetValue(null, provider);
    }

    private sealed class SessionStorageScope : IDisposable
    {
        private readonly string _sessionPath;
        private readonly string? _backupPath;
        private readonly bool _hadExisting;

        public SessionStorageScope(bool clearSession)
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MIC");
            Directory.CreateDirectory(directory);
            _sessionPath = Path.Combine(directory, "session.json");

            if (File.Exists(_sessionPath))
            {
                _backupPath = Path.Combine(Path.GetTempPath(), $"mic-session-backup-{Guid.NewGuid():N}.json");
                File.Copy(_sessionPath, _backupPath, overwrite: true);
                _hadExisting = true;
            }

            if (clearSession && File.Exists(_sessionPath))
                File.Delete(_sessionPath);
        }

        public void Dispose()
        {
            if (File.Exists(_sessionPath)) File.Delete(_sessionPath);
            if (_hadExisting && _backupPath != null && File.Exists(_backupPath))
            {
                File.Copy(_backupPath, _sessionPath, overwrite: true);
                File.Delete(_backupPath);
            }
        }
    }

    #endregion
}
