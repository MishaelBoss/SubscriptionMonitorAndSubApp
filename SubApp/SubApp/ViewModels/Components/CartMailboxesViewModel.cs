using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using SubApp.Data;
using SubApp.Models;
using SubApp.Scripts;
using static System.Decimal;

namespace SubApp.ViewModels.Components;

public partial class CartMailboxesViewModel(Mailbox mail) : ViewModelBase
{
    [ObservableProperty] private string _email = mail.Email;
    [ObservableProperty] private string? _lastCheck = mail.LastChecked?.ToString("g") ?? "Никогда";
    [ObservableProperty] private string _provider = mail.Provider;
    [ObservableProperty] private string _status = mail is { IsActive: true } ? "Активен" : "Отключен";
    [ObservableProperty] private bool _isParsing;
    
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
                using var client = new ImapClient();
                await client.ConnectAsync(mail.ImapServer, mail.ImapPort, true);
                await client.AuthenticateAsync(mail.Email, mail.PasswordEncrypted);

                var inbox = client.Inbox;
                await inbox.OpenAsync(FolderAccess.ReadOnly);

                var query = SearchQuery.DeliveredAfter(DateTime.Now.AddMonths(-6));
                var uids = await inbox.SearchAsync(query);
                var total = uids.Count;

                for (int i = 0; i < total; i++)
                {
                    var message = await inbox.GetMessageAsync(uids[i]);
                    var result = ParseMessageContent(message);

                    if (result.IsSubscription)
                    {
                        await SaveSubscriptionToDb(result, message, uids[i].ToString());
                    }

                    WeakReferenceMessenger.Default.Send(new ParsingProgressMessage(
                        mail.Id, i + 1, total, "Анализ писем...", $"Обработано: {message.Subject}"));
                }

                await client.DisconnectAsync(true);
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

            await Task.Run(async () =>
            {
                using var client = new ImapClient();
                await client.ConnectAsync(mail.ImapServer, mail.ImapPort, true);
                await client.AuthenticateAsync(mail.Email, mail.PasswordEncrypted);

                var inbox = client.Inbox;
                await inbox.OpenAsync(FolderAccess.ReadOnly);

                var count = inbox.Count;
                var startIndex = Math.Max(0, count - 5);

                for (int i = count - 1; i >= startIndex; i--)
                {
                    var message = await inbox.GetMessageAsync(i);
                    var messageId = message.MessageId ?? $"fast_{i}_{DateTime.Now.Ticks}";
                    var result = ParseMessageContent(message);
                    await SaveSubscriptionToDb(result, message, messageId);
                }

                await client.DisconnectAsync(true);
            });

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
    
    private (bool IsSubscription, string Service, decimal Amount, Currency Currency) ParseMessageContent(MimeMessage message)
    {
        var knownServices = new[] { "Netflix", "Spotify", "Yandex", "YouTube", "Adobe", "Apple" };
    
        var subject = message.Subject?.ToLower()?? string.Empty;
        var body = message.TextBody?.ToLower() ?? string.Empty;
        var from = message.From.ToString().ToLower();

        foreach (var service in knownServices)
        {
            if (subject == null || (!from.Contains(service.ToLower()) && !subject.Contains(service.ToLower()))) continue;
            
            var match = Regex.Match(body, @"(\d+[\.,]?\d{0,2})\s?(руб|₽|\$|eur)");
            
            Currency foundCurrency = Currency.RUB;
            decimal amount = 0;

            if (!match.Success) return (true, service, amount, foundCurrency);
            TryParse(match.Groups[1].Value.Replace(",", "."), out amount);

            var symbol = match.Groups[2].Value.ToLower();
            foundCurrency = symbol switch
            {
                "$" => Currency.USD,
                "eur" => Currency.EUR,
                _ => Currency.RUB
            };

            return (true, service, amount, foundCurrency);
        }

        return (false, "", 0, Currency.RUB);
    }

    private async Task SaveSubscriptionToDb((bool IsSubscription, string ServiceName, decimal Amount, Currency Currency) result, MimeMessage message, string messageId)
    {
        await using var db = new AppDbContext();

        var emailExists = await db.ParsedEmails.AnyAsync(e => e.MessageId == messageId);
        
        if (!emailExists)
        {
            var parsedEmail = new ParsedEmail
            {
                MailboxId = mail.Id,
                MessageId = messageId,
                Subject = message.Subject ?? "Без темы",
                FromEmail = message.From.ToString(),
                ReceivedDate = message.Date.DateTime,
                ServiceName = result.ServiceName,
                Amount = result.Amount,
                RawContent = message.TextBody?[..Math.Min(message.TextBody.Length, 1000)] ?? "",
                IsProcessed = true,
                ErrorMessage = ""
            };
            db.ParsedEmails.Add(parsedEmail);
            await db.SaveChangesAsync();
        }

        if (!result.IsSubscription) return;

        var service = await db.Services.FirstOrDefaultAsync(s => s.Name == result.ServiceName);
        if (service == null)
        {
            service = new Service
            {
                Name = result.ServiceName, 
                IsActive = true,
                Website = "",
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            };
            db.Services.Add(service);
            await db.SaveChangesAsync();
        }

        var subExists = await db.Subscriptions.AnyAsync(s => s.Name == result.ServiceName && s.UserId == mail.UserId);
        if (!subExists)
        {
            var newSub = new Subscription
            {
                Uuid = Guid.NewGuid(),
                Name = result.ServiceName,
                Amount = result.Amount,
                UserId = (int)mail.UserId,
                ServiceId = service.Id,
                Currency = result.Currency.ToString(),
                BillingCycle = "monthly",
                Status = "active",
                StartDate = DateTime.Now,
                NextPaymentDate = DateTime.Now.AddMonths(1),
                Notes = "",
                AutoRenew = true,
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                LastChecked = DateTime.Now
            };
            db.Subscriptions.Add(newSub);
            await db.SaveChangesAsync();
        }
    }
}