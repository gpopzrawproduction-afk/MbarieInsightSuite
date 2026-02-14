using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MIC.Core.Intelligence;

namespace MIC.Tests.Unit.Core.Intelligence;

public sealed class PositionQuestionnaireServiceTests
{
    private readonly PositionQuestionnaireService _sut = new();

    // ──────────────────────────────────────────────────────────────
    // GetPositionCategory via GetQuestionnaireForPositionAsync
    // ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Managing Director")]
    [InlineData("General Manager")]
    public async Task GetQuestionnaire_ExecutivePositions_ReturnsExecutiveQuestions(string position)
    {
        var questions = await _sut.GetQuestionnaireForPositionAsync(position);

        questions.Should().NotBeEmpty();
        questions.Should().Contain(q => q.Id.StartsWith("exec-"));
    }

    [Theory]
    [InlineData("Workshop Manager")]
    [InlineData("Production Supervisor")]
    [InlineData("Logistics Coordinator")]
    [InlineData("Team Lead")]
    public async Task GetQuestionnaire_ManagementPositions_ReturnsManagementQuestions(string position)
    {
        var questions = await _sut.GetQuestionnaireForPositionAsync(position);

        questions.Should().NotBeEmpty();
        questions.Should().Contain(q => q.Id.StartsWith("mgmt-"));
    }

    [Theory]
    [InlineData("Mechanical Engineer")]
    [InlineData("Electrical Technician")]
    [InlineData("QAQC Inspector")]
    [InlineData("Senior Electrical")]
    public async Task GetQuestionnaire_TechnicalPositions_ReturnsTechnicalQuestions(string position)
    {
        var questions = await _sut.GetQuestionnaireForPositionAsync(position);

        questions.Should().NotBeEmpty();
        questions.Should().Contain(q => q.Id.StartsWith("tech-"));
    }

    [Theory]
    [InlineData("Crane Operator")]
    [InlineData("Pipe Fitter")]
    [InlineData("Expert Welder")]
    [InlineData("Truck Driver")]
    [InlineData("Workshop Helper")]
    public async Task GetQuestionnaire_OperationalPositions_ReturnsOperationalQuestions(string position)
    {
        var questions = await _sut.GetQuestionnaireForPositionAsync(position);

        questions.Should().NotBeEmpty();
        questions.Should().Contain(q => q.Id.StartsWith("op-"));
    }

    [Theory]
    [InlineData("HR Specialist")]
    [InlineData("Logistics Assistant")]
    [InlineData("Material Controller")]
    [InlineData("Document Control Officer")]
    [InlineData("Store Personnel")]
    public async Task GetQuestionnaire_SupportPositions_ReturnsSupportQuestions(string position)
    {
        var questions = await _sut.GetQuestionnaireForPositionAsync(position);

        questions.Should().NotBeEmpty();
        questions.Should().Contain(q => q.Id.StartsWith("support-"));
    }

    [Theory]
    [InlineData("Receptionist")]
    [InlineData("Intern")]
    [InlineData("Janitor")]
    public async Task GetQuestionnaire_UnknownPosition_ReturnsGeneralQuestions(string position)
    {
        var questions = await _sut.GetQuestionnaireForPositionAsync(position);

        questions.Should().NotBeEmpty();
        questions.Should().Contain(q => q.Id.StartsWith("gen-"));
    }

    // ──────────────────────────────────────────────────────────────
    // Question structure validation
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetQuestionnaire_AllQuestions_HaveRequiredProperties()
    {
        var positions = new[] { "Managing Director", "Workshop Manager", "Engineer", "Operator", "HR Manager", "Intern" };

        foreach (var position in positions)
        {
            var questions = await _sut.GetQuestionnaireForPositionAsync(position);

            foreach (var q in questions)
            {
                q.Id.Should().NotBeNullOrWhiteSpace($"Position: {position}");
                q.Text.Should().NotBeNullOrWhiteSpace($"Position: {position}, Id: {q.Id}");
                q.Category.Should().NotBeNullOrWhiteSpace($"Position: {position}, Id: {q.Id}");
                q.Options.Should().NotBeEmpty($"Position: {position}, Id: {q.Id}");
            }
        }
    }

    [Theory]
    [InlineData("Managing Director", 2)]  // 2 exec questions
    [InlineData("Workshop Manager", 3)]   // 2 mgmt + 1 position-specific (workshop manager)
    [InlineData("Engineer", 3)]           // 2 tech + 1 position-specific (engineer)
    [InlineData("Crane Operator", 2)]     // 2 operational
    [InlineData("HR Specialist", 2)]      // 2 support (no management keywords, 'hr' triggers support)
    [InlineData("Intern", 2)]             // 2 general
    public async Task GetQuestionnaire_ReturnsExpectedQuestionCount(string position, int expectedCount)
    {
        var questions = await _sut.GetQuestionnaireForPositionAsync(position);

        questions.Should().HaveCount(expectedCount);
    }

    // ──────────────────────────────────────────────────────────────
    // Position-specific questions
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetQuestionnaire_WorkshopManager_IncludesWorkshopSpecificQuestion()
    {
        var questions = await _sut.GetQuestionnaireForPositionAsync("Workshop Manager");

        questions.Should().Contain(q => q.Id == "ws-equipment-focus");
        var wsQuestion = questions.First(q => q.Id == "ws-equipment-focus");
        wsQuestion.Category.Should().Be("Workshop Operations");
    }

    [Fact]
    public async Task GetQuestionnaire_Engineer_IncludesEngineeringDisciplineQuestion()
    {
        var questions = await _sut.GetQuestionnaireForPositionAsync("Senior Engineer");

        questions.Should().Contain(q => q.Id == "eng-discipline");
        var engQuestion = questions.First(q => q.Id == "eng-discipline");
        engQuestion.Category.Should().Be("Engineering");
        engQuestion.Options.Should().Contain("Mechanical");
    }

    [Fact]
    public async Task GetQuestionnaire_SafetyPosition_IncludesSafetyQuestion()
    {
        var questions = await _sut.GetQuestionnaireForPositionAsync("Safety Coordinator");

        questions.Should().Contain(q => q.Id == "safety-focus");
    }

    [Fact]
    public async Task GetQuestionnaire_LogisticsPosition_IncludesLogisticsQuestion()
    {
        var questions = await _sut.GetQuestionnaireForPositionAsync("Logistics Coordinator");

        questions.Should().Contain(q => q.Id == "logistics-focus");
    }

    [Fact]
    public async Task GetQuestionnaire_PositionWithNoSpecificQuestions_DoesNotAddExtra()
    {
        var questions = await _sut.GetQuestionnaireForPositionAsync("Intern");

        // General only — no position-specific branch matches
        questions.Should().OnlyContain(q => q.Id.StartsWith("gen-"));
    }

    // ──────────────────────────────────────────────────────────────
    // Category question content validation
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetQuestionnaire_ExecutiveQuestions_HaveStrategyAndInfoCategories()
    {
        var questions = await _sut.GetQuestionnaireForPositionAsync("General Manager");

        questions.Select(q => q.Category).Should().Contain("Strategy");
        questions.Select(q => q.Category).Should().Contain("Information");
    }

    [Fact]
    public async Task GetQuestionnaire_ManagementQuestions_HaveTeamAndCommunicationCategories()
    {
        // Use a position that's Management but NOT workshop manager to avoid the extra question
        var questions = await _sut.GetQuestionnaireForPositionAsync("Production Supervisor");

        questions.Select(q => q.Category).Should().Contain("Team");
        questions.Select(q => q.Category).Should().Contain("Communication");
    }

    [Fact]
    public async Task GetQuestionnaire_OperationalQuestions_HaveSchedulingAndReportingCategories()
    {
        var questions = await _sut.GetQuestionnaireForPositionAsync("Crane Operator");

        questions.Select(q => q.Category).Should().Contain("Scheduling");
        questions.Select(q => q.Category).Should().Contain("Reporting");
    }

    [Fact]
    public async Task GetQuestionnaire_SupportQuestions_HaveCoordinationAndToolsCategories()
    {
        var questions = await _sut.GetQuestionnaireForPositionAsync("Store Personnel");

        questions.Select(q => q.Category).Should().Contain("Coordination");
        questions.Select(q => q.Category).Should().Contain("Tools");
    }

    [Fact]
    public async Task GetQuestionnaire_GeneralQuestions_HaveCommunicationAndProductivityCategories()
    {
        var questions = await _sut.GetQuestionnaireForPositionAsync("Intern");

        questions.Select(q => q.Category).Should().Contain("Communication");
        questions.Select(q => q.Category).Should().Contain("Productivity");
    }

    // ──────────────────────────────────────────────────────────────
    // GetDepartmentForPosition via GenerateRecommendationsAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateRecommendations_EngineerPosition_DelegatesWithEngineeringDepartment()
    {
        var answers = new Dictionary<string, string>();

        var result = await _sut.GenerateRecommendationsAsync("Engineer", answers);

        // Engineer position → Engineering department → IntelligenceProcessor generates 3 recommendations
        result.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("Safety Officer")]
    [InlineData("Workshop Foreman")]
    [InlineData("Operations Manager")]
    [InlineData("Project Lead")]
    [InlineData("Quality Inspector")]
    [InlineData("Data Scientist")]
    public async Task GenerateRecommendations_VariousPositions_ReturnsNonEmptyRecommendations(string position)
    {
        var answers = new Dictionary<string, string>();

        var result = await _sut.GenerateRecommendationsAsync(position, answers);

        result.Should().NotBeEmpty();
    }

    // ──────────────────────────────────────────────────────────────
    // ProcessAnswerForRecommendations
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateRecommendations_HighEmailVolumeAnswer_AddsFilterRecommendation()
    {
        var answers = new Dictionary<string, string>
        {
            { "gen-email-volume", "More than 50" }
        };

        var result = await _sut.GenerateRecommendationsAsync("Intern", answers);

        result.Should().Contain(r => r.Contains("filters") || r.Contains("filter"));
        result.Should().Contain(r => r.Contains("scheduled email checking"));
    }

    [Fact]
    public async Task GenerateRecommendations_DailyReportingAnswer_AddsAutomationRecommendation()
    {
        var answers = new Dictionary<string, string>
        {
            { "op-reporting", "Daily" }
        };

        var result = await _sut.GenerateRecommendationsAsync("Crane Operator", answers);

        result.Should().Contain(r => r.Contains("automating data collection"));
        result.Should().Contain(r => r.Contains("email templates"));
    }

    [Fact]
    public async Task GenerateRecommendations_LargeTeamAnswer_AddsDistributionListRecommendation()
    {
        var answers = new Dictionary<string, string>
        {
            { "mgmt-team-size", "21+" }
        };

        var result = await _sut.GenerateRecommendationsAsync("Workshop Manager", answers);

        result.Should().Contain(r => r.Contains("distribution lists"));
        result.Should().Contain(r => r.Contains("email tracking"));
    }

    [Fact]
    public async Task GenerateRecommendations_UnmatchedAnswer_DoesNotAddExtraRecommendations()
    {
        var answers = new Dictionary<string, string>
        {
            { "gen-email-volume", "Less than 10" }  // Doesn't match "More than 50"
        };

        var result = await _sut.GenerateRecommendationsAsync("Intern", answers);

        // Should only have IntelligenceProcessor base recommendations (3) + no answer extras
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GenerateRecommendations_UnknownQuestionId_DoesNotAddExtraRecommendations()
    {
        var answers = new Dictionary<string, string>
        {
            { "unknown-question", "some answer" }
        };

        var result = await _sut.GenerateRecommendationsAsync("Intern", answers);

        // Only IntelligenceProcessor base recommendations
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GenerateRecommendations_EmptyAnswers_ReturnsOnlyBaseRecommendations()
    {
        var answers = new Dictionary<string, string>();

        var result = await _sut.GenerateRecommendationsAsync("Intern", answers);

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GenerateRecommendations_MultipleMatchingAnswers_AddsAllRecommendations()
    {
        var answers = new Dictionary<string, string>
        {
            { "gen-email-volume", "More than 50" },
            { "mgmt-team-size", "21+" }
        };

        var result = await _sut.GenerateRecommendationsAsync("Workshop Manager", answers);

        // 3 base + 2 email volume + 2 team size = 7
        result.Should().HaveCount(7);
    }

    // ──────────────────────────────────────────────────────────────
    // Case insensitivity
    // ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("MANAGING DIRECTOR")]
    [InlineData("managing director")]
    [InlineData("Managing Director")]
    public async Task GetQuestionnaire_CaseInsensitivePositionMatching(string position)
    {
        var questions = await _sut.GetQuestionnaireForPositionAsync(position);

        questions.Should().Contain(q => q.Id.StartsWith("exec-"));
    }

    // ──────────────────────────────────────────────────────────────
    // PositionCategory enum coverage
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void PositionCategory_HasAllExpectedValues()
    {
        Enum.GetValues<PositionCategory>().Should().HaveCount(6);
        Enum.IsDefined(PositionCategory.Executive).Should().BeTrue();
        Enum.IsDefined(PositionCategory.Management).Should().BeTrue();
        Enum.IsDefined(PositionCategory.Technical).Should().BeTrue();
        Enum.IsDefined(PositionCategory.Operational).Should().BeTrue();
        Enum.IsDefined(PositionCategory.Support).Should().BeTrue();
        Enum.IsDefined(PositionCategory.General).Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────
    // Question class default state
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Question_DefaultValues_AreEmpty()
    {
        var question = new Question();

        question.Id.Should().BeEmpty();
        question.Text.Should().BeEmpty();
        question.Category.Should().BeEmpty();
        question.Options.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Question_CanSetProperties()
    {
        var question = new Question
        {
            Id = "test-id",
            Text = "Test question?",
            Category = "Testing",
            Options = new List<string> { "A", "B" }
        };

        question.Id.Should().Be("test-id");
        question.Text.Should().Be("Test question?");
        question.Category.Should().Be("Testing");
        question.Options.Should().HaveCount(2);
    }
}
