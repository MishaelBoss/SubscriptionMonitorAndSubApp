using Avalonia.Controls;
using SubApp.ViewModels.Components;

namespace SubApp;

public partial class ViewSubscriptionUserControlView : UserControl
{
    public ViewSubscriptionUserControlView()
    {
        InitializeComponent();
        DataContext = new ViewSubscriptionUserControlViewModel();
    }
}