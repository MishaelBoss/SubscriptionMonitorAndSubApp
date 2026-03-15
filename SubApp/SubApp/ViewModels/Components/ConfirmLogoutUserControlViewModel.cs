using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SubApp.Scripts;

namespace SubApp.ViewModels.Components;

public partial class ConfirmLogoutUserControlViewModel : ViewModelBase
{
    [RelayCommand]
    public void Logout()
    {
        AuthService.Logout();
        WeakReferenceMessenger.Default.Send(new OpenOrCloseConfirmLogoutMessage());
    }

    [RelayCommand]
    public void Close()
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseConfirmLogoutMessage());
    }
}