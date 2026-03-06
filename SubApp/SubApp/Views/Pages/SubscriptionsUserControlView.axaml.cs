using Avalonia.Controls;
using SubApp.ViewModels.Pages;

namespace SubApp.Views.Pages;

public partial class SubscriptionsUserControlView : UserControl
{
    public SubscriptionsUserControlView()
    {
        InitializeComponent();
        DataContext = new SubscriptionsUserControlViewModel();
    }
}