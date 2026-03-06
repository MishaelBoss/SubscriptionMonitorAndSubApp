using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SubApp.Scripts;

namespace SubApp.ViewModels.Components;

public partial class DashboardUserControlViewModel : ViewModelBase
{
    [RelayCommand]
    public void OpenProfile() 
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseProfileMessage());
    }
}
