using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SubApp.Scripts;

namespace SubApp.ViewModels.Components;

public partial class ProfileUserControlViewModel : ViewModelBase
{
    [RelayCommand]
    public void CloseProfile()
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseProfileMessage());
    }

    [RelayCommand]
    public void Logout()
    {
        AuthService.Logout();
    }
}
