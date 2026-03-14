using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
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
        
        _ = LoadPaymentHistory(sub);
    }

    private async Task LoadPaymentHistory(Subscription sub)
    {
        await using var db = new AppDbContext();
    
        var history = await db.ParsedEmails
            .Where(e => e.ServiceName == sub.Name && e.Mailbox.UserId == sub.UserId && e.ProcessedSubscriptionId == Sub.Id)
            .OrderByDescending(e => e.ReceivedDate)
            .ToListAsync();

        PaymentHistory.Clear();
        
        foreach (var payment in history)
            PaymentHistory.Add(payment);
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
