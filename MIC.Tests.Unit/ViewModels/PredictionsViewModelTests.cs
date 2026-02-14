using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Authentication.Common;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Predictions;
using MIC.Core.Intelligence.Predictions;
using MIC.Desktop.Avalonia.ViewModels;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace MIC.Tests.Unit.ViewModels;

/// <summary>
/// Tests for PredictionsViewModel covering prediction generation and management.
/// Target: 15 tests for predictive analytics functionality
/// </summary>
public class PredictionsViewModelTests
{
    private readonly IPredictiveAnalyticsService _predictiveService;
    private readonly ISessionService _sessionService;
    private readonly ILogger<PredictionsViewModel> _logger;

    public PredictionsViewModelTests()
    {
        _predictiveService = Substitute.For<IPredictiveAnalyticsService>();
        _sessionService = Substitute.For<ISessionService>();
        _logger = Substitute.For<ILogger<PredictionsViewModel>>();

        // Default user setup
        _sessionService.GetUser().Returns(new UserDto { Id = Guid.NewGuid(), Username = "testuser", Email = "test@example.com" });
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_InitializesProperties()
    {
        // Act
        var viewModel = new PredictionsViewModel(_predictiveService, _sessionService, _logger);

        // Assert
        viewModel.Predictions.Should().NotBeNull();
        viewModel.IsLoading.Should().BeFalse();
        viewModel.StatusMessage.Should().NotBeNullOrEmpty();
        viewModel.SelectedTimeHorizon.Should().Be(30);
    }

    [Fact]
    public void Constructor_InitializesCommands()
    {
        // Act
        var viewModel = new PredictionsViewModel(_predictiveService, _sessionService, _logger);

        // Assert
        viewModel.GeneratePredictionsCommand.Should().NotBeNull();
        viewModel.RefreshCommand.Should().NotBeNull();
        viewModel.ExportPredictionsCommand.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_AcceptsAllParameters()
    {
        // Act
        var viewModel = new PredictionsViewModel(_predictiveService, _sessionService, _logger);

        // Assert
        viewModel.Should().NotBeNull();
        viewModel.Predictions.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_HandlesNullLogger()
    {
        // Act - logger is optional in practice
        var act = () => new PredictionsViewModel(_predictiveService, _sessionService, null!);

        // Assert - should not throw during construction
        act.Should().NotThrow();
    }

    #endregion

    #region Property Tests

    [Fact]
    public void SelectedTimeHorizon_CanBeModified()
    {
        // Arrange
        var viewModel = new PredictionsViewModel(_predictiveService, _sessionService, _logger);

        // Act
        viewModel.SelectedTimeHorizon = 60;

        // Assert
        viewModel.SelectedTimeHorizon.Should().Be(60);
    }

    [Fact]
    public void IsLoading_StartsAsFalse()
    {
        // Arrange & Act
        var viewModel = new PredictionsViewModel(_predictiveService, _sessionService, _logger);

        // Assert
        viewModel.IsLoading.Should().BeFalse();
    }

    #endregion

    #region LoadPredictionsAsync Tests

    [Fact]
    public async Task LoadPredictionsAsync_PopulatesPredictionsCollection()
    {
        // Arrange
        var predictions = new List<Prediction>
        {
            new() { Title = "High Email Volume", Description = "Expected surge", Category = "Email", Confidence = 0.85, OccurrenceDate = DateTime.Now.AddDays(1), TimeHorizonDays = 30, Type = PredictionType.EmailVolumeIncrease },
            new() { Title = "Meeting Conflict", Description = "Potential overlap", Category = "Calendar", Confidence = 0.72, OccurrenceDate = DateTime.Now.AddDays(3), TimeHorizonDays = 30, Type = PredictionType.HighPriorityAlertSpike }
        };

        _predictiveService.GeneratePredictionsAsync(Arg.Any<Guid>(), Arg.Any<int>())
            .Returns(predictions);

        var viewModel = new PredictionsViewModel(_predictiveService, _sessionService, _logger);

        // Wait for constructor initialization
        await Task.Delay(100);

        // Assert
        viewModel.Predictions.Should().HaveCount(2);
        viewModel.Predictions[0].Title.Should().Be("High Email Volume");
        viewModel.Predictions[0].ConfidencePercentage.Should().Be("85%");
        viewModel.Predictions[1].Title.Should().Be("Meeting Conflict");
        viewModel.Predictions[1].ConfidencePercentage.Should().Be("72%");
    }

    [Fact]
    public async Task LoadPredictionsAsync_SetsCorrectConfidenceColors()
    {
        // Arrange
        var predictions = new List<Prediction>
        {
            new() { Title = "High Confidence", Confidence = 0.9, TimeHorizonDays = 30, Type = PredictionType.TrendReversal },
            new() { Title = "Medium Confidence", Confidence = 0.65, TimeHorizonDays = 30, Type = PredictionType.MetricAnomaly },
            new() { Title = "Low Confidence", Confidence = 0.4, TimeHorizonDays = 30, Type = PredictionType.HighPriorityAlertSpike }
        };

        _predictiveService.GeneratePredictionsAsync(Arg.Any<Guid>(), Arg.Any<int>())
            .Returns(predictions);

        var viewModel = new PredictionsViewModel(_predictiveService, _sessionService, _logger);
        await Task.Delay(100);

        // Assert
        viewModel.Predictions[0].ConfidenceColor.Should().Be("#10B981"); // Green - High
        viewModel.Predictions[1].ConfidenceColor.Should().Be("#FFB84D"); // Gold - Medium
        viewModel.Predictions[2].ConfidenceColor.Should().Be("#EF4444"); // Red - Low
    }

    [Fact]
    public async Task LoadPredictionsAsync_UpdatesStatusMessage()
    {
        // Arrange
        var predictions = new List<Prediction> { new() { Title = "Test", TimeHorizonDays = 30, Type = PredictionType.TrendReversal } };
        _predictiveService.GeneratePredictionsAsync(Arg.Any<Guid>(), Arg.Any<int>())
            .Returns(predictions);

        var viewModel = new PredictionsViewModel(_predictiveService, _sessionService, _logger);
        await Task.Delay(100);

        // Assert
        viewModel.StatusMessage.Should().Contain("Loaded");
        viewModel.StatusMessage.Should().Contain("1");
    }

    [Fact]
    public async Task LoadPredictionsAsync_HandlesServiceError()
    {
        // Arrange
        _predictiveService.GeneratePredictionsAsync(Arg.Any<Guid>(), Arg.Any<int>())
            .ThrowsAsync(new Exception("Service unavailable"));

        var viewModel = new PredictionsViewModel(_predictiveService, _sessionService, _logger);
        await Task.Delay(100);

        // Assert
        viewModel.StatusMessage.Should().Contain("Error");
        viewModel.IsLoading.Should().BeFalse();
    }

    #endregion

    #region Command Tests

    [Fact]
    public async Task GeneratePredictionsCommand_CallsService()
    {
        // Arrange
        var predictions = new List<Prediction>();
        _predictiveService.GeneratePredictionsAsync(Arg.Any<Guid>(), Arg.Any<int>())
            .Returns(predictions);

        var viewModel = new PredictionsViewModel(_predictiveService, _sessionService, _logger);
        await Task.Delay(100);

        // Reset call count
        _predictiveService.ClearReceivedCalls();

        // Act
        viewModel.GeneratePredictionsCommand.Execute(null);
        await Task.Delay(200);

        // Assert
        await _predictiveService.Received(1).GeneratePredictionsAsync(Arg.Any<Guid>(), Arg.Any<int>());
    }

    [Fact]
    public async Task RefreshCommand_ReloadsData()
    {
        // Arrange
        var predictions = new List<Prediction>
        {
            new() { Title = "Prediction 1", TimeHorizonDays = 30, Type = PredictionType.TrendReversal }
        };
        _predictiveService.GeneratePredictionsAsync(Arg.Any<Guid>(), Arg.Any<int>())
            .Returns(predictions);

        var viewModel = new PredictionsViewModel(_predictiveService, _sessionService, _logger);
        await Task.Delay(100);

        viewModel.Predictions.Clear();
        _predictiveService.ClearReceivedCalls();

        // Act
        viewModel.RefreshCommand.Execute(null);
        await Task.Delay(200);

        // Assert
        await _predictiveService.Received(1).GeneratePredictionsAsync(Arg.Any<Guid>(), Arg.Any<int>());
    }

    [Fact]
    public async Task ExportPredictionsCommand_UpdatesStatusMessage()
    {
        // Arrange
        _predictiveService.GeneratePredictionsAsync(Arg.Any<Guid>(), Arg.Any<int>())
            .Returns(new List<Prediction>());

        var viewModel = new PredictionsViewModel(_predictiveService, _sessionService, _logger);
        await Task.Delay(100);

        // Act
        viewModel.ExportPredictionsCommand.Execute(null);
        await Task.Delay(200);

        // Assert
        viewModel.StatusMessage.Should().Contain("Export");
    }

    #endregion
}
