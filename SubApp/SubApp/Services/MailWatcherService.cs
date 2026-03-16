using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using SubApp.Models;
using SubApp.Scripts;

namespace SubApp.Services;

/*public class MailWatcherService
{
    private readonly PeriodicTimer _timer = new(TimeSpan.FromMinutes(10));
    private readonly CancellationTokenSource _cts = new();

    public void Start()
    {
        _ = WatchLoop();
    }

    private async Task WatchLoop()
    {
        while (await _timer.WaitForNextTickAsync(_cts.Token))
        {
            await using var db = new AppDbContext();
        
            var mailboxes = await db.Mailboxes.Where(m => m.IsActive).ToListAsync();

            foreach (var mail in mailboxes)
            {
                var lastCheck = mail.LastChecked ?? DateTime.MinValue;
                var frequencyInMinutes = mail.CheckFrequency;

                if (DateTime.Now < lastCheck.AddMinutes(frequencyInMinutes)) continue;
                await RunBackgroundParsing(mail);
                
                mail.LastChecked = DateTime.Now;
                await db.SaveChangesAsync();
            }
        }
    }

    private async Task RunBackgroundParsing(Mailbox mail)
    {
        try 
        {
            using var client = new ImapClient();
            await client.ConnectAsync(mail.ImapServer, mail.ImapPort, true);
            await client.AuthenticateAsync(mail.Email, mail.PasswordEncrypted);

            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadOnly);

            var query = SearchQuery.DeliveredAfter(DateTime.Now.AddDays(-1));
            var uids = await inbox.SearchAsync(query);

            foreach (var uid in uids)
            {
                var message = await inbox.GetMessageAsync(uid);
            }

            await using var db = new AppDbContext();
            var dbMail = await db.Mailboxes.FindAsync(mail.Id);
            if (dbMail != null) {
                dbMail.LastChecked = DateTime.Now;
                await db.SaveChangesAsync();
            }
            
            WeakReferenceMessenger.Default.Send(new RefreshSubscriptionMessage());
        }
        catch (Exception ex) {
            Console.WriteLine($"Ошибка авто-проверки {mail.Email}: {ex.Message}");
        }
    }
}*/