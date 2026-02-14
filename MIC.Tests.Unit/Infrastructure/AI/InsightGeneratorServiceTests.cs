using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MIC.Infrastructure.AI.Models;
using MIC.Infrastructure.AI.Services;
using NSubstitute;
using Xunit;

namespace MIC.Tests.Unit.Infrastructure.AI;

/// <summary>
/// Tests for InsightGeneratorService covering daily summaries, metrics analysis, and alert analysis.
/// Target: 12 tests for comprehensive insight generation coverage
/// </summary>
public class InsightGeneratorServiceTests
{
    private readonly IChatService _mockChatService;
    private readonly InsightGeneratorService _sut;

    public InsightGeneratorServiceTests()
    {
        _mockChatService = Substitute.For<IChatService>();
        _sut = new InsightGeneratorService(_mockChatService);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullChatService_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new InsightGeneratorService(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("chatService");
    }

    #endregion

    #region GenerateDailySummaryAsync Tests

    [Fact]
    public async Task GenerateDailySummaryAsync_WithSuccessfulResponse_ReturnsInsightWithContent()
    {
        // Arrange
        var chatResponse = new ChatCompletionResult
        {
            Success = true,
            Response = "Business health: Strong. Key metrics: Revenue up 15%, Costs stable. Alerts: 2 minor issues. Recommendation: Focus on customer retention."
        };
        _mockChatService.SendMessageAsync(
            Arg.Any<string>(),
            "daily-summary",
            Arg.Any<CancellationToken>())
            .Returns(chatResponse);

        // Act
        var result = await _sut.GenerateDailySummaryAsync();

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be(InsightType.Summary);
        result.Title.Should().Be("Daily Business Summary");
        result.Description.Should().Be(chatResponse.Response);
        result.Severity.Should().Be(InsightSeverity.Info);
        result.Confidence.Should().BeApproximately(0.85, 0.01);
    }

    [Fact]
    public async Task GenerateDailySummaryAsync_WithFailedResponse_ReturnsInsightWithErrorMessage()
    {
        // Arrange
        var chatResponse = new ChatCompletionResult
        {
            Success = false,
            Error = "AI service unavailable"
        };
        _mockChatService.SendMessageAsync(
            Arg.Any<string>(),
            "daily-summary",
            Arg.Any<CancellationToken>())
            .Returns(chatResponse);

        // Act
        var result = await _sut.GenerateDailySummaryAsync();

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be(InsightType.Summary);
        result.Description.Should().Be("Unable to generate summary.");
        result.Confidence.Should().Be(0.0);
    }

    [Fact]
    public async Task GenerateDailySummaryAsync_SendsCorrectPrompt()
    {
        // Arrange
        _mockChatService.SendMessageAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(new ChatCompletionResult { Success = true, Response = "Summary" });

        // Act
        await _sut.GenerateDailySummaryAsync();

        // Assert
        await _mockChatService.Received(1).SendMessageAsync(
            Arg.Is<string>(prompt => 
                prompt.Contains("daily business summary") &&
                prompt.Contains("business health") &&
                prompt.Contains("Key metrics") &&
                prompt.Contains("Critical alerts")),
            "daily-summary",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateDailySummaryAsync_RespectsCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        _mockChatService.SendMessageAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(new ChatCompletionResult { Success = true, Response = "Summary" });

        // Act
        await _sut.GenerateDailySummaryAsync(cts.Token);

        // Assert
        await _mockChatService.Received(1).SendMessageAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            cts.Token);
    }

    #endregion

    #region AnalyzeMetricsAsync Tests

    [Fact]
    public async Task AnalyzeMetricsAsync_WithSuccessfulResponse_ReturnsInsightList()
    {
        // Arrange
        var chatResponse = new ChatCompletionResult
        {
            Success = true,
            Response = "Revenue trend: Up 10% over last month. Expenses: Within budget. Customer acquisition cost: Trending down."
        };
        _mockChatService.SendMessageAsync(
            Arg.Any<string>(),
            "metric-analysis",
            Arg.Any<CancellationToken>())
            .Returns(chatResponse);

        // Act
        var result = await _sut.AnalyzeMetricsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Type.Should().Be(InsightType.Trend);
        result[0].Title.Should().Be("Metrics Analysis");
        result[0].Description.Should().Be(chatResponse.Response);
        result[0].Severity.Should().Be(InsightSeverity.Info);
        result[0].Confidence.Should().BeApproximately(0.8, 0.01);
    }

    [Fact]
    public async Task AnalyzeMetricsAsync_WithFailedResponse_ReturnsEmptyList()
    {
        // Arrange
        var chatResponse = new ChatCompletionResult
        {
            Success = false,
            Error = "Analysis failed"
        };
        _mockChatService.SendMessageAsync(
            Arg.Any<string>(),
            "metric-analysis",
            Arg.Any<CancellationToken>())
            .Returns(chatResponse);

        // Act
        var result = await _sut.AnalyzeMetricsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeMetricsAsync_SendsCorrectPrompt()
    {
        // Arrange
        _mockChatService.SendMessageAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(new ChatCompletionResult { Success = true, Response = "Analysis" });

        // Act
        await _sut.AnalyzeMetricsAsync();

        // Assert
        await _mockChatService.Received(1).SendMessageAsync(
            Arg.Is<string>(prompt => 
                prompt.Contains("business metrics") &&
                prompt.Contains("trends") &&
                prompt.Contains("target")),
            "metric-analysis",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnalyzeMetricsAsync_RespectsCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        _mockChatService.SendMessageAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(new ChatCompletionResult { Success = true, Response = "Analysis" });

        // Act
        await _sut.AnalyzeMetricsAsync(cts.Token);

        // Assert
        await _mockChatService.Received(1).SendMessageAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            cts.Token);
    }

    #endregion

    #region AnalyzeAlertsAsync Tests

    [Fact]
    public async Task AnalyzeAlertsAsync_WithSuccessfulResponse_ReturnsInsightList()
    {
        // Arrange
        var chatResponse = new ChatCompletionResult
        {
            Success = true,
            Response = "Critical: 3 high-priority alerts. Pattern: Email connectivity issues. Recommended: Check SMTP settings and credentials."
        };
        _mockChatService.SendMessageAsync(
            Arg.Any<string>(),
            "alert-analysis",
            Arg.Any<CancellationToken>())
            .Returns(chatResponse);

        // Act
        var result = await _sut.AnalyzeAlertsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Type.Should().Be(InsightType.Alert);
        result[0].Title.Should().Be("Alert Analysis");
        result[0].Description.Should().Be(chatResponse.Response);
        result[0].Severity.Should().Be(InsightSeverity.Warning);
        result[0].Confidence.Should().BeApproximately(0.75, 0.01);
    }

    [Fact]
    public async Task AnalyzeAlertsAsync_WithFailedResponse_ReturnsEmptyList()
    {
        // Arrange
        var chatResponse = new ChatCompletionResult
        {
            Success = false,
            Error = "Alert analysis failed"
        };
        _mockChatService.SendMessageAsync(
            Arg.Any<string>(),
            "alert-analysis",
            Arg.Any<CancellationToken>())
            .Returns(chatResponse);

        // Act
        var result = await _sut.AnalyzeAlertsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeAlertsAsync_SendsCorrectPrompt()
    {
        // Arrange
        _mockChatService.SendMessageAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(new ChatCompletionResult { Success = true, Response = "Alert analysis" });

        // Act
        await _sut.AnalyzeAlertsAsync();

        // Assert
        await _mockChatService.Received(1).SendMessageAsync(
            Arg.Is<string>(prompt => 
                prompt.Contains("active alerts") &&
                prompt.Contains("critical issues") &&
                prompt.Contains("patterns") &&
                prompt.Contains("Recommended actions")),
            "alert-analysis",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnalyzeAlertsAsync_RespectsCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        _mockChatService.SendMessageAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(new ChatCompletionResult { Success = true, Response = "Analysis" });

        // Act
        await _sut.AnalyzeAlertsAsync(cts.Token);

        // Assert
        await _mockChatService.Received(1).SendMessageAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            cts.Token);
    }

    #endregion
}
