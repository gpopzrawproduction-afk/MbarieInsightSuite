using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using FluentAssertions;
using MIC.Desktop.Avalonia.Services;

namespace MIC.Tests.Unit.Services;

public class AvaloniaUiDispatcherTests
{
    [Fact]
    public async Task RunAsync_WhenActionIsNull_ThrowsArgumentNullException()
    {
        var dispatcher = AvaloniaUiDispatcher.Instance;

        Func<Task> act = () => dispatcher.RunAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RunAsync_ExecutesActionOnUiThread()
    {
        var dispatcher = AvaloniaUiDispatcher.Instance;
        var executedOnUiThread = false;

        var runTask = dispatcher.RunAsync(() =>
        {
            executedOnUiThread = Dispatcher.UIThread.CheckAccess();
        });

        await PumpDispatcherUntilCompletedAsync(runTask, TimeSpan.FromSeconds(1));

        executedOnUiThread.Should().BeTrue();
    }

    private static async Task PumpDispatcherUntilCompletedAsync(Task task, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!task.IsCompleted && DateTime.UtcNow < deadline)
        {
            Dispatcher.UIThread.RunJobs();
            await Task.Delay(10);
        }

        if (!task.IsCompleted)
        {
            throw new TimeoutException("Dispatcher task did not complete within the allotted time.");
        }

        await task;
    }
}
