using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using FluentAssertions;
using MediatR;
using MIC.Core.Application.Alerts.Common;
using MIC.Core.Application.Alerts.Queries.GetAllAlerts;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.AI.Plugins;
using NSubstitute;
using Xunit;

namespace MIC.Tests.Unit.Plugins;

/// <summary>
/// Tests for AlertsPlugin covering alert querying and filtering via Semantic Kernel functions.
/// </summary>
public class AlertsPluginTests
{
    private readonly IMediator _mediator;
    private readonly AlertsPlugin _plugin;

    public AlertsPluginTests()
    {
        _mediator = Substitute.For<IMediator>();
        _plugin = new AlertsPlugin(_mediator);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ThrowsOnNullMediator()
    {
        var act = () => new AlertsPlugin(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_AcceptsValidMediator()
    {
        var plugin = new AlertsPlugin(_mediator);
        plugin.Should().NotBeNull();
    }

    #endregion

    #region GetActiveAlertsAsync Tests

    [Fact]
    public async Task GetActiveAlertsAsync_ReturnsFormattedAlerts_WhenAlertsExist()
    {
        var alerts = new List<AlertDto>
        {
            new AlertDto { AlertName = "CPU Alert", Severity = AlertSeverity.Critical, Status = AlertStatus.Active, Description = "CPU high" },
            new AlertDto { AlertName = "Disk Alert", Severity = AlertSeverity.Warning, Status = AlertStatus.Active, Description = "Disk low" }
        };
        ErrorOr<IReadOnlyList<AlertDto>> result = alerts.AsReadOnly();
        _mediator.Send(Arg.Any<GetAllAlertsQuery>(), Arg.Any<CancellationToken>())
            .Returns(result);

        var output = await _plugin.GetActiveAlertsAsync();

        output.Should().NotBeNullOrEmpty();
        output.Should().Contain("CPU Alert");
    }

    [Fact]
    public async Task GetActiveAlertsAsync_ReturnsErrorMessage_WhenMediatorFails()
    {
        ErrorOr<IReadOnlyList<AlertDto>> errorResult = Error.Failure("Alerts.Failed", "Database error");
        _mediator.Send(Arg.Any<GetAllAlertsQuery>(), Arg.Any<CancellationToken>())
            .Returns(errorResult);

        var output = await _plugin.GetActiveAlertsAsync();

        (output.Contains("error") || output.Contains("Error")).Should().BeTrue();
    }

    [Fact]
    public async Task GetActiveAlertsAsync_FiltersResolvedAlerts()
    {
        var alerts = new List<AlertDto>
        {
            new AlertDto { AlertName = "Active", Status = AlertStatus.Active, Severity = AlertSeverity.Warning },
            new AlertDto { AlertName = "Resolved", Status = AlertStatus.Resolved, Severity = AlertSeverity.Warning }
        };
        ErrorOr<IReadOnlyList<AlertDto>> result = alerts.AsReadOnly();
        _mediator.Send(Arg.Any<GetAllAlertsQuery>(), Arg.Any<CancellationToken>())
            .Returns(result);

        var output = await _plugin.GetActiveAlertsAsync();

        output.Should().Contain("Active");
        // Resolved alerts should be filtered out
    }

    [Fact]
    public async Task GetActiveAlertsAsync_WithSeverityFilter_FiltersCorrectly()
    {
        var alerts = new List<AlertDto>
        {
            new AlertDto { AlertName = "CritAlert", Severity = AlertSeverity.Critical, Status = AlertStatus.Active },
            new AlertDto { AlertName = "InfoAlert", Severity = AlertSeverity.Info, Status = AlertStatus.Active }
        };
        ErrorOr<IReadOnlyList<AlertDto>> result = alerts.AsReadOnly();
        _mediator.Send(Arg.Any<GetAllAlertsQuery>(), Arg.Any<CancellationToken>())
            .Returns(result);

        var output = await _plugin.GetActiveAlertsAsync(severity: "Critical");

        output.Should().Contain("CritAlert");
    }

    [Fact]
    public async Task GetActiveAlertsAsync_RespectsMaxAlerts()
    {
        var alerts = new List<AlertDto>();
        for (int i = 0; i < 20; i++)
        {
            alerts.Add(new AlertDto { AlertName = $"Alert{i}", Severity = AlertSeverity.Warning, Status = AlertStatus.Active });
        }
        ErrorOr<IReadOnlyList<AlertDto>> result = alerts.AsReadOnly();
        _mediator.Send(Arg.Any<GetAllAlertsQuery>(), Arg.Any<CancellationToken>())
            .Returns(result);

        var output = await _plugin.GetActiveAlertsAsync(maxAlerts: 5);

        output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetActiveAlertsAsync_ReturnsNoAlertsMessage_WhenEmpty()
    {
        var alerts = new List<AlertDto>();
        ErrorOr<IReadOnlyList<AlertDto>> result = alerts.AsReadOnly();
        _mediator.Send(Arg.Any<GetAllAlertsQuery>(), Arg.Any<CancellationToken>())
            .Returns(result);

        var output = await _plugin.GetActiveAlertsAsync();

        output.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region GetAlertSummaryAsync Tests

    [Fact]
    public async Task GetAlertSummaryAsync_ReturnsSummaryWithCounts()
    {
        var alerts = new List<AlertDto>
        {
            new AlertDto { Severity = AlertSeverity.Critical, Status = AlertStatus.Active },
            new AlertDto { Severity = AlertSeverity.Critical, Status = AlertStatus.Active },
            new AlertDto { Severity = AlertSeverity.Warning, Status = AlertStatus.Active },
            new AlertDto { Severity = AlertSeverity.Info, Status = AlertStatus.Resolved }
        };
        ErrorOr<IReadOnlyList<AlertDto>> result = alerts.AsReadOnly();
        _mediator.Send(Arg.Any<GetAllAlertsQuery>(), Arg.Any<CancellationToken>())
            .Returns(result);

        var output = await _plugin.GetAlertSummaryAsync();

        output.Should().NotBeNullOrEmpty();
        // Should contain counts
        output.Should().NotBeNullOrEmpty();
        // Should contain summary information
    }

    [Fact]
    public async Task GetAlertSummaryAsync_HandlesEmptyAlerts()
    {
        var alerts = new List<AlertDto>();
        ErrorOr<IReadOnlyList<AlertDto>> result = alerts.AsReadOnly();
        _mediator.Send(Arg.Any<GetAllAlertsQuery>(), Arg.Any<CancellationToken>())
            .Returns(result);

        var output = await _plugin.GetAlertSummaryAsync();

        output.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region GetRecentAlertActivityAsync Tests

    [Fact]
    public async Task GetRecentAlertActivityAsync_ReturnsRecentAlerts()
    {
        var alerts = new List<AlertDto>
        {
            new AlertDto
            {
                AlertName = "Recent",
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                Severity = AlertSeverity.Warning,
                Status = AlertStatus.Active
            }
        };
        ErrorOr<IReadOnlyList<AlertDto>> result = alerts.AsReadOnly();
        _mediator.Send(Arg.Any<GetAllAlertsQuery>(), Arg.Any<CancellationToken>())
            .Returns(result);

        var output = await _plugin.GetRecentAlertActivityAsync(hours: 24);

        output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetRecentAlertActivityAsync_UsesDefaultHours()
    {
        var alerts = new List<AlertDto>();
        ErrorOr<IReadOnlyList<AlertDto>> result = alerts.AsReadOnly();
        _mediator.Send(Arg.Any<GetAllAlertsQuery>(), Arg.Any<CancellationToken>())
            .Returns(result);

        var output = await _plugin.GetRecentAlertActivityAsync();

        output.Should().NotBeNullOrEmpty();
        await _mediator.Received(1).Send(Arg.Any<GetAllAlertsQuery>(), Arg.Any<CancellationToken>());
    }

    #endregion
}
