using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SubApp.Scripts;
using System.Collections.Generic;

namespace SubApp.ViewModels.Components;

public partial class AddOrEditNewSubscriptionUserControlViewModel : ViewModelBase
{
    public List<string> Service { get; } = [ "Выберите сервис", "FizoVod" ];
    public List<string> PaymentPeriod { get; } = [ "Ежемесячно", "Ежеквартально", "Ежегодно", "Еженедельно", "Свой период" ];
    public List<string> Currency { get; } = [ "₽ Рубль", "$ Долор", "€ Евро" ];
    public List<string> Status { get; } = [ "Активна", "Приостановлена", "Пробный период" ];

    [ObservableProperty] private string _erroredSubscription = string.Empty;
    [ObservableProperty] private string _selectedService = "Выберите сервис";
    [ObservableProperty] private string _subscriptionName = string.Empty;
    [ObservableProperty] private string _selectedPaymentPeriod = "Ежемесячно";
    [ObservableProperty] private int _sum;
    [ObservableProperty] private string _selectedCurrency = "Рубль";
    [ObservableProperty] private string _selectedStatus = "Активна";
    [ObservableProperty] private bool _automaticRenewal = true;
    [ObservableProperty] private string _notes = string.Empty;

    public bool IsActiveConfirmButton
        => true;

    [RelayCommand]
    public void Close() 
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseAddOrEditNewSubscriptionMessage());
    }
}
