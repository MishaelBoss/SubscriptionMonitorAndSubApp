using Avalonia.Controls;
using SubApp.ViewModels.Components;

namespace SubApp.Views.Components;

public partial class ProfileUserControlView : UserControl
{
    public ProfileUserControlView()
    {
        InitializeComponent();
        DataContext = new ProfileUserControlViewModel();
    }
}