using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using SubApp.Data;
using SubApp.Models;
using SubApp.Scripts;

namespace SubApp.Services;

public class EmailProcessingService
{
    private const string BaseApiUrl = "http://10.0.2.2:8000";

    public async Task ProcessMailboxAsync(Mailbox mail, int daysToLookBack = 2)
    {
        try 
        {
            if (string.IsNullOrWhiteSpace(mail.ImapServer))
            {
                Console.WriteLine($"[EmailService] ОШИБКА: Сервер IMAP пуст для {mail.Email}. Проверь JSON-аттрибуты в модели!");
                return;
            }
            
            using var client = new ImapClient();
            Console.WriteLine($"[DEBUG] Пытаюсь подключиться к серверу: '{mail.ImapServer}' на порт {mail.ImapPort}");
            
            await client.ConnectAsync(mail.ImapServer, mail.ImapPort).ConfigureAwait(false);
            await client.AuthenticateAsync(mail.Email, mail.PasswordEncrypted).ConfigureAwait(false);

            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadOnly);

            var query = SearchQuery.DeliveredAfter(DateTime.Now.AddDays(-daysToLookBack));
            var uids = await inbox.SearchAsync(query);

            foreach (var uid in uids)
            {
                var message = await inbox.GetMessageAsync(uid);
                var result = ParseMessageContent(message);
                
                int current = uids.IndexOf(uid) + 1;
                WeakReferenceMessenger.Default.Send(new ParsingProgressMessage(
                    mail.Id, current, uids.Count, "Анализ писем...", $"Письмо {current} из {uids.Count}"));
                
                if (result.IsSubscription)
                {
                    await SaveSubscriptionToDb(result, message, mail);
                }
            }

            await client.DisconnectAsync(true);
            
            await UpdateLastCheckedDateApi(mail);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EmailService] Ошибка в {mail.Email}: {ex.Message}");
        }
    }
    
    private (bool IsSubscription, string ServiceName, decimal Amount, Currency Currency) ParseMessageContent(MimeMessage message)
    {
        var knownServices = new[] { "Netflix", "Spotify", "Yandex", "YouTube", "Adobe", "Apple" };
        var subject = message.Subject?.ToLower() ?? string.Empty;
        var body = message.TextBody?.ToLower() ?? string.Empty;
        var from = message.From.ToString().ToLower();

        foreach (var service in knownServices)
        {
            if (from.Contains(service.ToLower()) || subject.Contains(service.ToLower()))
            {
                var match = Regex.Match(body, @"(\d+[\.,]?\d{0,2})\s?(руб|₽|\$|eur)");
                Currency foundCurrency = Currency.RUB;
                decimal amount = 0;

                if (match.Success)
                {
                    decimal.TryParse(match.Groups[1].Value.Replace(",", "."), 
                        System.Globalization.NumberStyles.Any, 
                        System.Globalization.CultureInfo.InvariantCulture, out amount);
                    
                    var symbol = match.Groups[2].Value.ToLower();
                    foundCurrency = symbol switch {
                        "$" => Currency.USD,
                        "eur" => Currency.EUR,
                        _ => Currency.RUB
                    };
                }
                return (true, service, amount, foundCurrency);
            }
        }
        return (false, "", 0, Currency.RUB);
    }
    
    private async Task SaveSubscriptionToDb((bool IsSubscription, string ServiceName, decimal Amount, Currency Currency) result, MimeMessage message, Mailbox mail)
    {
        var session = AuthService.CurrentSession;
        if (session == null) return;

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", session.Token);
        
        var emailData = new {
            mailbox = mail.Id,
            message_id = message.MessageId,
            subject = message.Subject ?? "Без темы",
            from_email = message.From.Mailboxes.FirstOrDefault()?.Address ?? "unknown@email.com",
            received_date = message.Date.DateTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            service_name = result.ServiceName,
            amount = result.Amount,
            is_subscription = true,
            raw_content = message.TextBody != null ? message.TextBody[..Math.Min(message.TextBody.Length, 1000)] : ""
        };

        var emailJson = JsonSerializer.Serialize(emailData);
        var content = new StringContent(emailJson, Encoding.UTF8, "application/json");
        
        var emailRequest = new HttpRequestMessage(HttpMethod.Post, $"{BaseApiUrl}/mail/api/emails/");
        emailRequest.Version = new Version(1, 1);
        emailRequest.Content = content;
        
        var emailResp = await client.SendAsync(emailRequest);
        if (!emailResp.IsSuccessStatusCode)
        {
            var err = await emailResp.Content.ReadAsStringAsync();
            Console.WriteLine($"[API ERROR] Письмо не сохранено: {err}");
        }

        var api = new ApiService(session.Token);
        var allSubs = await api.GetSubscriptionsAsync();
        
        var targetSub = allSubs.FirstOrDefault(s => 
            s.Name.ToLower().Contains(result.ServiceName.ToLower()) && 
            s.Amount == result.Amount);

        if (targetSub != null)
        {
            targetSub.MarkAsPaid(message.Date.DateTime);
            
            var subUpdateData = new {
                service = targetSub.ServiceId,
                name = targetSub.Name,
                amount = targetSub.Amount,
                currency = targetSub.Currency,
                billing_cycle = targetSub.BillingCycle,
                status = targetSub.Status,
                start_date = targetSub.StartDate.ToString("yyyy-MM-dd"),
                next_payment_date = targetSub.NextPaymentDate.ToString("yyyy-MM-dd"),
                is_active = targetSub.IsActive
            };

            var subJson = JsonSerializer.Serialize(subUpdateData);
            var subBuffer = Encoding.UTF8.GetBytes(subJson);
            var subContent = new ByteArrayContent(subBuffer);
            subContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var subRequest = new HttpRequestMessage(HttpMethod.Put, $"http://10.0.2.2:8000/subscriptions/api/subscriptions/{targetSub.Id}/");
            subRequest.Version = new Version(1, 1);
            subRequest.Content = subContent;
            
            await client.SendAsync(subRequest);
        }
        
        WeakReferenceMessenger.Default.Send(new RefreshSubscriptionMessage());
        WeakReferenceMessenger.Default.Send(new RefreshMailboxMessage());
    }

    private async Task UpdateLastCheckedDateApi(Mailbox mail)
    {
        var session = AuthService.CurrentSession;
        if (session == null) return;

        try {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", session.Token);
            
            var url = $"{BaseApiUrl}/mail/api/mailboxes/{mail.Id}/";
            
            var updateData = new
            {
                last_checked = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")
            };
            
            var json = JsonSerializer.Serialize(updateData);
            var buffer = Encoding.UTF8.GetBytes(json);

            var content = new ByteArrayContent(buffer);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            
            var request = new HttpRequestMessage(HttpMethod.Patch, url);
            request.Version = new Version(1, 0);
            request.Content = content;
            
            await client.SendAsync(request);
        } catch (Exception ex) {
            Console.WriteLine($"[EmailService] Не удалось обновить дату проверки: {ex.Message}");
        }
    }
}