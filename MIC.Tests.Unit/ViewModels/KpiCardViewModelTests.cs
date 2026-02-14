using FluentAssertions;
using MIC.Desktop.Avalonia.ViewModels;

namespace MIC.Tests.Unit.ViewModels;

public class KpiCardViewModelTests
{
    [Fact]
    public void Defaults_ShouldBeCorrect()
    {
        var vm = new KpiCardViewModel();

        vm.Title.Should().BeEmpty();
        vm.Value.Should().BeEmpty();
        vm.Target.Should().BeEmpty();
        vm.Change.Should().BeEmpty();
        vm.TrendIcon.Should().BeEmpty();
        vm.Status.Should().BeEmpty();
        vm.Progress.Should().Be(0);
        vm.Color.Should().Be("#00E5FF");
    }

    [Fact]
    public void AllProperties_CanBeInitialized()
    {
        var vm = new KpiCardViewModel
        {
            Title = "Revenue",
            Value = "$150,000",
            Target = "Target: 200,000",
            Change = "+5.5%",
            TrendIcon = "↑",
            Status = "On Target",
            Progress = 75.5,
            Color = "#39FF14"
        };

        vm.Title.Should().Be("Revenue");
        vm.Value.Should().Be("$150,000");
        vm.Target.Should().Be("Target: 200,000");
        vm.Change.Should().Be("+5.5%");
        vm.TrendIcon.Should().Be("↑");
        vm.Status.Should().Be("On Target");
        vm.Progress.Should().Be(75.5);
        vm.Color.Should().Be("#39FF14");
    }

    [Theory]
    [InlineData("On Target", "#39FF14")]
    [InlineData("Warning", "#FF6B00")]
    [InlineData("Critical", "#FF0055")]
    public void StatusAndColor_CommonCombinations(string status, string color)
    {
        var vm = new KpiCardViewModel { Status = status, Color = color };
        vm.Status.Should().Be(status);
        vm.Color.Should().Be(color);
    }

    [Fact]
    public void Progress_CanBeSetToMax()
    {
        var vm = new KpiCardViewModel { Progress = 100 };
        vm.Progress.Should().Be(100);
    }

    [Fact]
    public void Progress_CanBeSetToZero()
    {
        var vm = new KpiCardViewModel { Progress = 0 };
        vm.Progress.Should().Be(0);
    }

    [Fact]
    public void Progress_CanExceed100()
    {
        var vm = new KpiCardViewModel { Progress = 150 };
        vm.Progress.Should().Be(150);
    }

    [Fact]
    public void Title_CanContainSpecialCharacters()
    {
        var vm = new KpiCardViewModel { Title = "CPU% (avg)" };
        vm.Title.Should().Be("CPU% (avg)");
    }

    [Fact]
    public void Value_CanBeFormattedCurrency()
    {
        var vm = new KpiCardViewModel { Value = "$1,234,567" };
        vm.Value.Should().Be("$1,234,567");
    }

    [Fact]
    public void Change_CanBeNegative()
    {
        var vm = new KpiCardViewModel { Change = "-3.2%" };
        vm.Change.Should().Be("-3.2%");
    }
}
