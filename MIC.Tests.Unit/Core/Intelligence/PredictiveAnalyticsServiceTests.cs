using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.TextGeneration;
using MIC.Core.Intelligence.Predictions;
using NSubstitute;

namespace MIC.Tests.Unit.Core.Intelligence;

public sealed class PredictiveAnalyticsServiceTests
{
    private readonly ILogger<PredictiveAnalyticsService> _logger =
        NullLoggerFactory.Instance.CreateLogger<PredictiveAnalyticsService>();

    /// <summary>
    /// Creates a Kernel with no AI services so InvokePromptAsync throws.
    /// </summary>
    private static Kernel CreateBrokenKernel()
    {
        return Kernel.CreateBuilder().Build();
    }

    /// <summary>
    /// Creates a Kernel with a mock text generation service that returns dummy text.
    /// </summary>
    private static Kernel CreateWorkingKernel()
    {
        var textGen = Substitute.For<ITextGenerationService>();
        textGen.GetTextContentsAsync(Arg.Any<string>(), Arg.Any<PromptExecutionSettings?>(),
                Arg.Any<Kernel?>(), Arg.Any<CancellationToken>())
            .Returns(new List<TextContent> { new("dummy AI response") });

        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton(textGen);
        return builder.Build();
    }

    // ──────────────────────────────────────────────────────────────
    // Interface
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void ImplementsIPredictiveAnalyticsService()
    {
        var sut = new PredictiveAnalyticsService(CreateBrokenKernel(), _logger);

        sut.Should().BeAssignableTo<IPredictiveAnalyticsService>();
    }

    // ──────────────────────────────────────────────────────────────
    // Error paths — Kernel throws → methods return null predictions
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AnalyzeEmailTrendsAsync_KernelThrows_ReturnsNull()
    {
        var sut = new PredictiveAnalyticsService(CreateBrokenKernel(), _logger);

        var result = await sut.AnalyzeEmailTrendsAsync(Guid.NewGuid(), 30);

        result.Should().BeNull();
    }

    [Fact]
    public async Task AnalyzeAlertPatternsAsync_KernelThrows_ReturnsNull()
    {
        var sut = new PredictiveAnalyticsService(CreateBrokenKernel(), _logger);

        var result = await sut.AnalyzeAlertPatternsAsync(Guid.NewGuid(), 30);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ForecastMetricAnomaliesAsync_KernelThrows_ReturnsNull()
    {
        var sut = new PredictiveAnalyticsService(CreateBrokenKernel(), _logger);

        var result = await sut.ForecastMetricAnomaliesAsync(Guid.NewGuid(), 30);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GeneratePredictionsAsync_AllSubMethodsFail_ReturnsEmptyList()
    {
        var sut = new PredictiveAnalyticsService(CreateBrokenKernel(), _logger);

        var results = await sut.GeneratePredictionsAsync(Guid.NewGuid(), 30);

        // All three sub-methods return null, filtered out
        results.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────
    // Happy paths — working Kernel
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AnalyzeEmailTrendsAsync_WorkingKernel_ReturnsPrediction()
    {
        var sut = new PredictiveAnalyticsService(CreateWorkingKernel(), _logger);
        var userId = Guid.NewGuid();

        var result = await sut.AnalyzeEmailTrendsAsync(userId, 14);

        result.Should().NotBeNull();
        result!.UserId.Should().Be(userId);
        result.Category.Should().Be("Email");
        result.Type.Should().Be(MIC.Core.Domain.Predictions.PredictionType.EmailVolumeIncrease);
        result.Confidence.Should().BeGreaterThan(0);
        result.TimeHorizonDays.Should().Be(14);
        result.Status.Should().Be(MIC.Core.Domain.Predictions.PredictionStatus.Active);
    }

    [Fact]
    public async Task AnalyzeAlertPatternsAsync_WorkingKernel_ReturnsPrediction()
    {
        var sut = new PredictiveAnalyticsService(CreateWorkingKernel(), _logger);
        var userId = Guid.NewGuid();

        var result = await sut.AnalyzeAlertPatternsAsync(userId, 7);

        result.Should().NotBeNull();
        result!.UserId.Should().Be(userId);
        result.Category.Should().Be("Alerts");
        result.Type.Should().Be(MIC.Core.Domain.Predictions.PredictionType.HighPriorityAlertSpike);
        result.TimeHorizonDays.Should().Be(7);
    }

    [Fact]
    public async Task ForecastMetricAnomaliesAsync_WorkingKernel_ReturnsPrediction()
    {
        var sut = new PredictiveAnalyticsService(CreateWorkingKernel(), _logger);
        var userId = Guid.NewGuid();

        var result = await sut.ForecastMetricAnomaliesAsync(userId, 90);

        result.Should().NotBeNull();
        result!.UserId.Should().Be(userId);
        result.Category.Should().Be("Metrics");
        result.Type.Should().Be(MIC.Core.Domain.Predictions.PredictionType.MetricAnomaly);
        result.TimeHorizonDays.Should().Be(90);
    }

    [Fact]
    public async Task GeneratePredictionsAsync_WorkingKernel_ReturnsThreePredictions()
    {
        var sut = new PredictiveAnalyticsService(CreateWorkingKernel(), _logger);

        var results = await sut.GeneratePredictionsAsync(Guid.NewGuid(), 30);

        results.Should().HaveCount(3);
        results.Should().Contain(p => p.Category == "Email");
        results.Should().Contain(p => p.Category == "Alerts");
        results.Should().Contain(p => p.Category == "Metrics");
    }

    [Fact]
    public async Task GeneratePredictionsAsync_DefaultTimeHorizon_Uses30Days()
    {
        var sut = new PredictiveAnalyticsService(CreateWorkingKernel(), _logger);

        var results = await sut.GeneratePredictionsAsync(Guid.NewGuid());

        results.Should().NotBeEmpty();
        results.Should().OnlyContain(p => p.TimeHorizonDays == 30);
    }
}
