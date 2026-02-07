using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MIC.Desktop.Avalonia.Views;

public partial class NotificationCenterView : UserControl
{
    public NotificationCenterView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
