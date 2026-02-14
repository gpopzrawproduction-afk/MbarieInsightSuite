using FluentAssertions;
using MIC.Infrastructure.AI.Models;

namespace MIC.Tests.Unit.Infrastructure.AI;

/// <summary>
/// Tests for ChatModels.cs: ChatMessage, ChatRole, AIInsight,
/// InsightType, InsightSeverity, ChatCompletionResult, TokenUsage.
/// </summary>
public class ChatModelsTests
{
    #region ChatMessage

    [Fact]
    public void ChatMessage_Defaults_IdIsNotEmpty()
    {
        var msg = new ChatMessage();
        msg.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void ChatMessage_TwoInstances_HaveDifferentIds()
    {
        var a = new ChatMessage();
        var b = new ChatMessage();
        a.Id.Should().NotBe(b.Id);
    }

    [Fact]
    public void ChatMessage_Defaults_RoleIsSystem()
    {
        new ChatMessage().Role.Should().Be(ChatRole.System);
    }

    [Fact]
    public void ChatMessage_Defaults_ContentIsEmpty()
    {
        new ChatMessage().Content.Should().BeEmpty();
    }

    [Fact]
    public void ChatMessage_Defaults_TimestampIsRecent()
    {
        var msg = new ChatMessage();
        msg.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ChatMessage_Defaults_MetadataIsNull()
    {
        new ChatMessage().Metadata.Should().BeNull();
    }

    [Fact]
    public void ChatMessage_WithInit_SetsAllProperties()
    {
        var id = Guid.NewGuid();
        var ts = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var meta = new Dictionary<string, string> { ["key"] = "value" };
        var msg = new ChatMessage
        {
            Id = id,
            Role = ChatRole.User,
            Content = "Hello",
            Timestamp = ts,
            Metadata = meta
        };

        msg.Id.Should().Be(id);
        msg.Role.Should().Be(ChatRole.User);
        msg.Content.Should().Be("Hello");
        msg.Timestamp.Should().Be(ts);
        msg.Metadata.Should().ContainKey("key");
    }

    [Fact]
    public void ChatMessage_IsRecord_SupportsEquality()
    {
        var id = Guid.NewGuid();
        var ts = DateTime.UtcNow;
        var a = new ChatMessage { Id = id, Role = ChatRole.User, Content = "test", Timestamp = ts };
        var b = new ChatMessage { Id = id, Role = ChatRole.User, Content = "test", Timestamp = ts };
        a.Should().Be(b);
    }

    [Fact]
    public void ChatMessage_IsRecord_SupportsWithExpression()
    {
        var msg = new ChatMessage { Role = ChatRole.User, Content = "original" };
        var modified = msg with { Content = "modified" };
        modified.Content.Should().Be("modified");
        modified.Role.Should().Be(ChatRole.User);
        modified.Id.Should().Be(msg.Id);
    }

    #endregion

    #region ChatRole

    [Fact]
    public void ChatRole_HasSystemValue()
    {
        ChatRole.System.Should().Be((ChatRole)0);
    }

    [Fact]
    public void ChatRole_HasUserValue()
    {
        ChatRole.User.Should().Be((ChatRole)1);
    }

    [Fact]
    public void ChatRole_HasAssistantValue()
    {
        ChatRole.Assistant.Should().Be((ChatRole)2);
    }

    [Fact]
    public void ChatRole_AllValues()
    {
        var values = Enum.GetValues<ChatRole>();
        values.Should().HaveCount(3);
    }

    #endregion

    #region AIInsight

    [Fact]
    public void AIInsight_Defaults_TypeIsTrend()
    {
        new AIInsight().Type.Should().Be(InsightType.Trend);
    }

    [Fact]
    public void AIInsight_Defaults_TitleIsEmpty()
    {
        new AIInsight().Title.Should().BeEmpty();
    }

    [Fact]
    public void AIInsight_Defaults_DescriptionIsEmpty()
    {
        new AIInsight().Description.Should().BeEmpty();
    }

    [Fact]
    public void AIInsight_Defaults_SeverityIsInfo()
    {
        new AIInsight().Severity.Should().Be(InsightSeverity.Info);
    }

    [Fact]
    public void AIInsight_Defaults_RecommendationsIsEmpty()
    {
        new AIInsight().Recommendations.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void AIInsight_Defaults_RelatedEntityIdsIsEmpty()
    {
        new AIInsight().RelatedEntityIds.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void AIInsight_Defaults_ConfidenceIsZero()
    {
        new AIInsight().Confidence.Should().Be(0);
    }

    [Fact]
    public void AIInsight_Defaults_GeneratedAtIsRecent()
    {
        new AIInsight().GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AIInsight_WithInit_SetsAllProperties()
    {
        var entityId = Guid.NewGuid();
        var insight = new AIInsight
        {
            Type = InsightType.Anomaly,
            Title = "Revenue Spike",
            Description = "Revenue increased 50%",
            Severity = InsightSeverity.Warning,
            Recommendations = new List<string> { "Investigate", "Report" },
            RelatedEntityIds = new List<Guid> { entityId },
            Confidence = 0.95,
            GeneratedAt = new DateTime(2025, 1, 1)
        };

        insight.Type.Should().Be(InsightType.Anomaly);
        insight.Title.Should().Be("Revenue Spike");
        insight.Description.Should().Be("Revenue increased 50%");
        insight.Severity.Should().Be(InsightSeverity.Warning);
        insight.Recommendations.Should().HaveCount(2);
        insight.RelatedEntityIds.Should().Contain(entityId);
        insight.Confidence.Should().Be(0.95);
        insight.GeneratedAt.Should().Be(new DateTime(2025, 1, 1));
    }

    [Fact]
    public void AIInsight_IsRecord_SupportsWithExpression()
    {
        var insight = new AIInsight { Title = "Original" };
        var modified = insight with { Title = "Modified" };
        modified.Title.Should().Be("Modified");
    }

    #endregion

    #region InsightType

    [Theory]
    [InlineData(InsightType.Trend, 0)]
    [InlineData(InsightType.Anomaly, 1)]
    [InlineData(InsightType.Recommendation, 2)]
    [InlineData(InsightType.Prediction, 3)]
    [InlineData(InsightType.Summary, 4)]
    [InlineData(InsightType.Alert, 5)]
    [InlineData(InsightType.Correlation, 6)]
    public void InsightType_HasExpectedValues(InsightType type, int expected)
    {
        ((int)type).Should().Be(expected);
    }

    [Fact]
    public void InsightType_HasSevenValues()
    {
        Enum.GetValues<InsightType>().Should().HaveCount(7);
    }

    #endregion

    #region InsightSeverity

    [Theory]
    [InlineData(InsightSeverity.Info, 0)]
    [InlineData(InsightSeverity.Success, 1)]
    [InlineData(InsightSeverity.Warning, 2)]
    [InlineData(InsightSeverity.Critical, 3)]
    public void InsightSeverity_HasExpectedValues(InsightSeverity severity, int expected)
    {
        ((int)severity).Should().Be(expected);
    }

    [Fact]
    public void InsightSeverity_HasFourValues()
    {
        Enum.GetValues<InsightSeverity>().Should().HaveCount(4);
    }

    #endregion

    #region ChatCompletionResult

    [Fact]
    public void ChatCompletionResult_Defaults_SuccessIsFalse()
    {
        new ChatCompletionResult().Success.Should().BeFalse();
    }

    [Fact]
    public void ChatCompletionResult_Defaults_ResponseIsEmpty()
    {
        new ChatCompletionResult().Response.Should().BeEmpty();
    }

    [Fact]
    public void ChatCompletionResult_Defaults_ErrorIsNull()
    {
        new ChatCompletionResult().Error.Should().BeNull();
    }

    [Fact]
    public void ChatCompletionResult_Defaults_UsageIsNull()
    {
        new ChatCompletionResult().Usage.Should().BeNull();
    }

    [Fact]
    public void ChatCompletionResult_Defaults_DurationIsZero()
    {
        new ChatCompletionResult().Duration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void ChatCompletionResult_SuccessResult()
    {
        var result = new ChatCompletionResult
        {
            Success = true,
            Response = "Hello from AI",
            Duration = TimeSpan.FromSeconds(1.5),
            Usage = new TokenUsage { PromptTokens = 10, CompletionTokens = 20 }
        };

        result.Success.Should().BeTrue();
        result.Response.Should().Be("Hello from AI");
        result.Duration.Should().Be(TimeSpan.FromSeconds(1.5));
        result.Usage.Should().NotBeNull();
    }

    [Fact]
    public void ChatCompletionResult_ErrorResult()
    {
        var result = new ChatCompletionResult
        {
            Success = false,
            Error = "Rate limit exceeded",
            Duration = TimeSpan.FromMilliseconds(100)
        };

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Rate limit exceeded");
    }

    [Fact]
    public void ChatCompletionResult_IsRecord_SupportsWithExpression()
    {
        var result = new ChatCompletionResult { Success = false };
        var modified = result with { Success = true, Response = "OK" };
        modified.Success.Should().BeTrue();
        modified.Response.Should().Be("OK");
    }

    #endregion

    #region TokenUsage

    [Fact]
    public void TokenUsage_Defaults_PromptTokensIsZero()
    {
        new TokenUsage().PromptTokens.Should().Be(0);
    }

    [Fact]
    public void TokenUsage_Defaults_CompletionTokensIsZero()
    {
        new TokenUsage().CompletionTokens.Should().Be(0);
    }

    [Fact]
    public void TokenUsage_TotalTokens_IsSumOfPromptAndCompletion()
    {
        var usage = new TokenUsage { PromptTokens = 100, CompletionTokens = 50 };
        usage.TotalTokens.Should().Be(150);
    }

    [Fact]
    public void TokenUsage_TotalTokens_IsZeroWhenBothZero()
    {
        new TokenUsage().TotalTokens.Should().Be(0);
    }

    [Fact]
    public void TokenUsage_IsRecord_SupportsEquality()
    {
        var a = new TokenUsage { PromptTokens = 10, CompletionTokens = 20 };
        var b = new TokenUsage { PromptTokens = 10, CompletionTokens = 20 };
        a.Should().Be(b);
    }

    [Fact]
    public void TokenUsage_IsRecord_SupportsWithExpression()
    {
        var usage = new TokenUsage { PromptTokens = 10, CompletionTokens = 20 };
        var modified = usage with { PromptTokens = 100 };
        modified.PromptTokens.Should().Be(100);
        modified.CompletionTokens.Should().Be(20);
        modified.TotalTokens.Should().Be(120);
    }

    [Fact]
    public void TokenUsage_LargeValues_ComputeCorrectly()
    {
        var usage = new TokenUsage { PromptTokens = 100_000, CompletionTokens = 50_000 };
        usage.TotalTokens.Should().Be(150_000);
    }

    #endregion
}
