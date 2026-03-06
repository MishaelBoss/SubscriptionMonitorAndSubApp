using Avalonia.Controls;
using SubApp.ViewModels.Pages;

namespace SubApp.Views.Pages;

public partial class EmailsUserControlView : UserControl
{
    public EmailsUserControlView()
    {
        InitializeComponent();
        DataContext = new EmailsUserControlViewModel();
    }
}