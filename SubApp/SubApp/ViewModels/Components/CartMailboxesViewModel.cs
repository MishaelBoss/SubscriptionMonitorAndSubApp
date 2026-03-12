using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using SubApp.Data;
using SubApp.Models;
using SubApp.Scripts;

namespace SubApp.ViewModels.Components;

public partial class CartMailboxesViewModel(Mailbox mail) : ViewModelBase
{
    [ObservableProperty] private string _email = mail.Email;
    [ObservableProperty] private string? _lastCheck = mail.LastChecked?.ToString("g") ?? "Никогда";
    [ObservableProperty] private string _provider = mail.Provider.ToString() ?? "Другой";
    [ObservableProperty] private string _status = mail is { IsActive: true } ? "Активен" : "Отключен";
    
    [RelayCommand]
    public void Run()
    {
    }
        
    [RelayCommand]
    public void FustRun()
    {
    }
        
    [RelayCommand]
    public void OpenEditEmail()
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseAddOrEditEmailMessage(mail));
    }
        
    [RelayCommand]
    public void OpenDeleteEmail()
    {
        // try
        // {
        //     await using var db = new AppDbContext();
        //     
        //     var mailbox = await db.Mailboxes.FirstOrDefaultAsync(m => m.Id == mail.Id);
        //     if (mailbox == null) return;
        //     
        //     db.Mailboxes.Remove(mailbox);
        //     await db.SaveChangesAsync();
        //
        //     WeakReferenceMessenger.Default.Send(new RefreshMailboxMessage());
        //
        //     Console.WriteLine($"Ящик с ID {mail.Id} удален.");
        // }
        // catch (Exception ex)
        // {
        //     Console.WriteLine($"Ошибка при удалении: {ex.

        WeakReferenceMessenger.Default.Send(new OpenOrCloseConfirmDelete(async () => {
            await using var db = new AppDbContext();
            var mailbox = await db.Mailboxes.FirstOrDefaultAsync(m => m.Id == mail.Id);
            if (mailbox != null)
            {
                db.Mailboxes.Remove(mailbox);
                await db.SaveChangesAsync();
            }

            WeakReferenceMessenger.Default.Send(new RefreshMailboxMessage());
        }));
    }
}