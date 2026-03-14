using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using SubApp.Data;
using SubApp.Models;
using SubApp.Scripts;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SubApp.ViewModels.Pages;

public partial class HomeUserControlViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<Subscription> _subscriptions = [];
    [ObservableProperty] private ObservableCollection<ParsedEmail> _recentPayments = [];
    
    private const int PageSize = 5;
    [ObservableProperty] private bool _hasMoreDataSubscription = true;
    [ObservableProperty] private bool _hasMoreDataEmail = true;
    private static readonly List<Subscription>? CacheSubscription = [];
    private static readonly List<ParsedEmail>? CacheEmail = [];
    [ObservableProperty] private int _countSubscription;
    [ObservableProperty] private int _countEmail;
    [ObservableProperty] private double _totalMonthlyCost;
    [ObservableProperty] private double _totalYearlyCost;
    [ObservableProperty] private string? _nextPaymentSummary;

    [RelayCommand]
    public void OpenAddSubscription() 
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseAddOrEditNewSubscriptionMessage());
    }

    public HomeUserControlViewModel()
    {
        RecentPayments.Clear();

        Task.Run(async () => { await InitializationAsync(); });
    }

    private async Task InitializationAsync()
    {
        await AuthService.TryAutoLoginAsync();
        
        if(AuthService.CurrentSession?.Id == 0) return;
        
        await using var db = new AppDbContext();
        
        await LoadRecent(db);
        LoadSubscriptions(db);
    }

    private async Task LoadRecent(AppDbContext db)
    {
        var all = db.ParsedEmails
            .Where(s => AuthService.CurrentSession != null && s.Mailbox.UserId == AuthService.CurrentSession.Id)
            .ToList();
        
        CountEmail = all.Count;
        
        var recent = await db.ParsedEmails
            .Where(e => AuthService.CurrentSession != null && e.Mailbox.UserId == AuthService.CurrentSession.Id)
            .OrderByDescending(e => e.ReceivedDate)
            .Take(PageSize)
            .ToListAsync();
        
        if (CacheEmail != null && CacheEmail.Count != 0)
        {
            RecentPayments = new ObservableCollection<ParsedEmail>(CacheEmail);
            HasMoreDataEmail = RecentPayments.Count < CountEmail;
            return;
        }
        
        foreach (var email in recent)
        {
            RecentPayments.Add(email);
            CacheEmail?.Add(email);
        }
        
        HasMoreDataEmail = RecentPayments.Count < CountEmail;
    }
    
    [RelayCommand]
    private void LoadMoreEmails()
    {
        var currentCount = RecentPayments.Count;
        var totalCount = CountEmail;
        
        var remaining = totalCount - currentCount;
        if (remaining <= 0) return;
        
        using var db = new AppDbContext();
        var toTake = (remaining <= PageSize - 1) ? remaining : PageSize;

        var newItems = db.ParsedEmails
            .Where(e => AuthService.CurrentSession != null && e.Mailbox.UserId == AuthService.CurrentSession.Id)
            .OrderBy(s => s.ReceivedDate)
            .Include(e => e.Mailbox)
            .Skip(currentCount)
            .Take(toTake)
            .ToList();

        foreach (var item in newItems)
        {
            RecentPayments.Add(item);
            CacheEmail?.Add(item);
        }
        
        HasMoreDataEmail = RecentPayments.Count < totalCount;
    }

    private void LoadSubscriptions(AppDbContext db) 
    {
        var allActive = db.Subscriptions
            .Where(s => s.IsActive)
            .Where(s => AuthService.CurrentSession != null && s.UserId == AuthService.CurrentSession.Id)
            .Include(s => s.Service).ToList();
        
        CountSubscription = allActive.Count;
        TotalMonthlyCost = allActive.Sum(CalculateIndividualMonthlyCost);
        TotalYearlyCost = allActive.Sum(CalculateIndividualYearlyCost);
        
        var today = DateTime.Today;
        var nextWeek = today.AddDays(7);
        
        var upcomingPaymentsCount = allActive
            .Count(s => s.NextPaymentDate >= today && s.NextPaymentDate <= nextWeek);
        
        NextPaymentSummary = upcomingPaymentsCount.ToString();

        if (CacheSubscription != null && CacheSubscription.Count != 0)
        {
            Subscriptions = new ObservableCollection<Subscription>(CacheSubscription);
            HasMoreDataSubscription = Subscriptions.Count < CountSubscription;
            return;
        }

        var initialList = allActive.OrderBy(s => s.NextPaymentDate).Take(3).ToList();
        foreach (var sub in initialList)
        {
            Subscriptions.Add(sub);
            CacheSubscription?.Add(sub);
        }
        
        HasMoreDataSubscription = Subscriptions.Count < CountSubscription;
    }
    
    [RelayCommand]
    private void LoadMoreSubscriptions()
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
            CacheSubscription?.Add(item);
        }
        
        HasMoreDataSubscription = Subscriptions.Count < totalCount;
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
