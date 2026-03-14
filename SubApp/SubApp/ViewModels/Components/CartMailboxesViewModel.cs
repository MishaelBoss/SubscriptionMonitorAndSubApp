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

            await _emailService.ProcessMailboxAsync(mail, daysToLookBack: 180);

            // await Task.Run(async () =>
            // {
            //     using var client = new ImapClient();
            //     await client.ConnectAsync(mail.ImapServer, mail.ImapPort, true);
            //     await client.AuthenticateAsync(mail.Email, mail.PasswordEncrypted);
            //
            //     var inbox = client.Inbox;
            //     await inbox.OpenAsync(FolderAccess.ReadOnly);
            //
            //     var query = SearchQuery.DeliveredAfter(DateTime.Now.AddMonths(-6));
            //     var uids = await inbox.SearchAsync(query);
            //     var total = uids.Count;
            //
            //     for (int i = 0; i < total; i++)
            //     {
            //         var message = await inbox.GetMessageAsync(uids[i]);
            //         var result = ParseMessageContent(message);
            //
            //         if (result.IsSubscription)
            //         {
            //             await SaveSubscriptionToDb(result, message, uids[i].ToString());
            //         }
            //
            //         WeakReferenceMessenger.Default.Send(new ParsingProgressMessage(
            //             mail.Id, i + 1, total, "Анализ писем...", $"Обработано: {message.Subject}"));
            //     }
            //
            //     await client.DisconnectAsync(true);
            // });

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

            // await Task.Run(async () =>
            // {
            //     using var client = new ImapClient();
            //     await client.ConnectAsync(mail.ImapServer, mail.ImapPort, true);
            //     await client.AuthenticateAsync(mail.Email, mail.PasswordEncrypted);
            //
            //     var inbox = client.Inbox;
            //     await inbox.OpenAsync(FolderAccess.ReadOnly);
            //
            //     var count = inbox.Count;
            //     var startIndex = Math.Max(0, count - 5);
            //
            //     for (int i = count - 1; i >= startIndex; i--)
            //     {
            //         var message = await inbox.GetMessageAsync(i);
            //         var messageId = message.MessageId ?? $"fast_{i}_{DateTime.Now.Ticks}";
            //         var result = ParseMessageContent(message);
            //         await SaveSubscriptionToDb(result, message, messageId);
            //     }
            //
            //     await client.DisconnectAsync(true);
            // });

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