using Avalonia.Controls;
using SubApp.ViewModels.Components;

namespace SubApp.Views.Components;

public partial class MyEmailMessagesUserControlView : UserControl
{
    public MyEmailMessagesUserControlView()
    {
        InitializeComponent();
        DataContext = new MyEmailMessagesUserControlViewModel();
    }
}