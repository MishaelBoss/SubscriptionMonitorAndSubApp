using Avalonia.Controls;
using SubApp.ViewModels.Components;

namespace SubApp.Views.Components;

public partial class RegistrationUserControlView : UserControl
{
    public RegistrationUserControlView()
    {
        InitializeComponent();
        DataContext = new RegistrationUserControlViewModel();
    }
}