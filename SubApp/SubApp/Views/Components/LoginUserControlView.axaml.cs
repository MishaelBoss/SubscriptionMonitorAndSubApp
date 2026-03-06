using Avalonia.Controls;
using SubApp.ViewModels.Components;

namespace SubApp.Views.Components;

public partial class LoginUserControlView : UserControl
{
    public LoginUserControlView()
    {
        InitializeComponent();
        DataContext = new LoginUserControlViewModel();
    }
}