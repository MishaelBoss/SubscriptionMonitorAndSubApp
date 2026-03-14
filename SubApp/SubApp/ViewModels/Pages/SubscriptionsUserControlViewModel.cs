using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using SubApp.Data;
using SubApp.Models;
using SubApp.Scripts;

namespace SubApp.ViewModels.Pages
{
    public partial class SubscriptionsUserControlViewModel : ViewModelBase, IRecipient<RefreshSubscriptionMessage>
    {
        [ObservableProperty] private ObservableCollection<Subscription> _subscriptions = [];
        // public List<object> Status { get; } = ["Все", .. Enum.GetValues<SubscriptionStatus>().Cast<object>()];
        public List<string> Status { get; } = [ "Все", "Активные", "Просроченные", "Приостановленные", "Отмененные" ];

        
        [ObservableProperty] private object _selectedStatus = "Все";

        public SubscriptionsUserControlViewModel()
        {
            WeakReferenceMessenger.Default.Register(this);
            
            _ = Filter();
        }
        
        public void Receive(RefreshSubscriptionMessage message)
        {
            _ = Filter();
        }

        [RelayCommand]
        public void OpenDetails(Subscription? sub)
        {
            if(sub == null) return;
            WeakReferenceMessenger.Default.Send(new OpenOrCloseSubscriptionDetailsMessage(sub));
        }
        
        partial void OnSelectedStatusChanged(object value) 
            => _ = Filter();

        private async Task Filter()
        {
            await AuthService.TryAutoLoginAsync();
            var userId = AuthService.CurrentSession?.Id;
    
            if (userId is null or 0) return;

            await Task.Run(async () => 
            {
                await using var db = new AppDbContext();
                var query = db.Subscriptions.Include(s => s.Service).Where(s => s.UserId == userId);

                var statusStr = SelectedStatus.ToString();
                var today = DateTime.Today;

                switch (statusStr)
                {
                    case "Просроченные":
                        query = query.Where(s => s.Status.ToLower() == "active" && s.NextPaymentDate < today);
                        break;
                    case "Активные":
                        query = query.Where(s => s.Status.ToLower() == "active" && s.NextPaymentDate >= today);
                        break;
                    case "Приостановленные":
                        query = query.Where(s => s.Status.ToLower() == "paused");
                        break;
                    case "Отмененные":
                        query = query.Where(s => s.Status.ToLower() == "cancelled");
                        break;
                    case "Все":
                    default:
                        break;
                }

                var list = await query.ToListAsync();
        
                Dispatcher.UIThread.Post(() => {
                    Subscriptions = new ObservableCollection<Subscription>(list);
                });
            });
        }
        
        ~SubscriptionsUserControlViewModel()
        {
            WeakReferenceMessenger.Default.Unregister<RefreshSubscriptionMessage>(this);
        }
    }
}
