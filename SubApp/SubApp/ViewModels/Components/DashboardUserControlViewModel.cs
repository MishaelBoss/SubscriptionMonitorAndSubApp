using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SubApp.Scripts;

namespace SubApp.ViewModels.Components;

public partial class DashboardUserControlViewModel : ViewModelBase, IRecipient<UserLoggedInMessage
>
{
    [ObservableProperty] private string _username = string.Empty;

    public DashboardUserControlViewModel()
    {
        WeakReferenceMessenger.Default.Register(this);
        
        UpdateUsername();
    }

    public void Receive(UserLoggedInMessage message)
    {
        UpdateUsername();
    }

    private async void UpdateUsername()
    {
        await AuthService.TryAutoLoginAsync();
        Username = AuthService.CurrentSession?.Username ?? string.Empty;
    }

    [RelayCommand]
    public void OpenProfile() 
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseProfileMessage());
    }
    
    ~DashboardUserControlViewModel()
    {
        WeakReferenceMessenger.Default.Unregister<UserLoggedInMessage>(this);
    }
}
