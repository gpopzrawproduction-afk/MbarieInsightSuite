using Avalonia.Controls;
using MIC.Desktop.Avalonia.ViewModels;

namespace MIC.Desktop.Avalonia.Views;

public partial class ReportsView : UserControl
{
    public ReportsView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is ReportsViewModel vm)
        {
            vm.LoadReportsCommand.Execute(null);
        }
    }
}
