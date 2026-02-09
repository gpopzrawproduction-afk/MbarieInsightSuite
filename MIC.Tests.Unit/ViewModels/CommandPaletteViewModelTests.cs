using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Reactive.Threading.Tasks;
using FluentAssertions;
using MIC.Desktop.Avalonia.ViewModels;

namespace MIC.Tests.Unit.ViewModels;

public sealed class CommandPaletteViewModelTests
{
    [Fact]
    public void IsOpen_WhenSetTrue_ResetsSearchAndPopulatesFiltered()
    {
        var vm = new CommandPaletteViewModel
        {
            SearchText = "dashboard"
        };

        vm.IsOpen = true;

        vm.SearchText.Should().BeEmpty();
        vm.FilteredCommands.Should().NotBeEmpty();
        vm.SelectedIndex.Should().Be(0);
        vm.SelectedCommand.Should().NotBeNull();
    }

    [Fact]
    public void SearchText_FiltersCommandsCaseInsensitive()
    {
        var vm = new CommandPaletteViewModel();
        vm.IsOpen = true;

        vm.SearchText = "alerts";
        InvokeFilter(vm);

        vm.FilteredCommands.Should().NotBeEmpty();
        vm.FilteredCommands.Should().OnlyContain(cmd =>
            cmd.Name.Contains("alerts", StringComparison.OrdinalIgnoreCase) ||
            cmd.Description.Contains("alerts", StringComparison.OrdinalIgnoreCase) ||
            cmd.Category.Contains("alerts", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExecuteSelectedCommand_InvokesNavigationActionAndClosesAsync()
    {
        var vm = new CommandPaletteViewModel();
        vm.IsOpen = true;
        vm.SearchText = "dashboard";
        InvokeFilter(vm);

        var navigateTarget = string.Empty;
        vm.OnNavigate = route => navigateTarget = route;

        var index = vm.FilteredCommands.ToList().FindIndex(cmd => cmd.Name.Equals("Go to Dashboard", StringComparison.OrdinalIgnoreCase));
        index.Should().BeGreaterThanOrEqualTo(0);
        vm.SelectedIndex = index;

        await vm.ExecuteSelectedCommand.Execute().ToTask();

        navigateTarget.Should().Be("Dashboard");
        vm.IsOpen.Should().BeFalse();
        vm.SearchText.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteSelectedCommand_InvokesActionDelegateAsync()
    {
        var vm = new CommandPaletteViewModel();
        vm.IsOpen = true;
        vm.SearchText = "export";
        InvokeFilter(vm);

        var actionTarget = string.Empty;
        vm.OnAction = action => actionTarget = action;

        var index = vm.FilteredCommands.ToList().FindIndex(cmd => cmd.Name.Equals("Export Report", StringComparison.OrdinalIgnoreCase));
        index.Should().BeGreaterThanOrEqualTo(0);
        vm.SelectedIndex = index;

        await vm.ExecuteSelectedCommand.Execute().ToTask();

        actionTarget.Should().Be("Export");
    }

    private static void InvokeFilter(CommandPaletteViewModel vm)
    {
        var method = typeof(CommandPaletteViewModel).GetMethod("FilterCommands", BindingFlags.Instance | BindingFlags.NonPublic);
        method!.Invoke(vm, null);
    }
}
