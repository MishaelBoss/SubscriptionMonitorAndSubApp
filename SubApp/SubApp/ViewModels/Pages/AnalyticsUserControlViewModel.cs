using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using SubApp.Data;
using SubApp.Models;
using SubApp.Scripts;

namespace SubApp.ViewModels.Pages
{
    public partial class AnalyticsUserControlViewModel : ViewModelBase
    {
        [ObservableProperty] private int _countSubscription;
        [ObservableProperty] private int _countActiveSubscription;
        [ObservableProperty] private double _totalMonthlyCost;
        [ObservableProperty] private double _totalYearlyCost;
        
        public AnalyticsUserControlViewModel()
        {
            _ = LoadSubscriptions();
        }
        
        private async Task LoadSubscriptions() 
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
                
                var allActive = (await api.GetSubscriptionsAsync()).ToList();
                var activeSubs = allActive.Where(s => s.IsActive).ToList();
                
                var count = allActive.Count;
                var activeCount = activeSubs.Count;
                var monthly = activeSubs.Sum(CalculateIndividualMonthlyCost);
                var yearly = activeSubs.Sum(CalculateIndividualYearlyCost);

                Dispatcher.UIThread.Post(() => 
                {
                    CountSubscription = count;
                    CountActiveSubscription = activeCount;
                    TotalMonthlyCost = monthly;
                    TotalYearlyCost = yearly;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
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
}
