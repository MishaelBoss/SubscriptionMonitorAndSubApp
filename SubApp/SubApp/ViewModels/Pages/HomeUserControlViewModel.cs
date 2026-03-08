using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using SubApp.Data;
using SubApp.Models;
using SubApp.Scripts;

namespace SubApp.ViewModels.Pages;

public partial class HomeUserControlViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<Subscription> _subscriptions = [];
        
    [RelayCommand]
    public void OpenAddSubscription() 
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseAddOrEditNewSubscriptionMessage());
    }

    public HomeUserControlViewModel()
    {
        LoadSubscriptions();
    }

    private void LoadSubscriptions() 
    {
        using var db = new AppDbContext();
        
        var data = db.Subscriptions
            .Include(s => s.Service)
            .OrderBy(s => s.NextPaymentDate)
            .ToList();

        Subscriptions = new ObservableCollection<Subscription>(data);
    }
}
