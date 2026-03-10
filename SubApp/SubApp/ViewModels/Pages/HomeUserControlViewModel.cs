using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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
    [ObservableProperty] private double _totalMonthlyCost;
    [ObservableProperty] private double _totalYearlyCost;
    [ObservableProperty] private string _nextPaymentSummary;

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

        var allActive = db.Subscriptions.Where(s => s.IsActive).Include(s => s.Service).ToList();
        CountSubscription = allActive.Count;
        TotalMonthlyCost = allActive.Sum(CalculateIndividualMonthlyCost);
        TotalYearlyCost = allActive.Sum(CalculateIndividualYearlyCost);
        
        // var nextSub = allActive
        //     .Where(s => s.NextPaymentDate >= DateTime.Now)
        //     .OrderBy(s => s.NextPaymentDate)
        //     .FirstOrDefault();
        
        // NextPaymentSummary = nextSub != null 
        //     ? $"({nextSub.Amount:N0} {})" : "0";

        if (_cache != null && _cache.Count != 0)
        {
            Subscriptions = new ObservableCollection<Subscription>(_cache);
            HasMoreData = Subscriptions.Count < CountSubscription;
            return;
        }

        var initialList = allActive.OrderBy(s => s.NextPaymentDate).Take(3).ToList();
        foreach (var sub in initialList)
        {
            Subscriptions.Add(sub);
            _cache?.Add(sub);
        }
        
        HasMoreData = Subscriptions.Count < CountSubscription;
    }
    
    [RelayCommand]
    private void LoadMore()
    {
        var currentCount = Subscriptions.Count;
        var totalCount = CountSubscription;
        
        var remaining = totalCount - currentCount;
        if (remaining <= 0) return;
        
        using var db = new AppDbContext();
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
            _cache?.Add(item);
        }
        
        HasMoreData = Subscriptions.Count < totalCount;
    }
    
    private double CalculateIndividualMonthlyCost(Subscription sub)
    {
        var amountValue = (double)sub.Amount; 

        return sub.BillingCycle switch
        {
            "monthly" => amountValue,
            "yearly"  => amountValue / 12,
            "quarterly" => amountValue / 3,
            "weekly"  => amountValue * 4.33,
            "custom" when sub.BillingCycleDays > 0 => (amountValue / sub.BillingCycleDays) * 30,
            _ => 0
        };
    }
    
    private double CalculateIndividualYearlyCost(Subscription sub)
    {
        var monthly = CalculateIndividualMonthlyCost(sub); 
        return monthly * 12;
    }
}
