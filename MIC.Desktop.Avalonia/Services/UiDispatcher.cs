using System;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace MIC.Desktop.Avalonia.Services;

public interface IUiDispatcher
{
    Task RunAsync(Action action);
}

public sealed class AvaloniaUiDispatcher : IUiDispatcher
{
    public static AvaloniaUiDispatcher Instance { get; } = new();

    private AvaloniaUiDispatcher()
    {
    }

    public async Task RunAsync(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        await Dispatcher.UIThread.InvokeAsync(action);
    }
}
