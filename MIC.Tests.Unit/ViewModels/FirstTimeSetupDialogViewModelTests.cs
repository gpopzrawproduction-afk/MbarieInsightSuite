using System.Reactive.Concurrency;
using FluentAssertions;
using ReactiveUI;
using Xunit;

namespace MIC.Tests.Unit.ViewModels;

/// <summary>
/// Tests for <see cref="MIC.Desktop.Avalonia.ViewModels.FirstTimeSetupDialogViewModel"/>.
/// Tests switch expression mappings and default property values.
/// CloseDialog is not tested because it requires Avalonia Application.Current.
/// </summary>
public class FirstTimeSetupDialogViewModelTests
{
    static FirstTimeSetupDialogViewModelTests()
    {
        RxApp.MainThreadScheduler = CurrentThreadScheduler.Instance;
        RxApp.TaskpoolScheduler = CurrentThreadScheduler.Instance;
    }

    private static MIC.Desktop.Avalonia.ViewModels.FirstTimeSetupDialogViewModel Create()
        => new();

    #region Default Values

    [Fact]
    public void Defaults_EmailHistoryMonthsIndex_Is2()
    {
        var vm = Create();
        vm.EmailHistoryMonthsIndex.Should().Be(2); // 6 months
    }

    [Fact]
    public void Defaults_DownloadAttachments_True()
    {
        var vm = Create();
        vm.DownloadAttachments.Should().BeTrue();
    }

    [Fact]
    public void Defaults_IncludeSentFolder_True()
    {
        var vm = Create();
        vm.IncludeSentFolder.Should().BeTrue();
    }

    [Fact]
    public void Defaults_EnablePredictiveAnalytics_True()
    {
        var vm = Create();
        vm.EnablePredictiveAnalytics.Should().BeTrue();
    }

    [Fact]
    public void Defaults_EnableSentimentAnalysis_True()
    {
        var vm = Create();
        vm.EnableSentimentAnalysis.Should().BeTrue();
    }

    [Fact]
    public void Defaults_EnableAutoCategorization_True()
    {
        var vm = Create();
        vm.EnableAutoCategorization.Should().BeTrue();
    }

    [Fact]
    public void Defaults_PredictionHorizonIndex_Is1()
    {
        var vm = Create();
        vm.PredictionHorizonIndex.Should().Be(1); // 30 days
    }

    [Fact]
    public void Defaults_EnableDesktopNotifications_True()
    {
        var vm = Create();
        vm.EnableDesktopNotifications.Should().BeTrue();
    }

    [Fact]
    public void Defaults_EnableEmailDigest_True()
    {
        var vm = Create();
        vm.EnableEmailDigest.Should().BeTrue();
    }

    [Fact]
    public void Defaults_EnableProactiveInsights_True()
    {
        var vm = Create();
        vm.EnableProactiveInsights.Should().BeTrue();
    }

    #endregion

    #region Property Setters

    [Fact]
    public void EmailHistoryMonthsIndex_CanBeChanged()
    {
        var vm = Create();
        vm.EmailHistoryMonthsIndex = 4;
        vm.EmailHistoryMonthsIndex.Should().Be(4);
    }

    [Fact]
    public void PredictionHorizonIndex_CanBeChanged()
    {
        var vm = Create();
        vm.PredictionHorizonIndex = 3;
        vm.PredictionHorizonIndex.Should().Be(3);
    }

    [Fact]
    public void DownloadAttachments_CanBeToggled()
    {
        var vm = Create();
        vm.DownloadAttachments = false;
        vm.DownloadAttachments.Should().BeFalse();
    }

    [Fact]
    public void IncludeSentFolder_CanBeToggled()
    {
        var vm = Create();
        vm.IncludeSentFolder = false;
        vm.IncludeSentFolder.Should().BeFalse();
    }

    [Fact]
    public void EnablePredictiveAnalytics_CanBeToggled()
    {
        var vm = Create();
        vm.EnablePredictiveAnalytics = false;
        vm.EnablePredictiveAnalytics.Should().BeFalse();
    }

    #endregion

    #region Commands Exist

    [Fact]
    public void ContinueCommand_IsNotNull()
    {
        var vm = Create();
        vm.ContinueCommand.Should().NotBeNull();
    }

    [Fact]
    public void SkipCommand_IsNotNull()
    {
        var vm = Create();
        vm.SkipCommand.Should().NotBeNull();
    }

    #endregion
}
