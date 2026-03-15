using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SubApp.Data;
using SubApp.Models;
using SubApp.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Avalonia.Threading;

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
    
    private readonly string _baseUrl = "http://10.0.2.2:8000/subscriptions/api/subscriptions/";
    
    public string ConfirmButtonText
        => Sub == null ? "Добавить" : "Сохранить";

    public bool IsActiveConfirmButton =>
        SelectedService != null &&
        Sum > 0 &&
        StartDate != null &&
        NextPaymentDate != null;

    public AddOrEditNewSubscriptionUserControlViewModel(Subscription? sub = null)
    {
        Task.Run(LoadDataAsync);

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
    
    private async Task LoadDataAsync()
    {
        try
        {
            using var client = new HttpClient(); 
            const string url = "http://10.0.2.2:8000/subscriptions/api/";
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", AuthService.CurrentSession?.Token);
            
            var response = await client.GetFromJsonAsync<List<Service>>(url + "services/");
            if (response != null)
            {
                Dispatcher.UIThread.Post(() => {
                    Services = response;
                    if (Sub != null) SelectedService = Services.FirstOrDefault(s => s.Id == Sub.ServiceId);
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
    
    [RelayCommand]
    public async Task Save()
    {
        if (!IsActiveConfirmButton || AuthService.CurrentSession == null) return;
        
        Console.WriteLine($"Начало записи {SubscriptionName}");
        
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", AuthService.CurrentSession.Token);
            
            var subData = new {
                service = SelectedService!.Id,
                name = string.IsNullOrWhiteSpace(SubscriptionName) ? SelectedService.Name : SubscriptionName,
                amount = Sum ?? 0,
                currency = SelectedCurrency.ToString(),
                billing_cycle = SelectedPaymentPeriod.ToString(),
                status = SelectedStatus.ToString(),
                start_date = StartDate?.ToString("yyyy-MM-dd"),
                next_payment_date = NextPaymentDate?.ToString("yyyy-MM-dd"),
                auto_renew = AutomaticRenewal,
                notes = Notes,
                billing_cycle_days = DaysPeriod ?? 30
            };
            
            var json = System.Text.Json.JsonSerializer.Serialize(subData);
            var buffer = System.Text.Encoding.UTF8.GetBytes(json);
            var content = new ByteArrayContent(buffer);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var method = Sub == null ? HttpMethod.Post : HttpMethod.Put;
            var requestUrl = Sub == null ? _baseUrl : $"{_baseUrl}{Sub.Id}/";
        
            var request = new HttpRequestMessage(method, requestUrl);
            request.Version = new Version(1, 1);
            request.Content = content;

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                Close();
                WeakReferenceMessenger.Default.Send(new RefreshSubscriptionMessage());
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[DJANGO ERROR]: {error}");
                ErroredSubscription = "Ошибка: " + error;
            }
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