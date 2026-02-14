using FluentAssertions;
using MIC.Desktop.Avalonia.ViewModels;

namespace MIC.Tests.Unit.ViewModels;

/// <summary>
/// Tests for the NotificationItem POCO used by MainWindowViewModel.
/// </summary>
public class NotificationItemTests
{
    [Fact]
    public void Defaults_AreCorrect()
    {
        var item = new NotificationItem();

        item.Title.Should().BeEmpty();
        item.Message.Should().BeEmpty();
        item.TimeAgo.Should().BeEmpty();
        item.Type.Should().Be("Info");
    }

    [Fact]
    public void AllProperties_CanBeSet()
    {
        var item = new NotificationItem
        {
            Title = "Alert fired",
            Message = "CPU usage exceeded threshold",
            TimeAgo = "5 minutes ago",
            Type = "Warning"
        };

        item.Title.Should().Be("Alert fired");
        item.Message.Should().Be("CPU usage exceeded threshold");
        item.TimeAgo.Should().Be("5 minutes ago");
        item.Type.Should().Be("Warning");
    }

    [Theory]
    [InlineData("Info")]
    [InlineData("Warning")]
    [InlineData("Error")]
    [InlineData("Success")]
    public void Type_AcceptsVariousValues(string type)
    {
        var item = new NotificationItem { Type = type };
        item.Type.Should().Be(type);
    }

    [Fact]
    public void Title_CanContainSpecialCharacters()
    {
        var item = new NotificationItem { Title = "Alert: CPU > 90% (critical)" };
        item.Title.Should().Contain(">");
    }

    [Fact]
    public void Message_CanBeMultiLine()
    {
        var item = new NotificationItem { Message = "Line 1\nLine 2\nLine 3" };
        item.Message.Should().Contain("\n");
    }

    [Fact]
    public void TimeAgo_CanBeSet()
    {
        var item = new NotificationItem { TimeAgo = "just now" };
        item.TimeAgo.Should().Be("just now");
    }
}
