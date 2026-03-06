using Avalonia.Controls;
using SubApp.ViewModels.Components;

namespace SubApp.Views.Components;

public partial class DashboardUserControl : UserControl
{
    public DashboardUserControl()
    {
        InitializeComponent();
        DataContext = new DashboardUserControlViewModel();
    }
}