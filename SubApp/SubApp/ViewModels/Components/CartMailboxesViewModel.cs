using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using SubApp.Data;
using SubApp.Scripts;

namespace SubApp.ViewModels.Components;

public partial class CartMailboxesViewModel(long id) : ViewModelBase
{
    private long Id { get; } = id;
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
        WeakReferenceMessenger.Default.Send(new OpenOrCloseAddOrEditEmailMessage(Id, Email, null, null, null, null));
    }
        
    [RelayCommand]
    public async void OpenDeleteEmail()
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