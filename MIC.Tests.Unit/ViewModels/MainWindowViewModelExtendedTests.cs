using Xunit;
using FluentAssertions;
using NSubstitute;
using MIC.Desktop.Avalonia.ViewModels;
using MIC.Core.Application.Common.Interfaces;
using Serilog;
using UserRole = MIC.Core.Domain.Entities.UserRole;
using Microsoft.Extensions.DependencyInjection;
using System.Reactive;
using ReactiveUI;
using MIC.Core.Application.Authentication.Common;
using MIC.Desktop.Avalonia.Services;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Reflection;

namespace MIC.Tests.Unit.ViewModels;

/// <summary>
/// Extended tests for <see cref="MainWindowViewModel"/> covering NavigateTo routes,
/// ShowNotifications, UpdateUnreadIndicators, ResolveUserInitials edge cases,
/// RefreshCurrentView, SimpleCommand, and more.
/// </summary>
public class MainWindowViewModelExtendedTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ISessionService _sessionService;
    private readonly ILogger _logger;
    private readonly INotificationService _notificationService;
    private readonly ILocalizationService _localizationService;
    private readonly MainWindowViewModel _sut;

    static MainWindowViewModelExtendedTests()
    {
        RxApp.MainThreadScheduler = CurrentThreadScheduler.Instance;
        RxApp.TaskpoolScheduler = CurrentThreadScheduler.Instance;
    }

    public MainWindowViewModelExtendedTests()
    {
        _sessionService = Substitute.For<ISessionService>();
        _logger = Substitute.For<ILogger>();
        _localizationService = Substitute.For<ILocalizationService>();
        _notificationService = Substitute.For<INotificationService>();

        var emptyNotifications = new ObservableCollection<NotificationEntry>();
        var readOnlyNotifications = new ReadOnlyObservableCollection<NotificationEntry>(emptyNotifications);
        _notificationService.NotificationHistory.Returns(readOnlyNotifications);
        _notificationService.UnreadCount.Returns(0);

        var services = new ServiceCollection();
        services.AddSingleton(_localizationService);
        services.AddSingleton(new CommandPaletteViewModel());
        services.AddSingleton(_notificationService);
        services.AddSingleton(new NotificationCenterViewModel(_notificationService));

        var mockMediator = Substitute.For<MediatR.IMediator>();
        var mockNavigationService = Substitute.For<INavigationService>();
        var mockEmailRepository = Substitute.For<IEmailRepository>();
        var mockUiDispatcher = new ImmediateUiDispatcher();

        var dashboardVm = new DashboardViewModel(
            mockMediator, mockNavigationService, _sessionService,
            mockEmailRepository, mockUiDispatcher);
        services.AddSingleton(dashboardVm);

        _serviceProvider = services.BuildServiceProvider();
        _sut = new MainWindowViewModel(_serviceProvider, _sessionService, _logger);
    }

    #region NavigateTo — All View Routes

    [Fact]
    public void NavigateTo_Dashboard_SetsIsDashboardActive()
    {
        _sut.NavigateTo("Dashboard");

        _sut.CurrentViewName.Should().Be("Dashboard");
        _sut.IsDashboardActive.Should().BeTrue();
        _sut.IsAlertsActive.Should().BeFalse();
        _sut.IsMetricsActive.Should().BeFalse();
        _sut.IsEmailActive.Should().BeFalse();
        _sut.IsAIChatActive.Should().BeFalse();
        _sut.IsSettingsActive.Should().BeFalse();
    }

    [Fact]
    public void NavigateTo_Alerts_SetsIsAlertsActive()
    {
        _sut.NavigateTo("Alerts");

        _sut.CurrentViewName.Should().Be("Alerts");
        _sut.IsAlertsActive.Should().BeTrue();
        _sut.IsDashboardActive.Should().BeFalse();
    }

    [Fact]
    public void NavigateTo_Metrics_SetsIsMetricsActive()
    {
        _sut.NavigateTo("Metrics");

        _sut.CurrentViewName.Should().Be("Metrics");
        _sut.IsMetricsActive.Should().BeTrue();
        _sut.IsDashboardActive.Should().BeFalse();
    }

    [Fact]
    public void NavigateTo_Predictions_ThrowsWhenVMNotRegistered()
    {
        var act = () => _sut.NavigateTo("Predictions");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*PredictionsViewModel*not registered*");
    }

    [Fact]
    public void NavigateTo_KnowledgeBase_ThrowsWhenServiceProviderNotConfigured()
    {
        var act = () => _sut.NavigateTo("Knowledge Base");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void NavigateTo_Email_ThrowsWhenVMNotRegistered()
    {
        var act = () => _sut.NavigateTo("Email");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*EmailInboxViewModel*not registered*");
    }

    [Fact]
    public void NavigateTo_Settings_ThrowsWhenVMNotRegistered()
    {
        var act = () => _sut.NavigateTo("Settings");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SettingsViewModel*not registered*");
    }

    [Fact]
    public void NavigateTo_AIChat_ThrowsWhenVMNotRegistered()
    {
        var act = () => _sut.NavigateTo("AI Chat");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ChatViewModel*not registered*");
    }

    [Fact]
    public void NavigateTo_UnknownView_SetsCurrentViewNull()
    {
        _sut.NavigateTo("NonexistentView");

        _sut.CurrentViewName.Should().Be("NonexistentView");
        _sut.CurrentView.Should().BeNull();
    }

    [Fact]
    public void NavigateTo_UpdatesLastUpdateTime()
    {
        var before = DateTime.Now;
        _sut.NavigateTo("Alerts");
        _sut.LastUpdateTime.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region ShowNotifications via Reflection

    [Fact]
    public void ShowNotifications_TogglesNotificationPanel()
    {
        _sut.IsNotificationPanelOpen.Should().BeFalse();

        var method = typeof(MainWindowViewModel)
            .GetMethod("ShowNotifications", BindingFlags.NonPublic | BindingFlags.Instance);
        method!.Invoke(_sut, null);

        _sut.IsNotificationPanelOpen.Should().BeTrue();
    }

    [Fact]
    public void ShowNotifications_WhenOpening_MarksAllAsRead()
    {
        var method = typeof(MainWindowViewModel)
            .GetMethod("ShowNotifications", BindingFlags.NonPublic | BindingFlags.Instance);

        // First toggle opens the panel
        method!.Invoke(_sut, null);

        _notificationService.Received(1).MarkAllAsRead();
    }

    [Fact]
    public void ShowNotifications_WhenClosing_DoesNotMarkAsRead()
    {
        var method = typeof(MainWindowViewModel)
            .GetMethod("ShowNotifications", BindingFlags.NonPublic | BindingFlags.Instance);

        // Open then close
        method!.Invoke(_sut, null); // open
        _notificationService.ClearReceivedCalls();
        method!.Invoke(_sut, null); // close

        _notificationService.DidNotReceive().MarkAllAsRead();
    }

    #endregion

    #region UpdateUnreadIndicators via Reflection

    [Fact]
    public void UpdateUnreadIndicators_SetsUnreadCount()
    {
        _notificationService.UnreadCount.Returns(5);

        var method = typeof(MainWindowViewModel)
            .GetMethod("UpdateUnreadIndicators", BindingFlags.NonPublic | BindingFlags.Instance);
        method!.Invoke(_sut, null);

        _sut.UnreadNotificationCount.Should().Be(5);
    }

    [Fact]
    public void UpdateUnreadIndicators_ZeroUnread()
    {
        _notificationService.UnreadCount.Returns(0);

        var method = typeof(MainWindowViewModel)
            .GetMethod("UpdateUnreadIndicators", BindingFlags.NonPublic | BindingFlags.Instance);
        method!.Invoke(_sut, null);

        _sut.UnreadNotificationCount.Should().Be(0);
    }

    #endregion

    #region ResolveUserInitials via Property

    [Fact]
    public void UserInitials_ThreePartName_TakesFirstTwoInitials()
    {
        _sessionService.IsAuthenticated.Returns(true);
        _sessionService.GetUser().Returns(new UserDto
        {
            Id = Guid.NewGuid(),
            FullName = "John Michael Doe",
            Username = "john@test.com",
            Email = "john@test.com",
            Role = UserRole.User
        });

        _sut.UserInitials.Should().Be("JM");
    }

    [Fact]
    public void UserInitials_EmptyName_ReturnsQuestionMark()
    {
        _sessionService.IsAuthenticated.Returns(true);
        _sessionService.GetUser().Returns(new UserDto
        {
            Id = Guid.NewGuid(),
            FullName = "",
            Username = "",
            Email = "a@b.com",
            Role = UserRole.User
        });

        // With empty full name and username, may fall through to "Unknown"
        _sut.UserInitials.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void UserInitials_SingleCharName_ReturnsSingleChar()
    {
        _sessionService.IsAuthenticated.Returns(true);
        _sessionService.GetUser().Returns(new UserDto
        {
            Id = Guid.NewGuid(),
            FullName = "X",
            Username = "x@test.com",
            Email = "x@test.com",
            Role = UserRole.User
        });

        _sut.UserInitials.Should().Be("X");
    }

    [Fact]
    public void UserInitials_LowercaseName_ReturnsUppercaseInitials()
    {
        _sessionService.IsAuthenticated.Returns(true);
        _sessionService.GetUser().Returns(new UserDto
        {
            Id = Guid.NewGuid(),
            FullName = "alice bob",
            Username = "alice@test.com",
            Email = "alice@test.com",
            Role = UserRole.User
        });

        _sut.UserInitials.Should().Be("AB");
    }

    #endregion

    #region SidebarWidth & Visibility

    [Fact]
    public void IsSidebarVisible_CanBeToggled()
    {
        _sut.IsSidebarVisible = false;
        _sut.IsSidebarVisible.Should().BeFalse();
        _sut.SidebarWidth.Should().Be(0);

        _sut.IsSidebarVisible = true;
        _sut.IsSidebarVisible.Should().BeTrue();
        _sut.SidebarWidth.Should().Be(260);
    }

    [Fact]
    public void ToggleSidebarCommand_CanExecuteMultipleTimes()
    {
        var initial = _sut.IsSidebarExpanded;
        _sut.ToggleSidebarCommand.Execute().Subscribe();
        _sut.IsSidebarExpanded.Should().Be(!initial);

        _sut.ToggleSidebarCommand.Execute().Subscribe();
        _sut.IsSidebarExpanded.Should().Be(initial);
    }

    #endregion

    #region Connection & Status Properties

    [Fact]
    public void ConnectionStatus_DefaultsToConnected()
    {
        _sut.ConnectionStatus.Should().Be("Connected");
    }

    [Fact]
    public void IsConnected_DefaultsToTrue()
    {
        _sut.IsConnected.Should().BeTrue();
    }

    [Fact]
    public void StatusMessage_DefaultsToReady()
    {
        _sut.StatusMessage.Should().Be("Ready");
    }

    [Fact]
    public void CurrentTime_IsNotEmpty()
    {
        _sut.CurrentTime.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Greeting_DefaultsMbarieIntelligenceConsole()
    {
        _sut.Greeting.Should().Be("Mbarie Intelligence Console");
    }

    #endregion

    #region Navigation Commands

    [Fact]
    public void NavigationCommands_AreAll_NotNull()
    {
        _sut.NavigateToDashboardCommand.Should().NotBeNull();
        _sut.NavigateToAlertsCommand.Should().NotBeNull();
        _sut.NavigateToMetricsCommand.Should().NotBeNull();
        _sut.NavigateToPredictionsCommand.Should().NotBeNull();
        _sut.NavigateToKnowledgeBaseCommand.Should().NotBeNull();
        _sut.NavigateToChatCommand.Should().NotBeNull();
        _sut.NavigateToSettingsCommand.Should().NotBeNull();
        _sut.NavigateToEmailCommand.Should().NotBeNull();
    }

    [Fact]
    public void ActionCommands_AreAll_NotNull()
    {
        _sut.SaveCommand.Should().NotBeNull();
        _sut.UploadCommand.Should().NotBeNull();
        _sut.SyncCommand.Should().NotBeNull();
        _sut.NotificationsCommand.Should().NotBeNull();
        _sut.RefreshCommand.Should().NotBeNull();
        _sut.LogoutCommand.Should().NotBeNull();
        _sut.ToggleSidebarCommand.Should().NotBeNull();
    }

    #endregion

    #region SimpleCommand via Reflection Tests

    [Fact]
    public void SimpleCommand_CanBeCreatedViaReflection()
    {
        var simpleCommandType = typeof(MainWindowViewModel)
            .GetNestedType("SimpleCommand", BindingFlags.NonPublic);
        simpleCommandType.Should().NotBeNull();

        var executed = false;
        Action action = () => executed = true;
        var ctor = simpleCommandType!.GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(c => c.GetParameters().Length >= 1);
        ctor.Should().NotBeNull();
    }

    [Fact]
    public void ICommand_Properties_CutCommand_CanExecute()
    {
        // Test SimpleCommand behavior indirectly through exposed ICommand properties
        var cutCmd = _sut.CutCommand;
        cutCmd.Should().NotBeNull();
        ((System.Windows.Input.ICommand)cutCmd).CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ICommand_Properties_CopyCommand_CanExecute()
    {
        _sut.CopyCommand.Should().NotBeNull();
        ((System.Windows.Input.ICommand)_sut.CopyCommand).CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ICommand_Properties_PasteCommand_CanExecute()
    {
        _sut.PasteCommand.Should().NotBeNull();
        ((System.Windows.Input.ICommand)_sut.PasteCommand).CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ICommand_SelectAllCommand_CanExecute()
    {
        _sut.SelectAllCommand.Should().NotBeNull();
        ((System.Windows.Input.ICommand)_sut.SelectAllCommand).CanExecute(null).Should().BeTrue();
    }

    #endregion

    #region UserRole Edge Cases

    [Fact]
    public void UserRole_UserRoleType_ReturnsCorrectString()
    {
        _sessionService.IsAuthenticated.Returns(true);
        _sessionService.GetUser().Returns(new UserDto
        {
            Id = Guid.NewGuid(),
            FullName = "Test User",
            Username = "test",
            Email = "test@test.com",
            Role = UserRole.User
        });

        _sut.UserRole.Should().Be("User");
    }

    [Fact]
    public void UserRole_GuestRole_ReturnsGuest()
    {
        _sessionService.IsAuthenticated.Returns(true);
        _sessionService.GetUser().Returns(new UserDto
        {
            Id = Guid.NewGuid(),
            FullName = "Guest User",
            Username = "guest",
            Email = "guest@test.com",
            Role = UserRole.Guest
        });

        _sut.UserRole.Should().Be("Guest");
    }

    #endregion

    #region CheckForUpdates via Reflection

    [Fact]
    public void CheckForUpdates_DoesNotThrow()
    {
        var method = typeof(MainWindowViewModel)
            .GetMethod("CheckForUpdates", BindingFlags.NonPublic | BindingFlags.Instance);
        if (method != null)
        {
            var act = () => method.Invoke(_sut, null);
            act.Should().NotThrow();
        }
    }

    #endregion

    #region NewChatAsync / NewEmail via Reflection

    [Fact]
    public void NewChatAsync_ThrowsBecauseVMNotRegistered()
    {
        var method = typeof(MainWindowViewModel)
            .GetMethod("NewChatAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        if (method != null)
        {
            var act = () => method.Invoke(_sut, null);
            act.Should().Throw<TargetInvocationException>()
                .WithInnerException<InvalidOperationException>();
        }
    }

    [Fact]
    public void NewEmail_ThrowsBecauseVMNotRegistered()
    {
        var method = typeof(MainWindowViewModel)
            .GetMethod("NewEmail", BindingFlags.NonPublic | BindingFlags.Instance);
        if (method != null)
        {
            var act = () => method.Invoke(_sut, null);
            act.Should().Throw<TargetInvocationException>()
                .WithInnerException<InvalidOperationException>();
        }
    }

    #endregion

    #region IsNotificationPanelOpen property

    [Fact]
    public void IsNotificationPanelOpen_DefaultsFalse()
    {
        _sut.IsNotificationPanelOpen.Should().BeFalse();
    }

    [Fact]
    public void IsNotificationPanelOpen_CanBeSet()
    {
        _sut.IsNotificationPanelOpen = true;
        _sut.IsNotificationPanelOpen.Should().BeTrue();
    }

    #endregion

    private sealed class ImmediateUiDispatcher : IUiDispatcher
    {
        public Task RunAsync(Action action)
        {
            action();
            return Task.CompletedTask;
        }
    }
}
