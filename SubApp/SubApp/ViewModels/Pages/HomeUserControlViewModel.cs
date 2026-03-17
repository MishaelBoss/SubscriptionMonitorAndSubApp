using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SubApp.Data;
using SubApp.Models;
using SubApp.Scripts;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace SubApp.ViewModels.Pages;

public partial class HomeUserControlViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<Subscription> _subscriptions = [];
    [ObservableProperty] private ObservableCollection<ParsedEmail> _recentPayments = [];
    
    private const int PageSize = 5;
    [ObservableProperty] private bool _hasMoreDataSubscription = true;
    [ObservableProperty] private bool _hasMoreDataEmail = true;
    [ObservableProperty] private int _countSubscription;
    [ObservableProperty] private int _countEmail;
    [ObservableProperty] private double _totalMonthlyCost;
    [ObservableProperty] private double _totalYearlyCost;
    [ObservableProperty] private string? _nextPaymentSummary;
    
    [RelayCommand]
    public void OpenAddSubscription() 
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseAddOrEditSubscriptionMessage());
    }

    public HomeUserControlViewModel()
    {
        // Task.Run(async () => { await InitializationAsync(); });
        _ = InitializationAsync();
    }

    private async Task InitializationAsync()
    {
        try 
        {
            await AuthService.TryAutoLoginAsync();
            
            if(AuthService.CurrentSession == null) 
            {
                Console.WriteLine("DEBUG: Сессия пустая, загрузка отменена.");
                return;
            }

            var api = new ApiService(AuthService.CurrentSession.Token);
            
            await LoadRecent(api);
            await LoadSubscriptions(api);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"КРИТИЧЕСКАЯ ОШИБКА: {ex.Message}");
        }
    }
    
    private async Task LoadRecent(ApiService api)
    {
        var allEmails = await api.GetParsedEmailsAsync(); 
        var userId = AuthService.CurrentSession?.Id;
        
        var userEmails = allEmails
            .Where(e => (e.UserId == userId || userId == 0) && !string.IsNullOrEmpty(e.ServiceName)) 
            .GroupBy(e => e.MessageId)
            .Select(g => g.First())
            .OrderByDescending(e => e.ReceivedDate)
            .ToList();

        var totalCount = userEmails.Count;
        var recent = userEmails.Take(PageSize).ToList();
        
        Dispatcher.UIThread.Post(() => 
        {
            CountEmail = totalCount;
            RecentPayments.Clear();
            foreach (var email in recent) RecentPayments.Add(email);
            HasMoreDataEmail = RecentPayments.Count < CountEmail;
        });
    }
    
    [RelayCommand]
    private async Task LoadMoreEmails()
    {
        if (RecentPayments.Count >= CountEmail) return;

        var api = new ApiService(AuthService.CurrentSession.Token);
        var allEmails = await api.GetParsedEmailsAsync(); 

        var newItems = allEmails
            .Where(e => e.UserId == AuthService.CurrentSession.Id || AuthService.CurrentSession.Id == 0)
            .OrderByDescending(e => e.ReceivedDate)
            .Skip(RecentPayments.Count)
            .Take(PageSize)
            .ToList();

        Dispatcher.UIThread.Post(() => {
            foreach (var item in newItems) RecentPayments.Add(item);
            HasMoreDataEmail = RecentPayments.Count < CountEmail;
        });
    }


    private async Task LoadSubscriptions(ApiService api) 
    {
        var allActive = (await api.GetSubscriptionsAsync()).ToList();
    
        var totalCount = allActive.Count;
        var monthly = allActive.Sum(CalculateIndividualMonthlyCost);
        var yearly = allActive.Sum(CalculateIndividualYearlyCost);
        var initialList = allActive.OrderBy(s => s.NextPaymentDate).Take(3).ToList();
        
        var today = DateTime.Today;
        var nextWeek = today.AddDays(7);
        
        var upcomingPaymentsCount = allActive
            .Count(s => s.NextPaymentDate >= today && s.NextPaymentDate <= nextWeek);

        NextPaymentSummary = upcomingPaymentsCount.ToString();

        Dispatcher.UIThread.Post(() => 
        {
            CountSubscription = totalCount;
            TotalMonthlyCost = monthly;
            TotalYearlyCost = yearly;
            Subscriptions.Clear();
            foreach (var sub in initialList) Subscriptions.Add(sub);
            HasMoreDataSubscription = Subscriptions.Count < CountSubscription;
        });
    }
    
    [RelayCommand]
    private async Task LoadMoreSubscriptions()
    {
        if (Subscriptions.Count >= CountSubscription) return;

        var api = new ApiService(AuthService.CurrentSession.Token);
        var allSubscriptions = await api.GetSubscriptionsAsync();

        var newItems = allSubscriptions
            .OrderBy(s => s.NextPaymentDate)
            .Skip(Subscriptions.Count)
            .Take(PageSize)
            .ToList();
        
        Dispatcher.UIThread.Post(() =>
        {
            foreach (var item in newItems) Subscriptions.Add(item);
            HasMoreDataSubscription = Subscriptions.Count < CountSubscription;
        });
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
