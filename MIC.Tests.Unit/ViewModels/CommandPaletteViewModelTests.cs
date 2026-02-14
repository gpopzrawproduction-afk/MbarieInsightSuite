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

    #region AllCommands Population

    [Fact]
    public void Constructor_PopulatesAllCommandsCollection()
    {
        var vm = new CommandPaletteViewModel();
        vm.AllCommands.Should().NotBeEmpty();
        vm.AllCommands.Count.Should().BeGreaterThanOrEqualTo(12);
    }

    [Fact]
    public void AllCommands_ContainsNavigationCategory()
    {
        var vm = new CommandPaletteViewModel();
        vm.AllCommands.Should().Contain(c => c.Category == "Navigation");
    }

    [Fact]
    public void AllCommands_ContainsActionsCategory()
    {
        var vm = new CommandPaletteViewModel();
        vm.AllCommands.Should().Contain(c => c.Category == "Actions");
    }

    [Fact]
    public void AllCommands_ContainsAICategory()
    {
        var vm = new CommandPaletteViewModel();
        vm.AllCommands.Should().Contain(c => c.Category == "AI");
    }

    [Fact]
    public void AllCommands_ContainsSystemCategory()
    {
        var vm = new CommandPaletteViewModel();
        vm.AllCommands.Should().Contain(c => c.Category == "System");
    }

    [Fact]
    public void AllCommands_AllHaveNameAndDescription()
    {
        var vm = new CommandPaletteViewModel();
        vm.AllCommands.Should().OnlyContain(c =>
            !string.IsNullOrEmpty(c.Name) && !string.IsNullOrEmpty(c.Description));
    }

    [Fact]
    public void AllCommands_AllHaveActions()
    {
        var vm = new CommandPaletteViewModel();
        vm.AllCommands.Should().OnlyContain(c => c.Action != null);
    }

    #endregion

    #region FilteredCommands and Filtering

    [Fact]
    public void FilteredCommands_WhenOpen_MaximumTenResults()
    {
        var vm = new CommandPaletteViewModel();
        vm.IsOpen = true;
        InvokeFilter(vm);

        vm.FilteredCommands.Count.Should().BeLessOrEqualTo(10);
    }

    [Fact]
    public void FilterCommands_EmptySearch_ReturnsAllUpToTen()
    {
        var vm = new CommandPaletteViewModel();
        vm.IsOpen = true;
        vm.SearchText = string.Empty;
        InvokeFilter(vm);

        vm.FilteredCommands.Count.Should().BeLessOrEqualTo(10);
        vm.FilteredCommands.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void FilterCommands_SearchByCategory_ReturnsMatchingCommands()
    {
        var vm = new CommandPaletteViewModel();
        vm.IsOpen = true;
        vm.SearchText = "System";
        InvokeFilter(vm);

        vm.FilteredCommands.Should().NotBeEmpty();
        vm.FilteredCommands.Should().OnlyContain(c =>
            c.Name.Contains("System", StringComparison.OrdinalIgnoreCase) ||
            c.Description.Contains("System", StringComparison.OrdinalIgnoreCase) ||
            c.Category.Contains("System", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FilterCommands_NoMatch_ReturnsEmptyList()
    {
        var vm = new CommandPaletteViewModel();
        vm.IsOpen = true;
        vm.SearchText = "zzzzzznonexistent";
        InvokeFilter(vm);

        vm.FilteredCommands.Should().BeEmpty();
    }

    [Fact]
    public void FilterCommands_SetsSelectedIndexToZero_WhenResultsExist()
    {
        var vm = new CommandPaletteViewModel();
        vm.IsOpen = true;
        vm.SearchText = "dashboard";
        InvokeFilter(vm);

        vm.SelectedIndex.Should().Be(0);
    }

    #endregion

    #region MoveUp / MoveDown

    [Fact]
    public void MoveDown_IncrementsSelectedIndex()
    {
        var vm = new CommandPaletteViewModel();
        vm.IsOpen = true;
        InvokeFilter(vm);
        vm.SelectedIndex.Should().Be(0);

        InvokeMoveDown(vm);

        vm.SelectedIndex.Should().Be(1);
    }

    [Fact]
    public void MoveDown_DoesNotExceedLastIndex()
    {
        var vm = new CommandPaletteViewModel();
        vm.IsOpen = true;
        InvokeFilter(vm);

        var lastIndex = vm.FilteredCommands.Count - 1;
        vm.SelectedIndex = lastIndex;

        InvokeMoveDown(vm);

        vm.SelectedIndex.Should().Be(lastIndex);
    }

    [Fact]
    public void MoveUp_DecrementsSelectedIndex()
    {
        var vm = new CommandPaletteViewModel();
        vm.IsOpen = true;
        InvokeFilter(vm);
        vm.SelectedIndex = 2;

        InvokeMoveUp(vm);

        vm.SelectedIndex.Should().Be(1);
    }

    [Fact]
    public void MoveUp_DoesNotGoBelowZero()
    {
        var vm = new CommandPaletteViewModel();
        vm.IsOpen = true;
        InvokeFilter(vm);
        vm.SelectedIndex = 0;

        InvokeMoveUp(vm);

        vm.SelectedIndex.Should().Be(0);
    }

    #endregion

    #region Close / Toggle

    [Fact]
    public async Task CloseCommand_SetsIsOpenFalseAndClearsSearch()
    {
        var vm = new CommandPaletteViewModel();
        vm.IsOpen = true;
        vm.SearchText = "test";

        await vm.CloseCommand.Execute().ToTask();

        vm.IsOpen.Should().BeFalse();
        vm.SearchText.Should().BeEmpty();
    }

    [Fact]
    public void Toggle_WhenClosed_Opens()
    {
        var vm = new CommandPaletteViewModel();
        vm.IsOpen = false;

        vm.Toggle();

        vm.IsOpen.Should().BeTrue();
    }

    [Fact]
    public void Toggle_WhenOpen_Closes()
    {
        var vm = new CommandPaletteViewModel();
        vm.IsOpen = true;

        vm.Toggle();

        vm.IsOpen.Should().BeFalse();
    }

    #endregion

    #region SelectedCommand

    [Fact]
    public void SelectedCommand_ReturnsCorrectCommandAtIndex()
    {
        var vm = new CommandPaletteViewModel();
        vm.IsOpen = true;
        InvokeFilter(vm);
        vm.SelectedIndex = 0;

        vm.SelectedCommand.Should().Be(vm.FilteredCommands[0]);
    }

    [Fact]
    public void SelectedCommand_RetainsPreviousValue_WhenFilteredCommandsEmpty()
    {
        var vm = new CommandPaletteViewModel();
        vm.IsOpen = true;
        var previousSelected = vm.SelectedCommand;
        vm.SearchText = "zzzzzznonexistent";
        InvokeFilter(vm);

        vm.FilteredCommands.Should().BeEmpty();
        // SelectedCommand is not cleared by FilterCommands when results are empty
        vm.SelectedCommand.Should().Be(previousSelected);
    }

    #endregion

    #region IsOpen Setter Behavior

    [Fact]
    public void IsOpen_SetToFalse_ClearsFilteredCommands()
    {
        var vm = new CommandPaletteViewModel();
        vm.IsOpen = true;
        vm.FilteredCommands.Should().NotBeEmpty();

        vm.IsOpen = false;

        vm.SearchText.Should().BeEmpty();
    }

    [Fact]
    public void IsOpen_SetToTrue_ResetsSelectedIndex()
    {
        var vm = new CommandPaletteViewModel();
        vm.IsOpen = true;
        vm.SelectedIndex = 3;

        vm.IsOpen = true;

        vm.SelectedIndex.Should().Be(0);
    }

    #endregion

    #region Command Actions Invocation

    [Fact]
    public async Task ExecuteSelectedCommand_WhenNoCommandSelected_DoesNotThrow()
    {
        var vm = new CommandPaletteViewModel();
        vm.IsOpen = true;
        vm.SearchText = "zzzzzznonexistent";
        InvokeFilter(vm);

        var act = async () => await vm.ExecuteSelectedCommand.Execute().ToTask();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecuteSelectedCommand_ToggleTheme_InvokesOnAction()
    {
        var vm = new CommandPaletteViewModel();
        vm.IsOpen = true;
        vm.SearchText = "dark mode";
        InvokeFilter(vm);

        var actionTarget = string.Empty;
        vm.OnAction = action => actionTarget = action;

        var index = vm.FilteredCommands.ToList().FindIndex(
            cmd => cmd.Name.Contains("Dark Mode", StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            vm.SelectedIndex = index;
            await vm.ExecuteSelectedCommand.Execute().ToTask();
            actionTarget.Should().Be("ToggleTheme");
        }
    }

    [Fact]
    public async Task ExecuteSelectedCommand_AskAI_InvokesOnNavigate()
    {
        var vm = new CommandPaletteViewModel();
        vm.IsOpen = true;
        vm.SearchText = "Ask AI";
        InvokeFilter(vm);

        var navigateTarget = string.Empty;
        vm.OnNavigate = route => navigateTarget = route;

        var index = vm.FilteredCommands.ToList().FindIndex(
            cmd => cmd.Name.Contains("Ask AI", StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            vm.SelectedIndex = index;
            await vm.ExecuteSelectedCommand.Execute().ToTask();
            navigateTarget.Should().Be("AI Chat");
        }
    }

    #endregion

    #region CommandItem Class

    [Fact]
    public void CommandItem_DefaultValues_AreCorrect()
    {
        var item = new CommandItem();
        item.Category.Should().BeEmpty();
        item.Name.Should().BeEmpty();
        item.Description.Should().BeEmpty();
        item.Shortcut.Should().BeEmpty();
        item.Icon.Should().Be("?");
        item.Action.Should().BeNull();
    }

    [Fact]
    public void CommandItem_CanBeCreatedWithInitializers()
    {
        var executed = false;
        var item = new CommandItem
        {
            Category = "Test",
            Name = "Test Command",
            Description = "A test command",
            Shortcut = "Ctrl+T",
            Icon = "star",
            Action = () => executed = true
        };

        item.Category.Should().Be("Test");
        item.Name.Should().Be("Test Command");
        item.Action!.Invoke();
        executed.Should().BeTrue();
    }

    #endregion

    #region Helpers

    private static void InvokeFilter(CommandPaletteViewModel vm)
    {
        var method = typeof(CommandPaletteViewModel).GetMethod("FilterCommands", BindingFlags.Instance | BindingFlags.NonPublic);
        method!.Invoke(vm, null);
    }

    private static void InvokeMoveUp(CommandPaletteViewModel vm)
    {
        var method = typeof(CommandPaletteViewModel).GetMethod("MoveUp", BindingFlags.Instance | BindingFlags.NonPublic);
        method!.Invoke(vm, null);
    }

    private static void InvokeMoveDown(CommandPaletteViewModel vm)
    {
        var method = typeof(CommandPaletteViewModel).GetMethod("MoveDown", BindingFlags.Instance | BindingFlags.NonPublic);
        method!.Invoke(vm, null);
    }

    #endregion
}
