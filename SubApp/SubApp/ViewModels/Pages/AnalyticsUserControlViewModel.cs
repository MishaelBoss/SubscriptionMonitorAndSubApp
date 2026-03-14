using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
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
            await AuthService.TryAutoLoginAsync();
            var userId = AuthService.CurrentSession?.Id;
            if (userId is null or 0) return;

            await Task.Run(async () => 
            {
                await using var db = new AppDbContext();
            
                var allSubscriptions = await db.Subscriptions
                    .Where(s => s.UserId == userId)
                    .Include(s => s.Service)
                    .ToListAsync();

                var activeSubs = allSubscriptions.Where(s => s.IsActive).ToList();
                
                var count = allSubscriptions.Count;
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
}
