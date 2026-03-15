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
            if (AuthService.CurrentSession == null) return;

            try
            {
                var api = new ApiService(AuthService.CurrentSession.Token);
                var allSubscriptions = await api.GetSubscriptionsAsync();

                var statusStr = SelectedStatus.ToString();
                var today = DateTime.Today;

                IEnumerable<Subscription> filteredQuery = allSubscriptions;
                switch (statusStr)
                {
                    case "Просроченные":
                        filteredQuery = allSubscriptions.Where(s => 
                            s.Status.ToLower() == "active" && s.NextPaymentDate.Date < today);
                        break;
                    case "Активные":
                        filteredQuery = allSubscriptions.Where(s => 
                            s.Status.ToLower() == "active" && s.NextPaymentDate.Date >= today);
                        break;
                    case "Приостановленные":
                        filteredQuery = allSubscriptions.Where(s => s.Status.ToLower() == "paused");
                        break;
                    case "Отмененные":
                        filteredQuery = allSubscriptions.Where(s => s.Status.ToLower() == "cancelled");
                        break;
                    case "Все":
                    default:
                        break;
                }
                
                Dispatcher.UIThread.Post(() => {
                    Subscriptions = new ObservableCollection<Subscription>(filteredQuery);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        
        ~SubscriptionsUserControlViewModel()
        {
            WeakReferenceMessenger.Default.Unregister<RefreshSubscriptionMessage>(this);
        }
    }
}
