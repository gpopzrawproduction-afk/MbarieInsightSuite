using System;
using System.Reactive;
using FluentAssertions;
using MIC.Desktop.Avalonia.ViewModels;
using ReactiveUI;

namespace MIC.Tests.Unit.ViewModels;

public sealed class DialogBaseViewModelTests
{
    static DialogBaseViewModelTests()
    {
        RxApp.MainThreadScheduler = System.Reactive.Concurrency.CurrentThreadScheduler.Instance;
        RxApp.TaskpoolScheduler = System.Reactive.Concurrency.CurrentThreadScheduler.Instance;
    }

    [Fact]
    public void CloseDialogCommand_IsNotNull()
    {
        var vm = new TestDialogViewModel();

        vm.CloseDialogCommand.Should().NotBeNull();
    }

    [Fact]
    public void CloseDialogCommand_Executes_RaisesRequestClose()
    {
        var vm = new TestDialogViewModel();
        var raised = false;
        vm.RequestClose += () => raised = true;

        vm.CloseDialogCommand.Execute().Subscribe();

        raised.Should().BeTrue();
    }

    [Fact]
    public void CloseDialogCommand_NoSubscriber_DoesNotThrow()
    {
        var vm = new TestDialogViewModel();

        var act = () => vm.CloseDialogCommand.Execute().Subscribe();

        act.Should().NotThrow();
    }

    [Fact]
    public void RequestClose_MultipleSubscribers_AllNotified()
    {
        var vm = new TestDialogViewModel();
        var count = 0;
        vm.RequestClose += () => count++;
        vm.RequestClose += () => count++;

        vm.CloseDialogCommand.Execute().Subscribe();

        count.Should().Be(2);
    }

    /// <summary>
    /// Concrete subclass for testing the abstract-like DialogBaseViewModel.
    /// </summary>
    private class TestDialogViewModel : DialogBaseViewModel { }
}
