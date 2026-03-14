using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SubApp.Data;

namespace SubApp.Services;

public class MailBackgroundWorker
{
    private readonly EmailProcessingService _processor = new();
    private bool _isRunning;

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;
        
        Task.Run(async () => 
        {
            while (_isRunning)
            {
                await CheckAllMailboxes();
                await Task.Delay(TimeSpan.FromMinutes(5)); 
            }
        });
    }
    
    private async Task CheckAllMailboxes()
    {
        await using var db = new AppDbContext();
        var activeMails = await db.Mailboxes.Where(m => m.IsActive).ToListAsync();

        foreach (var mail in activeMails)
        {
            var nextCheckTime = (mail.LastChecked ?? DateTime.MinValue).AddMinutes(mail.CheckFrequency);

            if (DateTime.Now >= nextCheckTime)
            {
                await _processor.ProcessMailboxAsync(mail);
                
                var dbMail = await db.Mailboxes.FindAsync(mail.Id);
                if (dbMail != null) {
                    dbMail.LastChecked = DateTime.Now;
                    await db.SaveChangesAsync();
                }
            }
        }
    }
    
    public void Stop() => _isRunning = false;
}