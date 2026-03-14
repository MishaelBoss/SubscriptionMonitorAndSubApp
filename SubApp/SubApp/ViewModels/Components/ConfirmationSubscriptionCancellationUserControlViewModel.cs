using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using SubApp.Data;
using SubApp.Models;
using SubApp.Scripts;

namespace SubApp.ViewModels.Components;

public partial class ConfirmationSubscriptionCancellationUserControlViewModel(Subscription? sub) : ViewModelBase
{
    [RelayCommand]
    public async Task CancelSubscription()
    {
        if (sub == null) return;

        try 
        {
            await using var db = new AppDbContext();
        
            var dbSub = await db.Subscriptions.FirstOrDefaultAsync(s => s.Id == sub.Id);
        
            if (dbSub != null)
            {
                dbSub.Status = "Cancelled"; 
                dbSub.UpdatedAt = DateTime.Now;
            
                await db.SaveChangesAsync();

                sub.Status = "cancelled";
            
                WeakReferenceMessenger.Default.Send(new RefreshSubscriptionMessage());
                WeakReferenceMessenger.Default.Send(new OpenOrCloseConfirmationSubscriptionCancellationMessage());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при отмене подписки: {ex.Message}");
        }
    }
    
    [RelayCommand]
    public void Close()
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseConfirmationSubscriptionCancellationMessage());
    }
}