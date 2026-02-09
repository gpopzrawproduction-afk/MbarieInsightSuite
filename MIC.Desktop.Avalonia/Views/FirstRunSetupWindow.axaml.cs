using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MIC.Desktop.Avalonia.ViewModels;

namespace MIC.Desktop.Avalonia.Views;

public partial class FirstRunSetupWindow : Window
{
    public FirstRunSetupWindow()
    {
        InitializeComponent();
    }

    public FirstRunSetupWindow(FirstRunSetupViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
