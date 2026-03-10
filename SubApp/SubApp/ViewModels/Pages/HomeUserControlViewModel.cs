using System.Collections.Generic;
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
    private const int PageSize = 5;
    [ObservableProperty] private bool _hasMoreData = true;
    private static List<Subscription>? _cache = [];
    [ObservableProperty] private int _countSubscription;

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

        CountSubscription = db.Subscriptions.Count(s => s.IsActive);

        if (_cache.Any())
        {
            Subscriptions = new ObservableCollection<Subscription>(_cache);
            CheckIfMoreDataAvailable();
            return;
        }
        
        var list = db.Subscriptions
            .Include(s => s.Service)
            .Where(s => s.IsActive)
            .OrderBy(s => s.NextPaymentDate)
            .Take(3)
            .ToList();

        foreach (var sub in list)
        {
            Subscriptions.Add(sub);
            _cache.Add(sub);
        }
        
        CheckIfMoreDataAvailable();
    }
    
    [RelayCommand]
    private void LoadMore()
    {
        using var db = new AppDbContext();
        var currentCount = Subscriptions.Count;
        var totalCount = db.Subscriptions.Count(s => s.IsActive);
        var remaining = totalCount - currentCount;
        if (remaining <= 0) return;
        var toTake = (remaining <= PageSize - 1) ? remaining : PageSize;

        var newItems = db.Subscriptions
            .Include(s => s.Service)
            .Where(s => s.IsActive)
            .OrderBy(s => s.NextPaymentDate)
            .Skip(currentCount)
            .Take(toTake)
            .ToList();

        foreach (var item in newItems)
        {
            Subscriptions.Add(item);
            _cache.Add(item);
        }
        
        HasMoreData = Subscriptions.Count < totalCount;
    }
    
    private void CheckIfMoreDataAvailable()
    {
        using var db = new AppDbContext();
        var totalCount = db.Subscriptions.Count(s => s.IsActive);
        HasMoreData = Subscriptions.Count < totalCount;
    }
}
