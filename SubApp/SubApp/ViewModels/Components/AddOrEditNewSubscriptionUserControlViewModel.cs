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
        SubscriptionStatus.active, 
        SubscriptionStatus.paused, 
        SubscriptionStatus.trial 
    ];

    private Subscription? Sub { get; }

    [ObservableProperty] private string _erroredSubscription = string.Empty;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))] private Service? _selectedService;
    [ObservableProperty] private string _subscriptionName = string.Empty;
    [ObservableProperty] private BillingCycle _selectedPaymentPeriod = BillingCycle.monthly;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))] private decimal? _sum;
    [ObservableProperty] private Currency _selectedCurrency = Currency.RUB;
    [ObservableProperty] private SubscriptionStatus _selectedStatus = SubscriptionStatus.active;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))] private DateTime? _startDate = DateTime.UtcNow;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))] private DateTime? _nextPaymentDate;
    [ObservableProperty] private int? _daysPeriod = 30;
    [ObservableProperty] private bool _automaticRenewal = true;
    [ObservableProperty] private string _notes = string.Empty;
    
    public string ConfirmButtonText
        => Sub == null ? "Добавить" : "Сохранить";

    public bool IsActiveConfirmButton =>
        SelectedService != null &&
        Sum > 0 &&
        StartDate != null &&
        NextPaymentDate != null;

    public AddOrEditNewSubscriptionUserControlViewModel(Subscription? sub = null)
    {
        LoadData();

        if (sub != null)
        {
            Sub = sub;

            SelectedService = Services.FirstOrDefault(s => s.Id == sub.ServiceId);
            SubscriptionName = sub.Name;
            if (Enum.TryParse<BillingCycle>(sub.BillingCycle, out var cycle)) SelectedPaymentPeriod = cycle;
            Sum = sub.Amount;
            if (Enum.TryParse<Currency>(sub.Currency, out var currency)) SelectedCurrency = currency;
            if (Enum.TryParse<SubscriptionStatus>(sub.Status, out var status)) SelectedStatus = status;
            StartDate = sub.StartDate;
            NextPaymentDate = sub.NextPaymentDate;
            DaysPeriod = sub.BillingCycleDays;
            AutomaticRenewal = sub.AutoRenew;
            Notes = sub.Notes ?? string.Empty;
        }
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
            Subscription? subscription;

            if (SelectedService == null || Sum == null) return;

            await using var db = new AppDbContext();

            if (Sub == null)
            {
                subscription = new Subscription
                {
                    Uuid = Guid.NewGuid(),
                    CreatedAt = DateTime.Now,
                    UserId = (int)AuthService.CurrentSession.Id,
                    IsActive = true
                };
                
                db.Subscriptions.Add(subscription);
            }
            else
            {
                subscription = await db.Subscriptions.FindAsync(Sub.Id);
                if (subscription == null) return;
            }
            
            subscription.ServiceId = SelectedService.Id;
            subscription.Name = string.IsNullOrWhiteSpace(SubscriptionName) ? SelectedService.Name : SubscriptionName;
            subscription.Amount = Sum ?? 0;
            subscription.Currency = SelectedCurrency.ToString();
            subscription.BillingCycle = SelectedPaymentPeriod.ToString();
            subscription.Status = SelectedStatus.ToString();
            subscription.StartDate = StartDate ?? DateTime.Now;
            subscription.NextPaymentDate = NextPaymentDate ?? DateTime.Now;
            subscription.AutoRenew = AutomaticRenewal;
            subscription.Notes = Notes;
            subscription.BillingCycleDays = DaysPeriod ?? 30;
            subscription.UpdatedAt = DateTime.Now;
            subscription.LastChecked = DateTime.Now;
            
            await db.SaveChangesAsync();
        
            Close();
            ClearForm();

            WeakReferenceMessenger.Default.Send(new RefreshSubscriptionMessage());
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            ErroredSubscription = "Ошибка при сохранении данных";
        }
    }
    
    [RelayCommand]
    public void Close() 
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseAddOrEditSubscriptionMessage());
    }

    private void ClearForm() 
    {
        ErroredSubscription = string.Empty;
        SelectedService = null;
        SubscriptionName = string.Empty;
        SelectedPaymentPeriod = BillingCycle.monthly;
        Sum = 0;
        SelectedCurrency = Currency.RUB;
        SelectedStatus = SubscriptionStatus.active;
        StartDate = DateTime.UtcNow;
        NextPaymentDate = null;
        DaysPeriod = 30;
        AutomaticRenewal = true;
        Notes = string.Empty;
    }
}