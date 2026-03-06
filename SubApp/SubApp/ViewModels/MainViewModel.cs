using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using SubApp.Scripts;
using SubApp.ViewModels.Components;
using SubApp.ViewModels.Pages;

namespace SubApp.ViewModels;

public partial class MainViewModel : ViewModelBase, 
    IRecipient<OpenHomePageMessage>,
    IRecipient<OpenSubscriptionsPageMessage>, 
    IRecipient<OpenEmailsPageMessage>, 
    IRecipient<OpenAnalyticsPageMessage>,
    IRecipient<OpenOrCloseProfileMessage>,
    IRecipient<OpenOrCloseLoginMessage>,
    IRecipient<OpenOrCloseRegistrationMessage>
{
    [ObservableProperty] private ViewModelBase? _currentPage;
    [ObservableProperty] private ViewModelBase? _overlayContent;

    private readonly HomeUserControlViewModel _homeUserControlView = new();
    private readonly SubscriptionsUserControlViewModel _subscriptionsUserControlViewModel = new();
    private readonly EmailsUserControlViewModel _emailsUserControlViewModel = new();
    private readonly AnalyticsUserControlViewModel _analyticsUserControlViewModel = new();

    private readonly ProfileUserControlViewModel _profileUserControlViewModel = new();
    private readonly LoginUserControlViewModel _loginUserControlViewModel = new();
    private readonly RegistrationUserControlViewModel _registrationUserControlViewModel = new();

    public MainViewModel() 
    {
        CurrentPage = _homeUserControlView;

        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    public void Receive(OpenHomePageMessage message) 
    {
        CurrentPage = _homeUserControlView;
    }

    public void Receive(OpenSubscriptionsPageMessage message)
    {
        CurrentPage = _subscriptionsUserControlViewModel;
    }

    public void Receive(OpenEmailsPageMessage message)
    {
        CurrentPage = _emailsUserControlViewModel;
    }

    public void Receive(OpenAnalyticsPageMessage message)
    {
        CurrentPage = _analyticsUserControlViewModel;
    }

    public void Receive(OpenOrCloseProfileMessage message) 
    {
        OverlayContent = OverlayContent == null ? _profileUserControlViewModel : null;
    }

    public void Receive(OpenOrCloseLoginMessage message)
    {
        if (OverlayContent is LoginUserControlViewModel) OverlayContent = null;
        else OverlayContent = _loginUserControlViewModel;
    }

    public void Receive(OpenOrCloseRegistrationMessage message)
    {
        if (OverlayContent is RegistrationUserControlViewModel) OverlayContent = null;
        else OverlayContent = _registrationUserControlViewModel;
    }

    ~MainViewModel() 
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
}
