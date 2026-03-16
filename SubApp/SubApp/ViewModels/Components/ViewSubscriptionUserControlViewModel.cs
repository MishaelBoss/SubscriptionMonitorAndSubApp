using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SubApp.Data;
using SubApp.Models;
using SubApp.Scripts;

namespace SubApp.ViewModels.Components;

public partial class ViewSubscriptionUserControlViewModel : ViewModelBase
{ 
    public Subscription Sub { get; }
    [ObservableProperty] private ObservableCollection<ParsedEmail> _paymentHistory = [];
    
    public ViewSubscriptionUserControlViewModel(Subscription sub)
    {
        Sub = sub;
        
        _ = LoadPaymentHistory();
    }

    private async Task LoadPaymentHistory()
    {
        var session = AuthService.CurrentSession;
        if (session == null) return;

        try
        {
            var api = new ApiService(session.Token);
        
            var history = await api.GetEmailsForSubscriptionAsync(Sub.Id);

            Dispatcher.UIThread.Post(() => 
            {
                PaymentHistory.Clear();
                foreach (var payment in history.GroupBy(x => x.MessageId).Select(g => g.First()))
                {
                    PaymentHistory.Add(payment);
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки точечной истории: {ex.Message}");
        }
    }

    [RelayCommand]
    public void Close()
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseSubscriptionDetailsMessage());
    }

    [RelayCommand]
    public void Edit()
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseAddOrEditSubscriptionMessage(Sub));
    }

    [RelayCommand]
    public void ChangeStatus()
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseConfirmationSubscriptionCancellationMessage(Sub));
    }
}
