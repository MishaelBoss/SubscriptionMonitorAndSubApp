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
    IRecipient<OpenOrCloseRegistrationMessage>,
    IRecipient<OpenOrCloseAddOrEditEmailMessage>,
    IRecipient<OpenOrCloseAddOrEditNewSubscriptionMessage>
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
    private readonly AddOrEditMailboxUserControlViewModel _addOrEditMailboxUserControlViewModel = new();
    private readonly AddOrEditNewSubscriptionUserControlViewModel _addOrEditNewSubscriptionUserControlView = new();

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
        OverlayContent = OverlayContent is LoginUserControlViewModel ? null : _loginUserControlViewModel;
    }

    public void Receive(OpenOrCloseRegistrationMessage message)
    {
        OverlayContent = OverlayContent is RegistrationUserControlViewModel ? null : _registrationUserControlViewModel;
    }
    
    public void Receive(OpenOrCloseAddOrEditEmailMessage message)
    {
        OverlayContent = OverlayContent is AddOrEditMailboxUserControlViewModel ? null : _addOrEditMailboxUserControlViewModel;
    }

    public void Receive(OpenOrCloseAddOrEditNewSubscriptionMessage message)
    {
        OverlayContent = OverlayContent is AddOrEditNewSubscriptionUserControlViewModel ? null : _addOrEditNewSubscriptionUserControlView;
    }

    ~MainViewModel() 
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
}
