using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using FluentAssertions;
using MediatR;
using MIC.Core.Application.Alerts.Common;
using MIC.Core.Application.Alerts.Queries.GetAllAlerts;
using MIC.Core.Application.Authentication.Common;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Metrics.Common;
using MIC.Core.Application.Metrics.Queries.GetMetrics;
using MIC.Core.Domain.Entities;
using MIC.Desktop.Avalonia.ViewModels;
using MIC.Desktop.Avalonia.Services;
using Moq;
using ReactiveUI;
using Xunit;

using DomainUserRole = MIC.Core.Domain.Entities.UserRole;

namespace MIC.Tests.Unit.ViewModels;

public sealed class DashboardViewModelTests
{
    private static readonly IUiDispatcher UiDispatcher = new ImmediateUiDispatcher();

    static DashboardViewModelTests()
    {
        RxApp.MainThreadScheduler = CurrentThreadScheduler.Instance;
        RxApp.TaskpoolScheduler = CurrentThreadScheduler.Instance;
    }

    [Fact]
    public async Task RefreshCommand_WhenDependenciesReturnData_PopulatesDashboardState()
    {
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var alerts = new List<AlertDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                AlertName = "Database outage",
                Severity = AlertSeverity.Emergency,
                Status = AlertStatus.Active,
                Source = "Infra",
                TriggeredAt = now.AddMinutes(-15)
            },
            new()
            {
                Id = Guid.NewGuid(),
                AlertName = "Latency spike",
                Severity = AlertSeverity.Warning,
                Status = AlertStatus.Acknowledged,
                Source = "Monitoring",
                TriggeredAt = now.AddMinutes(-5)
            },
            new()
            {
                Id = Guid.NewGuid(),
                AlertName = "Daily digest",
                Severity = AlertSeverity.Info,
                Status = AlertStatus.Resolved,
                Source = "Ops",
                TriggeredAt = now.AddHours(-1)
            }
        };
        var metrics = new List<MetricDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                MetricName = "Throughput",
                Category = "Operations",
                Source = "Ops",
                Unit = "%",
                Value = 97.5,
                Timestamp = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                MetricName = "Error rate",
                Category = "Operations",
                Source = "Ops",
                Unit = "%",
                Value = 1.2,
                Timestamp = now
            }
        };

        var emailAccountId = Guid.NewGuid();
        var urgentEmail = new EmailMessage(
            "msg-1",
            "Urgent incident",
            "alerts@example.com",
            "Alerts",
            "user@example.com",
            now.AddHours(-3),
            now.AddHours(-2),
            "Critical body",
            userId,
            emailAccountId);
        urgentEmail.SetAIAnalysis(
            EmailPriority.Urgent,
            EmailCategory.Action,
            SentimentType.Negative,
            hasActionItems: true,
            requiresResponse: true,
            summary: "Critical incident needs attention");

        var highPriorityEmail = new EmailMessage(
            "msg-2",
            "Weekly summary",
            "reports@example.com",
            "Reports",
            "user@example.com",
            now.AddHours(-5),
            now.AddHours(-4),
            "Summary body",
            userId,
            emailAccountId);
        highPriorityEmail.SetAIAnalysis(
            EmailPriority.High,
            EmailCategory.Report,
            SentimentType.Neutral,
            hasActionItems: false,
            requiresResponse: false,
            summary: "Weekly summary ready");

        var mediatorMock = new Mock<IMediator>(MockBehavior.Strict);
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAllAlertsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(alerts);
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetMetricsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        var navigationServiceMock = new Mock<INavigationService>();

        var sessionServiceMock = new Mock<ISessionService>();
        sessionServiceMock
            .Setup(s => s.GetUser())
            .Returns(new UserDto
            {
                Id = userId,
                Username = "user",
                Email = "user@example.com",
                Role = DomainUserRole.Admin,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });

        var emailRepositoryMock = new Mock<IEmailRepository>(MockBehavior.Strict);
        emailRepositoryMock
            .Setup(r => r.GetEmailsAsync(
                userId,
                null,
                EmailFolder.Inbox,
                null,
                0,
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailMessage> { urgentEmail, highPriorityEmail });
        emailRepositoryMock
            .Setup(r => r.GetUnreadCountAsync(userId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        emailRepositoryMock
            .Setup(r => r.GetRequiresResponseCountAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var viewModel = new DashboardViewModel(
            mediatorMock.Object,
            navigationServiceMock.Object,
            sessionServiceMock.Object,
            emailRepositoryMock.Object,
            UiDispatcher);

        var refreshTask = viewModel.RefreshCommand.Execute().ToTask();
        Dispatcher.UIThread.RunJobs(null);
        await refreshTask;
        Dispatcher.UIThread.RunJobs(null);

        viewModel.IsLoading.Should().BeFalse();
        viewModel.ActiveAlerts.Should().Be(2);
        viewModel.RecentAlerts.Should().HaveCount(3);
        viewModel.RecentAlerts.First().Title.Should().Be("Database outage");
        viewModel.TotalMetrics.Should().Be(metrics.Count);
        viewModel.TotalEmails.Should().Be(2);
        viewModel.UnreadCount.Should().Be(1);
        viewModel.HighPriorityCount.Should().Be(2);
        viewModel.RequiresResponseCount.Should().Be(1);
        viewModel.RecentEmails.Should().HaveCount(2);
        viewModel.RefreshStatus.Should().Be("Auto-refresh: ON (30s)");
        viewModel.LastUpdated.Should().NotBeNullOrEmpty();

        mediatorMock.VerifyAll();
        emailRepositoryMock.VerifyAll();

        viewModel.AutoRefreshEnabled = false;
    }

    [Fact]
    public async Task RefreshCommand_WhenUserNotAuthenticated_ClearsEmailSection()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAllAlertsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AlertDto>());
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetMetricsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MetricDto>());

        var navigationServiceMock = new Mock<INavigationService>();

        var sessionServiceMock = new Mock<ISessionService>();
        sessionServiceMock
            .Setup(s => s.GetUser())
            .Returns(new UserDto
            {
                Id = Guid.Empty,
                Username = string.Empty,
                Email = string.Empty,
                Role = DomainUserRole.Guest,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });

        var emailRepositoryMock = new Mock<IEmailRepository>(MockBehavior.Strict);

        var viewModel = new DashboardViewModel(
            mediatorMock.Object,
            navigationServiceMock.Object,
            sessionServiceMock.Object,
            emailRepositoryMock.Object,
            UiDispatcher);

        var refreshTask = viewModel.RefreshCommand.Execute().ToTask();
        Dispatcher.UIThread.RunJobs(null);
        await refreshTask;
        Dispatcher.UIThread.RunJobs(null);

        viewModel.TotalEmails.Should().Be(0);
        viewModel.UnreadCount.Should().Be(0);
        viewModel.HighPriorityCount.Should().Be(0);
        viewModel.RequiresResponseCount.Should().Be(0);
        viewModel.RecentEmails.Should().BeEmpty();

        emailRepositoryMock.Verify(r => r.GetEmailsAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid?>(),
                It.IsAny<EmailFolder?>(),
                It.IsAny<bool?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        emailRepositoryMock.Verify(r => r.GetUnreadCountAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
        emailRepositoryMock.Verify(r => r.GetRequiresResponseCountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);

        viewModel.AutoRefreshEnabled = false;
    }

    [Fact]
    public async Task ToggleAutoRefreshCommand_TogglesStateAndStatus()
    {
        var mediatorMock = new Mock<IMediator>();
        var navigationServiceMock = new Mock<INavigationService>();
        var sessionServiceMock = new Mock<ISessionService>();
        sessionServiceMock
            .Setup(s => s.GetUser())
            .Returns(new UserDto
            {
                Id = Guid.NewGuid(),
                Username = "user",
                Email = "user@example.com",
                Role = DomainUserRole.Admin,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });

        var emailRepositoryMock = new Mock<IEmailRepository>();

        var viewModel = new DashboardViewModel(
            mediatorMock.Object,
            navigationServiceMock.Object,
            sessionServiceMock.Object,
            emailRepositoryMock.Object,
            UiDispatcher);

        viewModel.AutoRefreshEnabled.Should().BeTrue();

        await viewModel.ToggleAutoRefreshCommand.Execute().ToTask();
        viewModel.AutoRefreshEnabled.Should().BeFalse();
        viewModel.RefreshStatus.Should().Be("Auto-refresh: OFF");

        await viewModel.ToggleAutoRefreshCommand.Execute().ToTask();
        viewModel.AutoRefreshEnabled.Should().BeTrue();
        viewModel.RefreshStatus.Should().Be("Auto-refresh: ON (30s)");

        viewModel.AutoRefreshEnabled = false;
    }

    [Fact]
    public async Task NavigationCommands_InvokeNavigationService()
    {
        var mediatorMock = new Mock<IMediator>();
        var navigationServiceMock = new Mock<INavigationService>();
        var sessionServiceMock = new Mock<ISessionService>();
        sessionServiceMock
            .Setup(s => s.GetUser())
            .Returns(new UserDto
            {
                Id = Guid.NewGuid(),
                Username = "user",
                Email = "user@example.com",
                Role = DomainUserRole.User,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });

        var emailRepositoryMock = new Mock<IEmailRepository>();

        var viewModel = new DashboardViewModel(
            mediatorMock.Object,
            navigationServiceMock.Object,
            sessionServiceMock.Object,
            emailRepositoryMock.Object,
            UiDispatcher);

        await viewModel.CheckInboxCommand.Execute().ToTask();
        await viewModel.ViewUrgentItemsCommand.Execute().ToTask();
        await viewModel.AIChatCommand.Execute().ToTask();

        navigationServiceMock.Verify(n => n.NavigateTo("Email"), Times.Once);
        navigationServiceMock.Verify(n => n.NavigateTo("Alerts"), Times.Once);
        navigationServiceMock.Verify(n => n.NavigateTo("AI Chat"), Times.Once);

        viewModel.AutoRefreshEnabled = false;
    }

    #region Constructor Guard Tests

    [Fact]
    public void Constructor_ThrowsOnNullMediator()
    {
        var act = () => new DashboardViewModel(
            null!,
            new Mock<INavigationService>().Object,
            new Mock<ISessionService>().Object,
            new Mock<IEmailRepository>().Object,
            UiDispatcher);

        act.Should().Throw<ArgumentNullException>().WithParameterName("mediator");
    }

    [Fact]
    public void Constructor_ThrowsOnNullNavigationService()
    {
        var act = () => new DashboardViewModel(
            new Mock<IMediator>().Object,
            null!,
            new Mock<ISessionService>().Object,
            new Mock<IEmailRepository>().Object,
            UiDispatcher);

        act.Should().Throw<ArgumentNullException>().WithParameterName("navigationService");
    }

    [Fact]
    public void Constructor_ThrowsOnNullSessionService()
    {
        var act = () => new DashboardViewModel(
            new Mock<IMediator>().Object,
            new Mock<INavigationService>().Object,
            null!,
            new Mock<IEmailRepository>().Object,
            UiDispatcher);

        act.Should().Throw<ArgumentNullException>().WithParameterName("sessionService");
    }

    [Fact]
    public void Constructor_ThrowsOnNullEmailRepository()
    {
        var act = () => new DashboardViewModel(
            new Mock<IMediator>().Object,
            new Mock<INavigationService>().Object,
            new Mock<ISessionService>().Object,
            null!,
            UiDispatcher);

        act.Should().Throw<ArgumentNullException>().WithParameterName("emailRepository");
    }

    #endregion

    #region Static Helper Method Tests

    private static string InvokeGetRelativeTime(DateTime dateTime)
    {
        var method = typeof(DashboardViewModel).GetMethod("GetRelativeTime",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        return (string)method.Invoke(null, new object[] { dateTime })!;
    }

    private static string InvokeGetSeverityColor(string? severity)
    {
        var method = typeof(DashboardViewModel).GetMethod("GetSeverityColor",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        return (string)method.Invoke(null, new object?[] { severity })!;
    }

    private static string InvokeGetSeverityBadgeBackground(string? severity)
    {
        var method = typeof(DashboardViewModel).GetMethod("GetSeverityBadgeBackground",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        return (string)method.Invoke(null, new object?[] { severity })!;
    }

    [Fact]
    public void GetRelativeTime_LessThanOneMinute_ReturnsJustNow()
    {
        InvokeGetRelativeTime(DateTime.UtcNow.AddSeconds(-10)).Should().Be("Just now");
    }

    [Fact]
    public void GetRelativeTime_Minutes_ReturnsMinutesAgo()
    {
        InvokeGetRelativeTime(DateTime.UtcNow.AddMinutes(-15)).Should().EndWith("m ago");
    }

    [Fact]
    public void GetRelativeTime_Hours_ReturnsHoursAgo()
    {
        InvokeGetRelativeTime(DateTime.UtcNow.AddHours(-5)).Should().EndWith("h ago");
    }

    [Fact]
    public void GetRelativeTime_Days_ReturnsDaysAgo()
    {
        InvokeGetRelativeTime(DateTime.UtcNow.AddDays(-3)).Should().EndWith("d ago");
    }

    [Fact]
    public void GetRelativeTime_MoreThanWeek_ReturnsFormattedDate()
    {
        var date = DateTime.UtcNow.AddDays(-10);
        InvokeGetRelativeTime(date).Should().Contain(date.ToString("MMM"));
    }

    [Fact]
    public void GetSeverityColor_Emergency_ReturnsRed()
    {
        InvokeGetSeverityColor("Emergency").Should().Be("#EF4444");
    }

    [Fact]
    public void GetSeverityColor_Critical_ReturnsRed()
    {
        InvokeGetSeverityColor("Critical").Should().Be("#EF4444");
    }

    [Fact]
    public void GetSeverityColor_Warning_ReturnsAmber()
    {
        InvokeGetSeverityColor("Warning").Should().Be("#F59E0B");
    }

    [Fact]
    public void GetSeverityColor_Info_ReturnsBlue()
    {
        InvokeGetSeverityColor("Info").Should().Be("#3B82F6");
    }

    [Fact]
    public void GetSeverityColor_Unknown_ReturnsGray()
    {
        InvokeGetSeverityColor("SomethingElse").Should().Be("#64748B");
    }

    [Fact]
    public void GetSeverityColor_Null_ReturnsGray()
    {
        InvokeGetSeverityColor(null).Should().Be("#64748B");
    }

    [Fact]
    public void GetSeverityBadgeBackground_Emergency_ReturnsSemiTransparentRed()
    {
        InvokeGetSeverityBadgeBackground("Emergency").Should().Be("#EF444420");
    }

    [Fact]
    public void GetSeverityBadgeBackground_Warning_ReturnsSemiTransparentAmber()
    {
        InvokeGetSeverityBadgeBackground("Warning").Should().Be("#F59E0B20");
    }

    [Fact]
    public void GetSeverityBadgeBackground_Info_ReturnsSemiTransparentBlue()
    {
        InvokeGetSeverityBadgeBackground("Info").Should().Be("#3B82F620");
    }

    [Fact]
    public void GetSeverityBadgeBackground_Unknown_ReturnsSemiTransparentGray()
    {
        InvokeGetSeverityBadgeBackground("Other").Should().Be("#64748B20");
    }

    #endregion

    #region Sub-ViewModel Tests

    [Fact]
    public void DashboardAlertViewModel_HasDefaultProperties()
    {
        var vm = new DashboardAlertViewModel();
        vm.Title.Should().BeEmpty();
        vm.Source.Should().BeEmpty();
        vm.TimeAgo.Should().BeEmpty();
        vm.SeverityText.Should().BeEmpty();
        vm.SeverityColor.Should().Be("#64748B");
        vm.SeverityBadgeBackground.Should().Be("#64748B20");
    }

    [Fact]
    public void DashboardAlertViewModel_CanSetProperties()
    {
        var vm = new DashboardAlertViewModel
        {
            Title = "Alert",
            Source = "System",
            TimeAgo = "5m ago",
            SeverityText = "Critical",
            SeverityColor = "#EF4444",
            SeverityBadgeBackground = "#EF444420"
        };
        vm.Title.Should().Be("Alert");
        vm.Source.Should().Be("System");
        vm.SeverityText.Should().Be("Critical");
    }

    [Fact]
    public void DashboardPredictionViewModel_ConfidenceText_ReturnsPercentage()
    {
        var vm = new DashboardPredictionViewModel { Confidence = 85 };
        vm.ConfidenceText.Should().Be("85%");
    }

    [Fact]
    public void DashboardPredictionViewModel_HasDefaultProperties()
    {
        var vm = new DashboardPredictionViewModel();
        vm.Title.Should().BeEmpty();
        vm.Timestamp.Should().BeEmpty();
        vm.Confidence.Should().Be(0);
        vm.ConfidenceText.Should().Be("0%");
        vm.TrendText.Should().BeEmpty();
        vm.TrendColor.Should().Be("#64748B");
    }

    [Fact]
    public void DashboardEmailViewModel_HasDefaultProperties()
    {
        var vm = new DashboardEmailViewModel();
        vm.Subject.Should().BeEmpty();
        vm.Sender.Should().BeEmpty();
        vm.TimeAgo.Should().BeEmpty();
        vm.PriorityIcon.Should().BeEmpty();
        vm.PriorityColor.Should().Be("#64748B");
    }

    #endregion

    #region Error Path Tests

    [Fact]
    public async Task RefreshCommand_WhenMediatorThrows_SetsErrorStatus()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAllAlertsQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var sessionServiceMock = new Mock<ISessionService>();
        sessionServiceMock
            .Setup(s => s.GetUser())
            .Returns(new UserDto
            {
                Id = Guid.NewGuid(),
                Username = "user",
                Email = "user@example.com",
                Role = DomainUserRole.User,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });

        var viewModel = new DashboardViewModel(
            mediatorMock.Object,
            new Mock<INavigationService>().Object,
            sessionServiceMock.Object,
            new Mock<IEmailRepository>().Object,
            UiDispatcher);

        await viewModel.RefreshCommand.Execute().ToTask();

        viewModel.RefreshStatus.Should().Contain("Error");
        viewModel.IsLoading.Should().BeFalse();

        viewModel.AutoRefreshEnabled = false;
    }

    [Fact]
    public void AutoRefreshEnabled_DefaultTrue()
    {
        var mediatorMock = new Mock<IMediator>();
        var sessionServiceMock = new Mock<ISessionService>();
        sessionServiceMock.Setup(s => s.GetUser()).Returns(new UserDto
        {
            Id = Guid.Empty, Username = "", Email = "",
            Role = DomainUserRole.Guest,
            CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        });

        var viewModel = new DashboardViewModel(
            mediatorMock.Object,
            new Mock<INavigationService>().Object,
            sessionServiceMock.Object,
            new Mock<IEmailRepository>().Object,
            UiDispatcher);

        viewModel.AutoRefreshEnabled.Should().BeTrue();
        viewModel.AutoRefreshEnabled = false;
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
