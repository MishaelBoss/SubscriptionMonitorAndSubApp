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

public partial class CartMailboxesViewModel(long id, Mailbox mail) : ViewModelBase
{
    private long Id { get; } = id;
    private Mailbox Mail { get; } = mail;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _lastCheck = string.Empty;
    [ObservableProperty] private string _provider = string.Empty;
    [ObservableProperty] private string _status = string.Empty;
    
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
        WeakReferenceMessenger.Default.Send(new OpenOrCloseAddOrEditEmailMessage(Mail));
    }
        
    [RelayCommand]
    public async Task OpenDeleteEmail()
    {
        try
        {
            await using var db = new AppDbContext();
            
            var mailbox = await db.Mailboxes.FirstOrDefaultAsync(m => m.Id == Id);
            if (mailbox == null) return;
            
            db.Mailboxes.Remove(mailbox);
            await db.SaveChangesAsync();

            WeakReferenceMessenger.Default.Send(new RefreshMailboxMessage());

            Console.WriteLine($"Ящик с ID {Id} удален.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при удалении: {ex.Message}");
        }
    }
}