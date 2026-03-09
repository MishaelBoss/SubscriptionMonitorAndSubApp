using System;
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

namespace SubApp.ViewModels.Pages
{
    public partial class SubscriptionsUserControlViewModel : ViewModelBase
    {
        [ObservableProperty] private ObservableCollection<Subscription> _subscriptions = [];
        public List<object> Status { get; } = ["Все", .. Enum.GetValues<SubscriptionStatus>().Cast<object>()];
        
        [ObservableProperty] private object _selectedStatus = "Все";

        public SubscriptionsUserControlViewModel()
        {
            Filter();
        }

        [RelayCommand]
        public void OpenDetails(Subscription? sub)
        {
            if(sub == null) return;
            WeakReferenceMessenger.Default.Send(new OpenOrCloseSubscriptionDetailsMessage(sub));
        }
        
        partial void OnSelectedStatusChanged(object value) 
            => Filter();

        private void Filter()
        {
            using var db = new AppDbContext();
            var query = db.Subscriptions.Include(s => s.Service).AsQueryable();

            if (SelectedStatus is SubscriptionStatus status) query = query.Where(s => s.Status == status.ToString());
            
            Subscriptions = new ObservableCollection<Subscription>(query.ToList());
        }
    }
}
