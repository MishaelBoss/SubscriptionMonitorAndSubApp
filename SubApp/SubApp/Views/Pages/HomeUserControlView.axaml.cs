using Avalonia.Controls;
using SubApp.ViewModels.Pages;

namespace SubApp.Views.Pages;

public partial class HomeUserControlView : UserControl
{
    public HomeUserControlView()
    {
        InitializeComponent();
        DataContext = new HomeUserControlViewModel();
    }
}