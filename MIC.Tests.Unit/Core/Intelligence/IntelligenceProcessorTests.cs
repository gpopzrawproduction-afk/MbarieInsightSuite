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
}
