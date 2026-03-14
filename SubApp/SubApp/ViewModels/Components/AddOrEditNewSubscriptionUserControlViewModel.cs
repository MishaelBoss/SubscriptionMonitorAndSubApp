using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SubApp.Data;
using SubApp.Models;
using SubApp.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SubApp.ViewModels.Components;

public partial class AddOrEditNewSubscriptionUserControlViewModel : ViewModelBase
{
    [ObservableProperty] private List<Service> _services = [];
    
    public List<BillingCycle> PaymentPeriods { get; } = [.. Enum.GetValues<BillingCycle>()];
    public List<Currency> Currencies { get; } = [.. Enum.GetValues<Currency>()];
    public List<SubscriptionStatus> Status { get; } = 
    [ 
        SubscriptionStatus.Active, 
        SubscriptionStatus.Paused, 
        SubscriptionStatus.Trial 
    ];
    
    // public List<string> Service { get; } = [ "Выберите сервис", "FizoVod" ];
    // public List<string> PaymentPeriod { get; } = [ "Ежемесячно", "Ежеквартально", "Ежегодно", "Еженедельно", "Свой период" ];
    // public List<string> Currency { get; } = [ "₽ Рубль", "$ Долор", "€ Евро" ];
    // public List<string> CurrencyList { get; } = [ "RUB", "USD", "EUR" ];
    // public List<string> Status { get; } = [ "Активна", "Приостановлена", "Пробный период" ];

    [ObservableProperty] private string _erroredSubscription = string.Empty;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))] private Service? _selectedService;
    [ObservableProperty] private string _subscriptionName = string.Empty;
    [ObservableProperty] private BillingCycle _selectedPaymentPeriod = BillingCycle.Monthly;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))] private decimal? _sum;
    [ObservableProperty] private Currency _selectedCurrency = Currency.RUB;
    [ObservableProperty] private SubscriptionStatus _selectedStatus = SubscriptionStatus.Active;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))] private DateTime? _startDate = DateTime.UtcNow;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))] private DateTime? _nextPaymentDate;
    [ObservableProperty] private int? _daysPeriod = 30;
    [ObservableProperty] private bool _automaticRenewal = true;
    [ObservableProperty] private string _notes = string.Empty;

    public bool IsActiveConfirmButton =>
        SelectedService != null &&
        Sum > 0 &&
        StartDate != null &&
        NextPaymentDate != null;

    public AddOrEditNewSubscriptionUserControlViewModel()
    {
        ClearForm();
        LoadData();
    }
    
    private void LoadData()
    {
        using var db = new AppDbContext();
        Services = [.. db.Services.Where(s => s.IsActive)];
    }
    
    [RelayCommand]
    public async Task Save()
    {
        await AuthService.TryAutoLoginAsync();
        
        if(AuthService.CurrentSession?.Id == 0 || AuthService.CurrentSession == null) return;
        
        Console.WriteLine($"Начало записи {SubscriptionName}");
        
        try
        {
            if (SelectedService == null || Sum == null) return;

            await using var db = new AppDbContext();
        
            var newSub = new Subscription
            {
                ServiceId = SelectedService.Id,
                Name = string.IsNullOrWhiteSpace(SubscriptionName) ? SelectedService.Name : SubscriptionName,
                Amount = Sum ?? 0,
                Currency = SelectedCurrency.ToString(), 
                BillingCycle = SelectedPaymentPeriod.ToString(),
                Status = SelectedStatus.ToString(),
                StartDate = StartDate ?? DateTime.Now,
                NextPaymentDate = NextPaymentDate ?? DateTime.Now,
                AutoRenew = AutomaticRenewal,
                Notes = Notes,
                UserId = (int)AuthService.CurrentSession.Id,
                Uuid = Guid.NewGuid(),
                LastChecked = DateTime.Now,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsActive = true,
                BillingCycleDays = DaysPeriod ?? 30
            };

            db.Subscriptions.Add(newSub);
            await db.SaveChangesAsync();
        
            Close();
            ClearForm();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
    
    [RelayCommand]
    public void Close() 
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseAddOrEditNewSubscriptionMessage());
    }

    private void ClearForm() 
    {
        ErroredSubscription = string.Empty;
        SelectedService = null;
        SubscriptionName = string.Empty;
        SelectedPaymentPeriod = BillingCycle.Monthly;
        Sum = 0;
        SelectedCurrency = Currency.RUB;
        SelectedStatus = SubscriptionStatus.Active;
        StartDate = DateTime.UtcNow;
        NextPaymentDate = null;
        DaysPeriod = 30;
        AutomaticRenewal = true;
        Notes = string.Empty;
    }
}