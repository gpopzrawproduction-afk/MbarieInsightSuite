using Avalonia.Controls;
using MIC.Desktop.Avalonia.ViewModels;

namespace MIC.Desktop.Avalonia.Views;

/// <summary>
/// PredictionView - AI-powered metric forecasting with glassmorphic design.
/// </summary>
public partial class PredictionView : UserControl
{
    public PredictionView()
    {
        InitializeComponent();

        DataContext = new ForecastingViewModel(
            Program.ServiceProvider?.GetRequiredService<MediatR.IMediator>()
                ?? throw new InvalidOperationException("IMediator not registered"),
            Program.ServiceProvider?.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ForecastingViewModel>>()
                ?? throw new InvalidOperationException("ILogger<ForecastingViewModel> not registered"));
    }
}
