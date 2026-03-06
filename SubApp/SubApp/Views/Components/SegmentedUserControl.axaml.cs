using Avalonia.Controls;
using SubApp.ViewModels.Components;

namespace SubApp.Views.Components;

public partial class SegmentedUserControl : UserControl
{
    public SegmentedUserControl()
    {
        InitializeComponent();
        DataContext = new SegmentedUserControlViewModel();
    }
}