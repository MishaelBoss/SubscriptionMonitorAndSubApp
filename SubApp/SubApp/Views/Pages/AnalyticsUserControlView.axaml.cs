using Avalonia.Controls;
using SubApp.ViewModels.Pages;

namespace SubApp.Views.Pages;

public partial class AnalyticsUserControlView : UserControl
{
    public AnalyticsUserControlView()
    {
        InitializeComponent();
        DataContext = new AnalyticsUserControlViewModel();
    }
}