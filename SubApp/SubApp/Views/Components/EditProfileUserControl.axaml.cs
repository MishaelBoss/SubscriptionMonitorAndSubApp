using Avalonia.Controls;
using SubApp.ViewModels.Components;

namespace SubApp;

public partial class EditProfileUserControl : UserControl
{
    public EditProfileUserControl()
    {
        InitializeComponent();
        DataContext = new EditProfileUserControlViewModel();
    }
}