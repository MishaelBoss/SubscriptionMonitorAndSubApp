using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SubApp.Models;
using SubApp.Scripts;

namespace SubApp.ViewModels.Components;

public partial class ViewSubscriptionUserControlViewModel(Subscription sub) : ViewModelBase
{ 
    public Subscription Sub { get; } = sub;

    [RelayCommand]
    public void Close()
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseSubscriptionDetailsMessage());
    }
}
