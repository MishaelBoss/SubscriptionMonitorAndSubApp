using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SubApp.ViewModels.Pages
{
    public partial class SubscriptionsUserControlViewModel : ViewModelBase
    {
        public List<string> Status { get; } = ["Все статусы", "Активные", "Просроченные", "Приостанновленые", "отмененные" ];
        [ObservableProperty] private string _selectedStatus = "Все статусы";
    }
}
