using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SubApp.Scripts;

namespace SubApp.ViewModels.Pages;

public partial class HomeUserControlViewModel : ViewModelBase
{
    [RelayCommand]
    public void OpenAddSubscription() 
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseAddOrEditNewSubscriptionMessage());
    }
}
