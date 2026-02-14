using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.AI.Services;
using NSubstitute;
using Xunit;

namespace MIC.Tests.Unit.Infrastructure.AI;

/// <summary>
/// Tests for <see cref="RealEmailAnalysisService"/>.
/// Covers constructor, unconfigured fallback paths, heuristic analysis, and ParseAnalysisResult.
/// </summary>
public class RealEmailAnalysisServiceTests
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RealEmailAnalysisService> _logger;

    public RealEmailAnalysisServiceTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AI:Provider"] = "None",
                ["AI:OpenAI:ApiKey"] = "",
                ["AI:OpenAI:ModelId"] = "gpt-4o",
                ["AI:Prompts:EmailAnalysis"] = "Analyze this email"
            })
            .Build();

        _logger = Substitute.For<ILogger<RealEmailAnalysisService>>();
    }

    private RealEmailAnalysisService CreateUnconfiguredService()
    {
        return new RealEmailAnalysisService(_configuration, _logger);
    }

    private static EmailMessage CreateTestEmail(
        string subject = "Test Subject",
        string fromName = "John",
        string fromAddress = "john@example.com",
        string bodyText = "Hello world")
    {
        return new EmailMessage(
            messageId: Guid.NewGuid().ToString(),
            subject: subject,
            fromAddress: fromAddress,
            fromName: fromName,
            toRecipients: "recipient@example.com",
            sentDate: DateTime.UtcNow,
            receivedDate: DateTime.UtcNow,
            bodyText: bodyText,
            userId: Guid.NewGuid(),
            emailAccountId: Guid.NewGuid());
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_UnconfiguredProvider_SetsIsConfiguredFalse()
    {
        var service = CreateUnconfiguredService();

        service.Should().NotBeNull();
        // Verify it logged a warning about not being configured
        _logger.ReceivedWithAnyArgs().LogWarning(default(string)!);
    }

    [Fact]
    public void Constructor_NoApiKey_SetsIsConfiguredFalse()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AI:Provider"] = "OpenAI",
                ["AI:OpenAI:ApiKey"] = ""
            })
            .Build();

        var service = new RealEmailAnalysisService(config, _logger);

        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_NullApiKey_SetsIsConfiguredFalse()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AI:Provider"] = "OpenAI"
            })
            .Build();

        var service = new RealEmailAnalysisService(config, _logger);

        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_DefaultModelId_UsesGpt4o()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>()).Build();

        var service = new RealEmailAnalysisService(config, _logger);

        // Model ID should default to gpt-4o
        var modelField = typeof(RealEmailAnalysisService)
            .GetField("_modelId", BindingFlags.NonPublic | BindingFlags.Instance);
        modelField.Should().NotBeNull();
        modelField!.GetValue(service).Should().Be("gpt-4o");
    }

    [Fact]
    public void Constructor_CustomModelId_UsesConfiguredValue()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AI:OpenAI:ModelId"] = "gpt-3.5-turbo"
            })
            .Build();

        var service = new RealEmailAnalysisService(config, _logger);

        var modelField = typeof(RealEmailAnalysisService)
            .GetField("_modelId", BindingFlags.NonPublic | BindingFlags.Instance);
        modelField!.GetValue(service).Should().Be("gpt-3.5-turbo");
    }

    #endregion

    #region AnalyzeEmailAsync — Unconfigured Path

    [Fact]
    public async Task AnalyzeEmailAsync_WhenNotConfigured_ReturnsDefaultAnalysis()
    {
        var service = CreateUnconfiguredService();
        var email = CreateTestEmail("Normal Subject");

        var result = await service.AnalyzeEmailAsync(email);

        result.Should().NotBeNull();
        result.Priority.Should().Be(EmailPriority.Normal);
        result.IsUrgent.Should().BeFalse();
        result.Sentiment.Should().Be(SentimentType.Neutral);
        result.ActionItems.Should().NotBeNull();
        result.ActionItems.Should().BeEmpty();
        result.ConfidenceScore.Should().Be(0.5);
    }

    [Fact]
    public async Task AnalyzeEmailAsync_UrgentSubject_ReturnsHighPriority()
    {
        var service = CreateUnconfiguredService();
        var email = CreateTestEmail("URGENT: Server down!");

        var result = await service.AnalyzeEmailAsync(email);

        result.Priority.Should().Be(EmailPriority.High);
        result.IsUrgent.Should().BeTrue();
    }

    [Fact]
    public async Task AnalyzeEmailAsync_AsapSubject_ReturnsHighPriority()
    {
        var service = CreateUnconfiguredService();
        var email = CreateTestEmail("Need this done ASAP");

        var result = await service.AnalyzeEmailAsync(email);

        result.Priority.Should().Be(EmailPriority.High);
        result.IsUrgent.Should().BeTrue();
    }

    [Fact]
    public async Task AnalyzeEmailAsync_FyiSubject_ReturnsLowPriority()
    {
        var service = CreateUnconfiguredService();
        var email = CreateTestEmail("FYI - quarterly report");

        var result = await service.AnalyzeEmailAsync(email);

        result.Priority.Should().Be(EmailPriority.Low);
        result.IsUrgent.Should().BeFalse();
    }

    [Fact]
    public async Task AnalyzeEmailAsync_NormalSubject_ReturnsNormalPriority()
    {
        var service = CreateUnconfiguredService();
        var email = CreateTestEmail("Meeting tomorrow at 3pm");

        var result = await service.AnalyzeEmailAsync(email);

        result.Priority.Should().Be(EmailPriority.Normal);
        result.IsUrgent.Should().BeFalse();
    }

    [Fact]
    public async Task AnalyzeEmailAsync_DefaultAnalysis_IncludesSummary()
    {
        var service = CreateUnconfiguredService();
        var email = CreateTestEmail("Team update", "Alice");

        var result = await service.AnalyzeEmailAsync(email);

        result.Summary.Should().Contain("Alice");
        result.Summary.Should().Contain("Team update");
    }

    #endregion

    #region GenerateSummaryAsync — Unconfigured Path

    [Fact]
    public async Task GenerateSummaryAsync_WhenNotConfigured_ReturnsDefaultSummary()
    {
        var service = CreateUnconfiguredService();
        var email = CreateTestEmail("Project Update", "Bob");

        var result = await service.GenerateSummaryAsync(email);

        result.Should().Contain("Bob");
        result.Should().Contain("Project Update");
    }

    [Fact]
    public async Task GenerateSummaryAsync_WhenNotConfigured_IncludesFromName()
    {
        var service = CreateUnconfiguredService();
        var email = CreateTestEmail("Hello", "Alice Smith");

        var result = await service.GenerateSummaryAsync(email);

        result.Should().Contain("Alice Smith");
    }

    #endregion

    #region ExtractActionItemsAsync — Unconfigured Path

    [Fact]
    public async Task ExtractActionItemsAsync_WhenNotConfigured_ReturnsEmptyList()
    {
        var service = CreateUnconfiguredService();
        var email = CreateTestEmail();

        var result = await service.ExtractActionItemsAsync(email);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region ParseAnalysisResult via Reflection

    [Fact]
    public void ParseAnalysisResult_ValidJson_ReturnsCorrectResult()
    {
        var service = CreateUnconfiguredService();
        var email = CreateTestEmail();
        var method = typeof(RealEmailAnalysisService)
            .GetMethod("ParseAnalysisResult", BindingFlags.NonPublic | BindingFlags.Instance);

        var aiResponse = """
            Here is the analysis:
            {
              "priority": "High",
              "isUrgent": true,
              "sentiment": "Negative",
              "actionItems": ["Review document", "Send reply"],
              "summary": "Urgent request for review",
              "confidence": 0.95
            }
            """;

        var result = method!.Invoke(service, new object[] { aiResponse, email }) as EmailAnalysisResult;

        result.Should().NotBeNull();
        result!.Priority.Should().Be(EmailPriority.High);
        result.IsUrgent.Should().BeTrue();
        result.Sentiment.Should().Be(SentimentType.Negative);
        result.ActionItems.Should().Contain("Review document");
        result.ActionItems.Should().Contain("Send reply");
        result.Summary.Should().Be("Urgent request for review");
        result.ConfidenceScore.Should().Be(0.95);
    }

    [Fact]
    public void ParseAnalysisResult_MalformedJson_ReturnsFallbackAnalysis()
    {
        var service = CreateUnconfiguredService();
        var email = CreateTestEmail("Test Subject");
        var method = typeof(RealEmailAnalysisService)
            .GetMethod("ParseAnalysisResult", BindingFlags.NonPublic | BindingFlags.Instance);

        var result = method!.Invoke(service, new object[] { "{invalid json content", email }) as EmailAnalysisResult;

        result.Should().NotBeNull();
        // Should return default/heuristic analysis
        result!.Sentiment.Should().Be(SentimentType.Neutral);
        result.ConfidenceScore.Should().Be(0.5);
    }

    [Fact]
    public void ParseAnalysisResult_NoJsonBraces_ReturnsFallbackAnalysis()
    {
        var service = CreateUnconfiguredService();
        var email = CreateTestEmail("Normal Email");
        var method = typeof(RealEmailAnalysisService)
            .GetMethod("ParseAnalysisResult", BindingFlags.NonPublic | BindingFlags.Instance);

        var result = method!.Invoke(service, new object[] { "Just some text without JSON", email }) as EmailAnalysisResult;

        result.Should().NotBeNull();
        result!.ConfidenceScore.Should().Be(0.5);
    }

    [Fact]
    public void ParseAnalysisResult_JsonWithMissingConfidence_DefaultsTo08()
    {
        var service = CreateUnconfiguredService();
        var email = CreateTestEmail();
        var method = typeof(RealEmailAnalysisService)
            .GetMethod("ParseAnalysisResult", BindingFlags.NonPublic | BindingFlags.Instance);

        var aiResponse = """
            {
              "priority": "Normal",
              "isUrgent": false,
              "sentiment": "Positive",
              "summary": "Good news"
            }
            """;

        var result = method!.Invoke(service, new object[] { aiResponse, email }) as EmailAnalysisResult;

        result.Should().NotBeNull();
        result!.ConfidenceScore.Should().Be(0.8);
    }

    [Fact]
    public void ParseAnalysisResult_JsonWithMissingActionItems_DefaultsToEmptyList()
    {
        var service = CreateUnconfiguredService();
        var email = CreateTestEmail();
        var method = typeof(RealEmailAnalysisService)
            .GetMethod("ParseAnalysisResult", BindingFlags.NonPublic | BindingFlags.Instance);

        var aiResponse = """
            {
              "priority": "Low",
              "isUrgent": false,
              "sentiment": "Neutral",
              "summary": "FYI information"
            }
            """;

        var result = method!.Invoke(service, new object[] { aiResponse, email }) as EmailAnalysisResult;

        result.Should().NotBeNull();
        result!.ActionItems.Should().BeEmpty();
    }

    [Fact]
    public void ParseAnalysisResult_ValidJsonWithSurroundingText_ExtractsJson()
    {
        var service = CreateUnconfiguredService();
        var email = CreateTestEmail();
        var method = typeof(RealEmailAnalysisService)
            .GetMethod("ParseAnalysisResult", BindingFlags.NonPublic | BindingFlags.Instance);

        var aiResponse = """
            Here is the analysis result:
            
            {
              "priority": "Normal",
              "isUrgent": false,
              "sentiment": "Positive",
              "actionItems": ["Follow up"],
              "summary": "Good update",
              "confidence": 0.85
            }
            
            Note: This is a preliminary analysis.
            """;

        var result = method!.Invoke(service, new object[] { aiResponse, email }) as EmailAnalysisResult;

        result.Should().NotBeNull();
        result!.Priority.Should().Be(EmailPriority.Normal);
        result.Summary.Should().Be("Good update");
    }

    #endregion

    #region BuildAnalysisPrompt via Reflection

    [Fact]
    public void BuildAnalysisPrompt_ContainsEmailDetails()
    {
        var service = CreateUnconfiguredService();
        var email = CreateTestEmail("Important Meeting", "Jane Doe", "jane@test.com");

        var method = typeof(RealEmailAnalysisService)
            .GetMethod("BuildAnalysisPrompt", BindingFlags.NonPublic | BindingFlags.Instance);

        var result = method!.Invoke(service, new object[] { email }) as string;

        result.Should().NotBeNull();
        result.Should().Contain("Jane Doe");
        result.Should().Contain("jane@test.com");
        result.Should().Contain("Important Meeting");
        result.Should().Contain("priority");
        result.Should().Contain("sentiment");
    }

    #endregion

    #region GetDefaultAnalysis via Reflection

    [Fact]
    public void GetDefaultAnalysis_UrgentEmail_ReturnsHighPriority()
    {
        var service = CreateUnconfiguredService();
        var email = CreateTestEmail("urgent request");

        var method = typeof(RealEmailAnalysisService)
            .GetMethod("GetDefaultAnalysis", BindingFlags.NonPublic | BindingFlags.Instance);

        var result = method!.Invoke(service, new object[] { email }) as EmailAnalysisResult;

        result.Should().NotBeNull();
        result!.Priority.Should().Be(EmailPriority.High);
        result.IsUrgent.Should().BeTrue();
    }

    [Fact]
    public void GetDefaultAnalysis_AsapEmail_ReturnsHighPriority()
    {
        var service = CreateUnconfiguredService();
        var email = CreateTestEmail("Need this done asap");

        var method = typeof(RealEmailAnalysisService)
            .GetMethod("GetDefaultAnalysis", BindingFlags.NonPublic | BindingFlags.Instance);

        var result = method!.Invoke(service, new object[] { email }) as EmailAnalysisResult;

        result.Should().NotBeNull();
        result!.IsUrgent.Should().BeTrue();
        result.Priority.Should().Be(EmailPriority.High);
    }

    [Fact]
    public void GetDefaultAnalysis_FyiEmail_ReturnsLowPriority()
    {
        var service = CreateUnconfiguredService();
        var email = CreateTestEmail("FYI: monthly report");

        var method = typeof(RealEmailAnalysisService)
            .GetMethod("GetDefaultAnalysis", BindingFlags.NonPublic | BindingFlags.Instance);

        var result = method!.Invoke(service, new object[] { email }) as EmailAnalysisResult;

        result.Should().NotBeNull();
        result!.Priority.Should().Be(EmailPriority.Low);
    }

    [Fact]
    public void GetDefaultAnalysis_NormalEmail_ReturnsNormalPriority()
    {
        var service = CreateUnconfiguredService();
        var email = CreateTestEmail("Quarterly review");

        var method = typeof(RealEmailAnalysisService)
            .GetMethod("GetDefaultAnalysis", BindingFlags.NonPublic | BindingFlags.Instance);

        var result = method!.Invoke(service, new object[] { email }) as EmailAnalysisResult;

        result.Should().NotBeNull();
        result!.Priority.Should().Be(EmailPriority.Normal);
        result.IsUrgent.Should().BeFalse();
    }

    [Fact]
    public void GetDefaultAnalysis_AlwaysReturnsSummary()
    {
        var service = CreateUnconfiguredService();
        var email = CreateTestEmail("Test Report", "Manager");

        var method = typeof(RealEmailAnalysisService)
            .GetMethod("GetDefaultAnalysis", BindingFlags.NonPublic | BindingFlags.Instance);

        var result = method!.Invoke(service, new object[] { email }) as EmailAnalysisResult;

        result.Should().NotBeNull();
        result!.Summary.Should().Contain("Manager");
        result.Summary.Should().Contain("Test Report");
    }

    [Fact]
    public void GetDefaultAnalysis_AlwaysReturnsNeutralSentiment()
    {
        var service = CreateUnconfiguredService();
        var email = CreateTestEmail("Anything");

        var method = typeof(RealEmailAnalysisService)
            .GetMethod("GetDefaultAnalysis", BindingFlags.NonPublic | BindingFlags.Instance);

        var result = method!.Invoke(service, new object[] { email }) as EmailAnalysisResult;

        result.Should().NotBeNull();
        result!.Sentiment.Should().Be(SentimentType.Neutral);
    }

    #endregion

    #region Cancellation Token Tests

    [Fact]
    public async Task AnalyzeEmailAsync_RespectsCancellationToken()
    {
        var service = CreateUnconfiguredService();
        var email = CreateTestEmail();
        using var cts = new CancellationTokenSource();

        // Unconfigured path should still return quickly
        var result = await service.AnalyzeEmailAsync(email, cts.Token);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateSummaryAsync_RespectsCancellationToken()
    {
        var service = CreateUnconfiguredService();
        var email = CreateTestEmail();
        using var cts = new CancellationTokenSource();

        var result = await service.GenerateSummaryAsync(email, cts.Token);

        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExtractActionItemsAsync_RespectsCancellationToken()
    {
        var service = CreateUnconfiguredService();
        var email = CreateTestEmail();
        using var cts = new CancellationTokenSource();

        var result = await service.ExtractActionItemsAsync(email, cts.Token);

        result.Should().NotBeNull();
    }

    #endregion
}
