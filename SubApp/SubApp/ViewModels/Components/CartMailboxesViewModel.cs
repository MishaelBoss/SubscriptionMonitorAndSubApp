using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using SubApp.Data;
using SubApp.Models;
using SubApp.Scripts;
using SubApp.Services;

namespace SubApp.ViewModels.Components;

public partial class CartMailboxesViewModel(Mailbox mail) : ViewModelBase
{
    [ObservableProperty] private string _email = mail.Email;
    [ObservableProperty] private string? _lastCheck = mail.LastChecked?.ToString("g") ?? "Никогда";
    [ObservableProperty] private string _provider = mail.Provider;
    [ObservableProperty] private string _status = mail is { IsActive: true } ? "Активен" : "Отключен";
    [ObservableProperty] private bool _isParsing;
    
    private readonly EmailProcessingService _emailService = new();
    
    [RelayCommand]
    public async Task Run()
    {
        if (IsParsing) return;

        try
        {
            IsParsing = true;
            Status = "Парсинг...";

            WeakReferenceMessenger.Default.Send(new OpenOrCloseProgressModalMessage(mail.Id));
            
            await Task.Run(async () => 
            {
                await _emailService.ProcessMailboxAsync(mail, daysToLookBack: 180);
            });

            Status = "Завершено";
            LastCheck = DateTime.Now.ToString("g");
        }
        catch (MailKit.Security.AuthenticationException)
        {
            Status = "Ошибка: нужен Пароль Приложения";
            WeakReferenceMessenger.Default.Send(new ParsingProgressMessage(
                mail.Id, 0, 0, "Ошибка: Google требует 'Пароль приложения' вместо обычного пароля.", null));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            Status = "Ошибка";
            WeakReferenceMessenger.Default.Send(new ParsingProgressMessage(
                mail.Id, 0, 0, $"Ошибка: {ex.Message}", null));
        }
        finally
        {
            IsParsing = false;
        }
    }
    
    [RelayCommand]
    public async Task FustRun()
    {
        if (IsParsing) return;

        try
        {
            IsParsing = true;
            Status = "Быстрая проверка...";
            
            await _emailService.ProcessMailboxAsync(mail, daysToLookBack: 2);
            
            Status = "Активен";
            LastCheck = DateTime.Now.ToString("g");
            
            WeakReferenceMessenger.Default.Send(new RefreshMailboxMessage());
        }
        catch (Exception ex)
        {
            Status = "Ошибка";
            Console.WriteLine(ex);
        }
        finally
        {
            IsParsing = false;
        }
    }
        
    [RelayCommand]
    public void OpenEditEmail()
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseAddOrEditEmailMessage(mail));
    }
        
    [RelayCommand]
    public void OpenDeleteEmail()
    {
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