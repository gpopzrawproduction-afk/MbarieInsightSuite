using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MIC.Core.Intelligence;

namespace MIC.Tests.Unit.Core.Intelligence;

public sealed class IntelligenceProcessorTests
{
    private readonly IntelligenceProcessor _sut = new();

    [Theory]
    [InlineData("Engineer", "organize technical discussions")]
    [InlineData("Workshop Manager", "equipment maintenance schedules")]
    [InlineData("Logistics Coordinator", "Optimize logistics planning")]
    public async Task GenerateEfficiencyRecommendationsAsync_ReturnsRoleSpecificGuidance(string role, string expectedFragment)
    {
        var recommendations = await _sut.GenerateEfficiencyRecommendationsAsync(
            department: "Operations",
            position: role,
            emailData: new[] { "Sample email body" });

        recommendations.Should().NotBeEmpty();
        recommendations.Should().Contain(line => line.IndexOf(expectedFragment, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    [Fact]
    public async Task GenerateEfficiencyRecommendationsAsync_DefaultsToGenericWhenRoleUnknown()
    {
        var recommendations = await _sut.GenerateEfficiencyRecommendationsAsync(
            department: "Analytics",
            position: "Data Scientist",
            emailData: Enumerable.Empty<string>());

        recommendations.Should().HaveCount(3);
        recommendations.First().Should().ContainEquivalentOf("Data Scientist");
    }

    [Theory]
    [InlineData("Workshop Manager", "Schedule equipment updates during low-email periods")]
    [InlineData("Project Manager", "Use email templates for status update requests")]
    [InlineData("Operations Manager", "Implement daily operational brief via email")]
    public async Task AnalyzeCommunicationPatternsAsync_AddsRoleSpecificActions(string role, string expectedAction)
    {
        var analysis = await _sut.AnalyzeCommunicationPatternsAsync(role, emailVolume: 42);

        analysis.Position.Should().Be(role);
        analysis.EmailVolume.Should().Be(42);
        analysis.PeakCommunicationTimes.Should().NotBeEmpty();
        analysis.RecommendedActions.Should().Contain(action => action.IndexOf(expectedAction, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    // ──────────────────────────────────────────────────────────────
    // Safety Coordinator — efficiency recommendations
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateEfficiencyRecommendationsAsync_SafetyCoordinator_ReturnsSafetyGuidance()
    {
        var recommendations = await _sut.GenerateEfficiencyRecommendationsAsync(
            department: "Safety", position: "Safety Coordinator", emailData: Enumerable.Empty<string>());

        recommendations.Should().HaveCount(3);
        recommendations.Should().Contain(r => r.IndexOf("safety compliance", StringComparison.OrdinalIgnoreCase) >= 0);
        recommendations.Should().Contain(r => r.IndexOf("incident reports", StringComparison.OrdinalIgnoreCase) >= 0);
        recommendations.Should().Contain(r => r.IndexOf("safety briefing", StringComparison.OrdinalIgnoreCase) >= 0);
    }

    // ──────────────────────────────────────────────────────────────
    // AnalyzeCommunicationPatternsAsync — default/unknown role
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AnalyzeCommunicationPatternsAsync_UnknownRole_ReturnsEmptyRecommendedActions()
    {
        var analysis = await _sut.AnalyzeCommunicationPatternsAsync("Data Analyst", emailVolume: 100);

        analysis.Position.Should().Be("Data Analyst");
        analysis.EmailVolume.Should().Be(100);
        analysis.PeakCommunicationTimes.Should().HaveCount(2);
        analysis.ResponseTimeAverageHours.Should().Be(2.5);
        analysis.RecommendedActions.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeCommunicationPatternsAsync_ZeroVolume_StillReturnsValidAnalysis()
    {
        var analysis = await _sut.AnalyzeCommunicationPatternsAsync("Workshop Manager", emailVolume: 0);

        analysis.EmailVolume.Should().Be(0);
        analysis.PeakCommunicationTimes.Should().NotBeEmpty();
        analysis.RecommendedActions.Should().NotBeEmpty();
    }

    // ──────────────────────────────────────────────────────────────
    // CommunicationAnalysis default state
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void CommunicationAnalysis_DefaultValues()
    {
        var analysis = new CommunicationAnalysis();

        analysis.Position.Should().BeEmpty();
        analysis.EmailVolume.Should().Be(0);
        analysis.PeakCommunicationTimes.Should().NotBeNull().And.BeEmpty();
        analysis.ResponseTimeAverageHours.Should().Be(0);
        analysis.RecommendedActions.Should().NotBeNull().And.BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────
    // Recommendations always returns exactly 3 items per known role
    // ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Workshop Manager")]
    [InlineData("Engineer")]
    [InlineData("Logistics Coordinator")]
    [InlineData("Safety Coordinator")]
    [InlineData("Random Unknown Position")]
    public async Task GenerateEfficiencyRecommendationsAsync_AlwaysReturnsThreeItems(string position)
    {
        var recommendations = await _sut.GenerateEfficiencyRecommendationsAsync(
            department: "Any", position: position, emailData: Enumerable.Empty<string>());

        recommendations.Should().HaveCount(3);
    }

    [Fact]
    public async Task GenerateEfficiencyRecommendationsAsync_DefaultPosition_IncludesPositionInFirstRecommendation()
    {
        var recommendations = await _sut.GenerateEfficiencyRecommendationsAsync(
            department: "IT", position: "Software Developer", emailData: new[] { "test" });

        recommendations.First().Should().Contain("Software Developer");
    }
}
