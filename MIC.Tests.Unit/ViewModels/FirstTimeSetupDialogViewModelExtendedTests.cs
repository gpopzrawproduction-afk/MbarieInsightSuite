using FluentAssertions;
using ReactiveUI;
using System.Reactive.Concurrency;
using MIC.Desktop.Avalonia.ViewModels;

namespace MIC.Tests.Unit.ViewModels;

/// <summary>
/// Extended tests for FirstTimeSetupDialogViewModel covering command execution,
/// switch expression branch coverage, and property change notifications.
/// </summary>
public class FirstTimeSetupDialogViewModelExtendedTests
{
    static FirstTimeSetupDialogViewModelExtendedTests()
    {
        RxApp.MainThreadScheduler = CurrentThreadScheduler.Instance;
        RxApp.TaskpoolScheduler = CurrentThreadScheduler.Instance;
    }

    #region ContinueCommand Execution - Exercises switch expression branches

    [Fact]
    public void ContinueCommand_Execute_ShouldNotThrow()
    {
        var vm = new FirstTimeSetupDialogViewModel();
        var act = () => vm.ContinueCommand.Execute(null);
        act.Should().NotThrow();
    }

    [Fact]
    public void ContinueCommand_CanExecute_ShouldBeTrue()
    {
        var vm = new FirstTimeSetupDialogViewModel();
        vm.ContinueCommand.CanExecute(null).Should().BeTrue();
    }

    [Theory]
    [InlineData(0)] // 1 month
    [InlineData(1)] // 3 months
    [InlineData(2)] // 6 months
    [InlineData(3)] // 12 months
    [InlineData(4)] // 120 months (all)
    [InlineData(5)] // default → 6 months
    [InlineData(99)] // out of range → default
    public void ContinueCommand_AllEmailHistoryBranches_ShouldNotThrow(int index)
    {
        var vm = new FirstTimeSetupDialogViewModel();
        vm.EmailHistoryMonthsIndex = index;
        var act = () => vm.ContinueCommand.Execute(null);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0)] // 7 days
    [InlineData(1)] // 30 days
    [InlineData(2)] // 90 days
    [InlineData(3)] // 180 days
    [InlineData(4)] // 365 days
    [InlineData(5)] // default → 30
    [InlineData(99)] // out of range → default
    public void ContinueCommand_AllPredictionHorizonBranches_ShouldNotThrow(int index)
    {
        var vm = new FirstTimeSetupDialogViewModel();
        vm.PredictionHorizonIndex = index;
        var act = () => vm.ContinueCommand.Execute(null);
        act.Should().NotThrow();
    }

    [Fact]
    public void ContinueCommand_WithAllFeaturesDisabled_ShouldNotThrow()
    {
        var vm = new FirstTimeSetupDialogViewModel();
        vm.DownloadAttachments = false;
        vm.IncludeSentFolder = false;
        vm.EnablePredictiveAnalytics = false;
        vm.EnableSentimentAnalysis = false;
        vm.EnableAutoCategorization = false;
        vm.EnableDesktopNotifications = false;
        vm.EnableEmailDigest = false;
        vm.EnableProactiveInsights = false;

        var act = () => vm.ContinueCommand.Execute(null);
        act.Should().NotThrow();
    }

    #endregion

    #region SkipCommand Execution

    [Fact]
    public void SkipCommand_Execute_ShouldNotThrow()
    {
        var vm = new FirstTimeSetupDialogViewModel();
        var act = () => vm.SkipCommand.Execute(null);
        act.Should().NotThrow();
    }

    [Fact]
    public void SkipCommand_CanExecute_ShouldBeTrue()
    {
        var vm = new FirstTimeSetupDialogViewModel();
        vm.SkipCommand.CanExecute(null).Should().BeTrue();
    }

    #endregion

    #region Property Change Notifications

    [Fact]
    public void EnableSentimentAnalysis_Toggle_RaisesPropertyChanged()
    {
        var vm = new FirstTimeSetupDialogViewModel();
        bool raised = false;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FirstTimeSetupDialogViewModel.EnableSentimentAnalysis))
                raised = true;
        };

        vm.EnableSentimentAnalysis = false;
        raised.Should().BeTrue();
    }

    [Fact]
    public void EnableAutoCategorization_Toggle_RaisesPropertyChanged()
    {
        var vm = new FirstTimeSetupDialogViewModel();
        bool raised = false;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FirstTimeSetupDialogViewModel.EnableAutoCategorization))
                raised = true;
        };

        vm.EnableAutoCategorization = false;
        raised.Should().BeTrue();
    }

    [Fact]
    public void EnableDesktopNotifications_Toggle_RaisesPropertyChanged()
    {
        var vm = new FirstTimeSetupDialogViewModel();
        bool raised = false;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FirstTimeSetupDialogViewModel.EnableDesktopNotifications))
                raised = true;
        };

        vm.EnableDesktopNotifications = false;
        raised.Should().BeTrue();
    }

    [Fact]
    public void EnableEmailDigest_Toggle_RaisesPropertyChanged()
    {
        var vm = new FirstTimeSetupDialogViewModel();
        bool raised = false;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FirstTimeSetupDialogViewModel.EnableEmailDigest))
                raised = true;
        };

        vm.EnableEmailDigest = false;
        raised.Should().BeTrue();
    }

    [Fact]
    public void EnableProactiveInsights_Toggle_RaisesPropertyChanged()
    {
        var vm = new FirstTimeSetupDialogViewModel();
        bool raised = false;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FirstTimeSetupDialogViewModel.EnableProactiveInsights))
                raised = true;
        };

        vm.EnableProactiveInsights = false;
        raised.Should().BeTrue();
    }

    [Fact]
    public void PredictionHorizonIndex_Set_RaisesPropertyChanged()
    {
        var vm = new FirstTimeSetupDialogViewModel();
        bool raised = false;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FirstTimeSetupDialogViewModel.PredictionHorizonIndex))
                raised = true;
        };

        vm.PredictionHorizonIndex = 4;
        raised.Should().BeTrue();
    }

    [Fact]
    public void EmailHistoryMonthsIndex_Set_RaisesPropertyChanged()
    {
        var vm = new FirstTimeSetupDialogViewModel();
        bool raised = false;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FirstTimeSetupDialogViewModel.EmailHistoryMonthsIndex))
                raised = true;
        };

        vm.EmailHistoryMonthsIndex = 0;
        raised.Should().BeTrue();
    }

    #endregion

    #region Toggle All Booleans

    [Fact]
    public void AllBooleans_CanBeToggledMultipleTimes()
    {
        var vm = new FirstTimeSetupDialogViewModel();

        vm.DownloadAttachments = false;
        vm.DownloadAttachments = true;
        vm.DownloadAttachments.Should().BeTrue();

        vm.IncludeSentFolder = false;
        vm.IncludeSentFolder = true;
        vm.IncludeSentFolder.Should().BeTrue();

        vm.EnablePredictiveAnalytics = false;
        vm.EnablePredictiveAnalytics = true;
        vm.EnablePredictiveAnalytics.Should().BeTrue();

        vm.EnableSentimentAnalysis = false;
        vm.EnableSentimentAnalysis = true;
        vm.EnableSentimentAnalysis.Should().BeTrue();

        vm.EnableAutoCategorization = false;
        vm.EnableAutoCategorization = true;
        vm.EnableAutoCategorization.Should().BeTrue();

        vm.EnableDesktopNotifications = false;
        vm.EnableDesktopNotifications = true;
        vm.EnableDesktopNotifications.Should().BeTrue();

        vm.EnableEmailDigest = false;
        vm.EnableEmailDigest = true;
        vm.EnableEmailDigest.Should().BeTrue();

        vm.EnableProactiveInsights = false;
        vm.EnableProactiveInsights = true;
        vm.EnableProactiveInsights.Should().BeTrue();
    }

    #endregion
}
