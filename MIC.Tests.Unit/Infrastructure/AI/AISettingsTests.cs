using FluentAssertions;
using MIC.Infrastructure.AI.Configuration;

namespace MIC.Tests.Unit.Infrastructure.AI;

/// <summary>
/// Tests for AI configuration POCOs: AISettings, OpenAISettings,
/// AzureOpenAISettings, AIFeatureFlags, SystemPromptSettings.
/// </summary>
public class AISettingsTests
{
    #region AISettings

    [Fact]
    public void AISettings_SectionName_IsAI()
    {
        AISettings.SectionName.Should().Be("AI");
    }

    [Fact]
    public void AISettings_Defaults_ProviderIsOpenAI()
    {
        var settings = new AISettings();
        settings.Provider.Should().Be("OpenAI");
    }

    [Fact]
    public void AISettings_Defaults_HasOpenAISettings()
    {
        var settings = new AISettings();
        settings.OpenAI.Should().NotBeNull();
    }

    [Fact]
    public void AISettings_Defaults_HasAzureOpenAISettings()
    {
        var settings = new AISettings();
        settings.AzureOpenAI.Should().NotBeNull();
    }

    [Fact]
    public void AISettings_Defaults_HasFeatureFlags()
    {
        var settings = new AISettings();
        settings.Features.Should().NotBeNull();
    }

    [Fact]
    public void AISettings_Defaults_HasSystemPrompt()
    {
        var settings = new AISettings();
        settings.SystemPrompt.Should().NotBeNull();
    }

    [Fact]
    public void AISettings_Provider_CanBeSet()
    {
        var settings = new AISettings { Provider = "AzureOpenAI" };
        settings.Provider.Should().Be("AzureOpenAI");
    }

    [Fact]
    public void AISettings_OpenAI_CanBeReplaced()
    {
        var custom = new OpenAISettings { ApiKey = "sk-test" };
        var settings = new AISettings { OpenAI = custom };
        settings.OpenAI.ApiKey.Should().Be("sk-test");
    }

    [Fact]
    public void AISettings_AzureOpenAI_CanBeReplaced()
    {
        var custom = new AzureOpenAISettings { Endpoint = "https://test.openai.azure.com" };
        var settings = new AISettings { AzureOpenAI = custom };
        settings.AzureOpenAI.Endpoint.Should().Be("https://test.openai.azure.com");
    }

    [Fact]
    public void AISettings_Features_CanBeReplaced()
    {
        var flags = new AIFeatureFlags { ChatEnabled = false };
        var settings = new AISettings { Features = flags };
        settings.Features.ChatEnabled.Should().BeFalse();
    }

    [Fact]
    public void AISettings_SystemPrompt_CanBeReplaced()
    {
        var prompt = new SystemPromptSettings { BusinessName = "TestBiz" };
        var settings = new AISettings { SystemPrompt = prompt };
        settings.SystemPrompt.BusinessName.Should().Be("TestBiz");
    }

    #endregion

    #region OpenAISettings

    [Fact]
    public void OpenAISettings_Defaults_ApiKeyEmpty()
    {
        new OpenAISettings().ApiKey.Should().BeEmpty();
    }

    [Fact]
    public void OpenAISettings_Defaults_ModelIsGpt4o()
    {
        new OpenAISettings().Model.Should().Be("gpt-4o");
    }

    [Fact]
    public void OpenAISettings_Defaults_EmbeddingModel()
    {
        new OpenAISettings().EmbeddingModel.Should().Be("text-embedding-3-small");
    }

    [Fact]
    public void OpenAISettings_Defaults_Temperature07()
    {
        new OpenAISettings().Temperature.Should().Be(0.7);
    }

    [Fact]
    public void OpenAISettings_Defaults_MaxTokens2000()
    {
        new OpenAISettings().MaxTokens.Should().Be(2000);
    }

    [Fact]
    public void OpenAISettings_Defaults_OrganizationIdNull()
    {
        new OpenAISettings().OrganizationId.Should().BeNull();
    }

    [Fact]
    public void OpenAISettings_AllProperties_Settable()
    {
        var s = new OpenAISettings
        {
            ApiKey = "sk-abc",
            Model = "gpt-3.5-turbo",
            EmbeddingModel = "text-embedding-ada-002",
            Temperature = 1.0,
            MaxTokens = 4000,
            OrganizationId = "org-xyz"
        };

        s.ApiKey.Should().Be("sk-abc");
        s.Model.Should().Be("gpt-3.5-turbo");
        s.EmbeddingModel.Should().Be("text-embedding-ada-002");
        s.Temperature.Should().Be(1.0);
        s.MaxTokens.Should().Be(4000);
        s.OrganizationId.Should().Be("org-xyz");
    }

    #endregion

    #region AzureOpenAISettings

    [Fact]
    public void AzureOpenAISettings_Defaults_EndpointEmpty()
    {
        new AzureOpenAISettings().Endpoint.Should().BeEmpty();
    }

    [Fact]
    public void AzureOpenAISettings_Defaults_ApiKeyEmpty()
    {
        new AzureOpenAISettings().ApiKey.Should().BeEmpty();
    }

    [Fact]
    public void AzureOpenAISettings_Defaults_ChatDeploymentNameEmpty()
    {
        new AzureOpenAISettings().ChatDeploymentName.Should().BeEmpty();
    }

    [Fact]
    public void AzureOpenAISettings_Defaults_EmbeddingDeploymentNameEmpty()
    {
        new AzureOpenAISettings().EmbeddingDeploymentName.Should().BeEmpty();
    }

    [Fact]
    public void AzureOpenAISettings_Defaults_Temperature07()
    {
        new AzureOpenAISettings().Temperature.Should().Be(0.7);
    }

    [Fact]
    public void AzureOpenAISettings_Defaults_MaxTokens2000()
    {
        new AzureOpenAISettings().MaxTokens.Should().Be(2000);
    }

    [Fact]
    public void AzureOpenAISettings_AllProperties_Settable()
    {
        var s = new AzureOpenAISettings
        {
            Endpoint = "https://myresource.openai.azure.com",
            ApiKey = "azure-key",
            ChatDeploymentName = "gpt4-deployment",
            EmbeddingDeploymentName = "embed-deployment",
            Temperature = 0.3,
            MaxTokens = 500
        };

        s.Endpoint.Should().Be("https://myresource.openai.azure.com");
        s.ApiKey.Should().Be("azure-key");
        s.ChatDeploymentName.Should().Be("gpt4-deployment");
        s.EmbeddingDeploymentName.Should().Be("embed-deployment");
        s.Temperature.Should().Be(0.3);
        s.MaxTokens.Should().Be(500);
    }

    #endregion

    #region AIFeatureFlags

    [Fact]
    public void AIFeatureFlags_Defaults_ChatEnabled()
    {
        new AIFeatureFlags().ChatEnabled.Should().BeTrue();
    }

    [Fact]
    public void AIFeatureFlags_Defaults_EmailIntelligenceDisabled()
    {
        new AIFeatureFlags().EmailIntelligenceEnabled.Should().BeFalse();
    }

    [Fact]
    public void AIFeatureFlags_Defaults_PredictionsEnabled()
    {
        new AIFeatureFlags().PredictionsEnabled.Should().BeTrue();
    }

    [Fact]
    public void AIFeatureFlags_Defaults_InsightsEnabled()
    {
        new AIFeatureFlags().InsightsEnabled.Should().BeTrue();
    }

    [Fact]
    public void AIFeatureFlags_Defaults_VoiceDisabled()
    {
        new AIFeatureFlags().VoiceEnabled.Should().BeFalse();
    }

    [Fact]
    public void AIFeatureFlags_Defaults_AnomalyDetectionEnabled()
    {
        new AIFeatureFlags().AnomalyDetectionEnabled.Should().BeTrue();
    }

    [Fact]
    public void AIFeatureFlags_AllProperties_Settable()
    {
        var f = new AIFeatureFlags
        {
            ChatEnabled = false,
            EmailIntelligenceEnabled = true,
            PredictionsEnabled = false,
            InsightsEnabled = false,
            VoiceEnabled = true,
            AnomalyDetectionEnabled = false
        };

        f.ChatEnabled.Should().BeFalse();
        f.EmailIntelligenceEnabled.Should().BeTrue();
        f.PredictionsEnabled.Should().BeFalse();
        f.InsightsEnabled.Should().BeFalse();
        f.VoiceEnabled.Should().BeTrue();
        f.AnomalyDetectionEnabled.Should().BeFalse();
    }

    #endregion

    #region SystemPromptSettings

    [Fact]
    public void SystemPromptSettings_Defaults_BusinessName()
    {
        new SystemPromptSettings().BusinessName.Should().Be("Mbarie Intelligence Console");
    }

    [Fact]
    public void SystemPromptSettings_Defaults_CustomPromptNull()
    {
        new SystemPromptSettings().CustomPrompt.Should().BeNull();
    }

    [Fact]
    public void SystemPromptSettings_Defaults_IncludeMetricsContextTrue()
    {
        new SystemPromptSettings().IncludeMetricsContext.Should().BeTrue();
    }

    [Fact]
    public void SystemPromptSettings_Defaults_IncludeAlertsContextTrue()
    {
        new SystemPromptSettings().IncludeAlertsContext.Should().BeTrue();
    }

    [Fact]
    public void SystemPromptSettings_Defaults_MaxAlertsInContext5()
    {
        new SystemPromptSettings().MaxAlertsInContext.Should().Be(5);
    }

    [Fact]
    public void SystemPromptSettings_AllProperties_Settable()
    {
        var s = new SystemPromptSettings
        {
            BusinessName = "Acme Corp",
            CustomPrompt = "You are helpful.",
            IncludeMetricsContext = false,
            IncludeAlertsContext = false,
            MaxAlertsInContext = 10
        };

        s.BusinessName.Should().Be("Acme Corp");
        s.CustomPrompt.Should().Be("You are helpful.");
        s.IncludeMetricsContext.Should().BeFalse();
        s.IncludeAlertsContext.Should().BeFalse();
        s.MaxAlertsInContext.Should().Be(10);
    }

    #endregion
}
