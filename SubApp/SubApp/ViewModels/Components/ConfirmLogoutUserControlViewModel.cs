using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SubApp.Scripts;

namespace SubApp.ViewModels.Components;

public partial class ConfirmLogoutUserControlViewModel : ViewModelBase
{
    [RelayCommand]
    public void Logou()
    {
        AuthService.Logout();
    }

    [RelayCommand]
    public void Close()
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseConfirmLogoutMessage());
    }
}