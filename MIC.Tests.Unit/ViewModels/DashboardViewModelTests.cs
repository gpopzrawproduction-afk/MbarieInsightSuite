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

    private sealed class ImmediateUiDispatcher : IUiDispatcher
    {
        public Task RunAsync(Action action)
        {
            action();
            return Task.CompletedTask;
        }
    }
}
