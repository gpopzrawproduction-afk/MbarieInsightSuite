using System;
using System.Reactive;
using FluentAssertions;
using MIC.Desktop.Avalonia.ViewModels;
using ReactiveUI;

namespace MIC.Tests.Unit.ViewModels;

public sealed class AboutDialogViewModelTests
{
    static AboutDialogViewModelTests()
    {
        RxApp.MainThreadScheduler = System.Reactive.Concurrency.CurrentThreadScheduler.Instance;
        RxApp.TaskpoolScheduler = System.Reactive.Concurrency.CurrentThreadScheduler.Instance;
    }

    [Fact]
    public void Ctor_InheritsDialogBaseViewModel()
    {
        var vm = new AboutDialogViewModel();

        vm.Should().BeAssignableTo<DialogBaseViewModel>();
    }

    [Fact]
    public void CloseDialogCommand_IsInherited_AndWorks()
    {
        var vm = new AboutDialogViewModel();
        var raised = false;
        vm.RequestClose += () => raised = true;

        vm.CloseDialogCommand.Execute().Subscribe();

        raised.Should().BeTrue();
    }
}
