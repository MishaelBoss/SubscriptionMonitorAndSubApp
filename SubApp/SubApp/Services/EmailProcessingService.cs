using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using SubApp.Data;
using SubApp.Models;
using SubApp.Scripts;

namespace SubApp.Services;

public class EmailProcessingService
{
    public async Task ProcessMailboxAsync(Mailbox mail, int daysToLookBack = 2)
    {
        try 
        {
            using var client = new ImapClient();
            
            await client.ConnectAsync(mail.ImapServer, mail.ImapPort, true);
            await client.AuthenticateAsync(mail.Email, mail.PasswordEncrypted);

            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadOnly);

            var query = SearchQuery.DeliveredAfter(DateTime.Now.AddDays(-daysToLookBack));
            var uids = await inbox.SearchAsync(query);

            foreach (var uid in uids)
            {
                var message = await inbox.GetMessageAsync(uid);
                var result = ParseMessageContent(message);
                
                var current = uids.IndexOf(uid) + 1;
                WeakReferenceMessenger.Default.Send(new ParsingProgressMessage(
                    mail.Id, current, uids.Count, "Анализ писем...", $"Письмо {current} из {uids.Count}"));
                
                await SaveSubscriptionToDb(result, message, uid.ToString(), mail);
            }

            await client.DisconnectAsync(true);
            
            await UpdateLastCheckedDate(mail.Id);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EmailService] Ошибка в {mail.Email}: {ex.Message}");
        }
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
            decimal.TryParse(match.Groups[1].Value.Replace(",", "."), 
                System.Globalization.NumberStyles.Any, 
                System.Globalization.CultureInfo.InvariantCulture, out amount);
            
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
    
    private async Task SaveSubscriptionToDb((bool IsSubscription, string ServiceName, decimal Amount, Currency Currency) result, MimeMessage message, string messageId, Mailbox mail)
    {
        await using var db = new AppDbContext();

        if (await db.ParsedEmails.AnyAsync(e => e.MessageId == messageId)) return;

        var targetSub = await db.Subscriptions
            .Include(s => s.Service)
            .FirstOrDefaultAsync(s => s.Service.Name == result.ServiceName 
                                   && s.UserId == mail.UserId 
                                   && s.Amount == result.Amount);

        var parsedEmail = new ParsedEmail
        {
            MailboxId = mail.Id,
            MessageId = messageId,
            Subject = message.Subject ?? "Без темы",
            FromEmail = message.From.ToString(),
            ReceivedDate = message.Date.DateTime,
            ServiceName = result.ServiceName,
            Amount = result.Amount,
            ProcessedSubscriptionId = targetSub?.Id,
            RawContent = message.TextBody?[..Math.Min(message.TextBody.Length, 1000)] ?? "",
            IsProcessed = true,
            ErrorMessage = ""
        };
        db.ParsedEmails.Add(parsedEmail);

        if (targetSub != null)
        {
            targetSub.MarkAsPaid(message.Date.DateTime); 
            targetSub.UpdatedAt = DateTime.Now;
            await db.SaveChangesAsync();
            
            WeakReferenceMessenger.Default.Send(new RefreshSubscriptionMessage());
            return;
        }

        if (!result.IsSubscription) 
        {
            await db.SaveChangesAsync();
            return;
        }

        var service = await db.Services.FirstOrDefaultAsync(s => s.Name == result.ServiceName);
        if (service == null)
        {
            service = new Service { 
                Name = result.ServiceName, 
                IsActive = true, 
                Website = "", 
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") 
            };
            db.Services.Add(service);
            await db.SaveChangesAsync();
        }

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
        
        WeakReferenceMessenger.Default.Send(new RefreshSubscriptionMessage());
    }

    private async Task UpdateLastCheckedDate(int mailboxId)
    {
        await using var db = new AppDbContext();
        var mailbox = await db.Mailboxes.FindAsync(mailboxId);
        if (mailbox != null)
        {
            mailbox.LastChecked = DateTime.Now;
            await db.SaveChangesAsync();
        }
    }
}