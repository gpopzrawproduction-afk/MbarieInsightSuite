using System;
using System.Linq;
using FluentAssertions;
using MIC.Core.Domain.Entities;
using Xunit;

namespace MIC.Tests.Unit.Domain;

/// <summary>
/// Tests for DecisionContext entity covering domain logic + events.
/// </summary>
public class DecisionContextTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        var deadline = DateTime.UtcNow.AddDays(7);
        var context = new DecisionContext("Budget Review", "Annual budget", "John", deadline, DecisionPriority.High);

        context.ContextName.Should().Be("Budget Review");
        context.Description.Should().Be("Annual budget");
        context.DecisionMaker.Should().Be("John");
        context.Deadline.Should().Be(deadline);
        context.Priority.Should().Be(DecisionPriority.High);
        context.Status.Should().Be(DecisionStatus.Pending);
        context.ContextData.Should().BeEmpty();
        context.ConsideredOptions.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_DefaultPriority_IsMedium()
    {
        var context = new DecisionContext("Test", "Desc", "User", DateTime.UtcNow.AddDays(1));

        context.Priority.Should().Be(DecisionPriority.Medium);
    }

    [Fact]
    public void Constructor_ThrowsOnNullContextName()
    {
        var act = () => new DecisionContext(null!, "Desc", "User", DateTime.UtcNow.AddDays(1));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ThrowsOnWhitespaceDescription()
    {
        var act = () => new DecisionContext("Name", "  ", "User", DateTime.UtcNow.AddDays(1));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ThrowsOnNullDecisionMaker()
    {
        var act = () => new DecisionContext("Name", "Desc", null!, DateTime.UtcNow.AddDays(1));
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region AddOption Tests

    [Fact]
    public void AddOption_AddsToConsideredOptions()
    {
        var context = new DecisionContext("Test", "Desc", "User", DateTime.UtcNow.AddDays(1));

        context.AddOption("Option A");
        context.AddOption("Option B");

        context.ConsideredOptions.Should().HaveCount(2);
        context.ConsideredOptions.Should().Contain("Option A");
        context.ConsideredOptions.Should().Contain("Option B");
    }

    [Fact]
    public void AddOption_IgnoresDuplicates()
    {
        var context = new DecisionContext("Test", "Desc", "User", DateTime.UtcNow.AddDays(1));

        context.AddOption("Option A");
        context.AddOption("Option A");

        context.ConsideredOptions.Should().HaveCount(1);
    }

    [Fact]
    public void AddOption_ThrowsOnNull()
    {
        var context = new DecisionContext("Test", "Desc", "User", DateTime.UtcNow.AddDays(1));

        var act = () => context.AddOption(null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddOption_ThrowsOnWhitespace()
    {
        var context = new DecisionContext("Test", "Desc", "User", DateTime.UtcNow.AddDays(1));

        var act = () => context.AddOption("   ");
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region SetAIRecommendation Tests

    [Fact]
    public void SetAIRecommendation_SetsValues()
    {
        var context = new DecisionContext("Test", "Desc", "User", DateTime.UtcNow.AddDays(1));

        context.SetAIRecommendation("Go with Option A", 0.85);

        context.AIRecommendation.Should().Be("Go with Option A");
        context.AIConfidence.Should().Be(0.85);
    }

    [Fact]
    public void SetAIRecommendation_RaisesDomainEvent()
    {
        var context = new DecisionContext("Budget", "Desc", "User", DateTime.UtcNow.AddDays(1));

        context.SetAIRecommendation("Choose B", 0.9);

        context.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AIRecommendationGeneratedEvent>()
            .Which.Recommendation.Should().Be("Choose B");
    }

    [Fact]
    public void SetAIRecommendation_ThrowsOnNullRecommendation()
    {
        var context = new DecisionContext("Test", "Desc", "User", DateTime.UtcNow.AddDays(1));

        var act = () => context.SetAIRecommendation(null!, 0.5);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetAIRecommendation_ThrowsOnConfidenceAboveOne()
    {
        var context = new DecisionContext("Test", "Desc", "User", DateTime.UtcNow.AddDays(1));

        var act = () => context.SetAIRecommendation("Rec", 1.5);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void SetAIRecommendation_ThrowsOnNegativeConfidence()
    {
        var context = new DecisionContext("Test", "Desc", "User", DateTime.UtcNow.AddDays(1));

        var act = () => context.SetAIRecommendation("Rec", -0.1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void SetAIRecommendation_AcceptsBoundaryValues()
    {
        var context = new DecisionContext("Test", "Desc", "User", DateTime.UtcNow.AddDays(1));

        context.SetAIRecommendation("Zero", 0.0);
        context.AIConfidence.Should().Be(0.0);

        context.SetAIRecommendation("One", 1.0);
        context.AIConfidence.Should().Be(1.0);
    }

    #endregion

    #region MakeDecision Tests

    [Fact]
    public void MakeDecision_SetsSelectedOption()
    {
        var context = new DecisionContext("Test", "Desc", "User", DateTime.UtcNow.AddDays(1));
        context.AddOption("Option A");
        context.AddOption("Option B");

        context.MakeDecision("Option A", "admin");

        context.SelectedOption.Should().Be("Option A");
        context.Status.Should().Be(DecisionStatus.Decided);
        context.DecidedAt.Should().NotBeNull();
        context.DecidedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MakeDecision_RaisesDomainEvent()
    {
        var context = new DecisionContext("Budget", "Desc", "User", DateTime.UtcNow.AddDays(1));
        context.AddOption("Option X");

        context.MakeDecision("Option X", "admin");

        context.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<DecisionMadeEvent>()
            .Which.SelectedOption.Should().Be("Option X");
    }

    [Fact]
    public void MakeDecision_ThrowsWhenOptionNotConsidered()
    {
        var context = new DecisionContext("Test", "Desc", "User", DateTime.UtcNow.AddDays(1));
        context.AddOption("Option A");

        var act = () => context.MakeDecision("Option Z", "admin");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not in the considered options*");
    }

    [Fact]
    public void MakeDecision_ThrowsOnNullOption()
    {
        var context = new DecisionContext("Test", "Desc", "User", DateTime.UtcNow.AddDays(1));

        var act = () => context.MakeDecision(null!, "admin");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MakeDecision_ThrowsOnNullDecidedBy()
    {
        var context = new DecisionContext("Test", "Desc", "User", DateTime.UtcNow.AddDays(1));
        context.AddOption("A");

        var act = () => context.MakeDecision("A", null!);
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region AddContextData Tests

    [Fact]
    public void AddContextData_AddsKeyValuePair()
    {
        var context = new DecisionContext("Test", "Desc", "User", DateTime.UtcNow.AddDays(1));

        context.AddContextData("budget", 50000);
        context.AddContextData("department", "Engineering");

        context.ContextData.Should().HaveCount(2);
        context.ContextData["budget"].Should().Be(50000);
        context.ContextData["department"].Should().Be("Engineering");
    }

    [Fact]
    public void AddContextData_OverwritesExistingKey()
    {
        var context = new DecisionContext("Test", "Desc", "User", DateTime.UtcNow.AddDays(1));

        context.AddContextData("budget", 50000);
        context.AddContextData("budget", 75000);

        context.ContextData["budget"].Should().Be(75000);
    }

    [Fact]
    public void AddContextData_ThrowsOnNullKey()
    {
        var context = new DecisionContext("Test", "Desc", "User", DateTime.UtcNow.AddDays(1));

        var act = () => context.AddContextData(null!, "value");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddContextData_ThrowsOnNullValue()
    {
        var context = new DecisionContext("Test", "Desc", "User", DateTime.UtcNow.AddDays(1));

        var act = () => context.AddContextData("key", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Enum Tests

    [Theory]
    [InlineData(DecisionPriority.Low, 0)]
    [InlineData(DecisionPriority.Medium, 1)]
    [InlineData(DecisionPriority.High, 2)]
    [InlineData(DecisionPriority.Critical, 3)]
    public void DecisionPriority_HasExpectedValues(DecisionPriority priority, int expected)
    {
        ((int)priority).Should().Be(expected);
    }

    [Theory]
    [InlineData(DecisionStatus.Pending, 0)]
    [InlineData(DecisionStatus.UnderReview, 1)]
    [InlineData(DecisionStatus.Decided, 2)]
    [InlineData(DecisionStatus.Implemented, 3)]
    [InlineData(DecisionStatus.Abandoned, 4)]
    public void DecisionStatus_HasExpectedValues(DecisionStatus status, int expected)
    {
        ((int)status).Should().Be(expected);
    }

    #endregion
}
