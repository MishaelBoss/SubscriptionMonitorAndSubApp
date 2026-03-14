using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using SubApp.Data;
using SubApp.Models;
using SubApp.Scripts;

namespace SubApp.ViewModels.Components;

public partial class AddOrEditMailboxUserControlViewModel : ViewModelBase
{
    public List<string> MailProviders { get; } = [ "Gmail", "Yandex", "Mail.ru", "Outlook", "Другой (IMAP)" ];
    
    private static readonly Dictionary<string, (string Server, int Port)> Providers = new()
    {
        { "Gmail", ("imap.gmail.com", 993) },
        { "Yandex", ("imap.yandex.ru", 993) },
        { "Mail.ru", ("imap.mail.ru", 993) },
        { "Outlook", ("outlook.office365.com", 993) }
    };
    
    public AddOrEditMailboxUserControlViewModel(Mailbox? mail = null)
    {
        Mailbox = mail;
        
        _selectedProvider = mail?.Provider ?? "Gmail";

        Email = mail != null ? mail.Email : string.Empty;
        Password = mail != null ? mail.PasswordEncrypted : string.Empty;
        ImapServer = mail != null ? mail.ImapServer : string.Empty;
        ImapPort = mail?.ImapPort ?? 0;
        FrequencyCheck = mail?.CheckFrequency ?? 60;
        AutoCheck = mail == null || mail.IsActive;
        
        if (mail != null)
        {
            _imapServer = mail.ImapServer;
            _imapPort = mail.ImapPort;
        }
        else if (Providers.TryGetValue(_selectedProvider, out var config))
        {
            _imapServer = config.Server;
            _imapPort = config.Port;
        }
    }

    private Mailbox? Mailbox { get; }

    [ObservableProperty] private string _errorEmail = string.Empty;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))] private string _email;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))] private string _password;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))] private string _imapServer;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))] private int _imapPort ;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))] private int _frequencyCheck;
    [ObservableProperty] private bool _autoCheck;
    [ObservableProperty] private string _selectedProvider;

    partial void OnSelectedProviderChanged(string value)
    {
        if (Providers.TryGetValue(value, out var config))
        {
            ImapServer = config.Server;
            ImapPort = config.Port;
        }
        else
        {
            ImapServer = string.Empty;
            ImapPort = 0;
        }
    }

    public bool IsActiveConfirmButton
        => !string.IsNullOrEmpty(Email)
           && !string.IsNullOrEmpty(Password)
           && !string.IsNullOrEmpty(ImapServer)
           && ImapPort > 0
           && FrequencyCheck > 0;

    public string ConfirmButtonText
        => Mailbox == null ? "Добавить" : "Сохранить";
    
    [RelayCommand]
    public void Close()
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseAddOrEditEmailMessage());
    }

    [RelayCommand]
    public async Task SaveEmailAsync()
    {
        if (!IsActiveConfirmButton) return;
        
        await AuthService.TryAutoLoginAsync();
        
        if(AuthService.CurrentSession?.Id == 0 || AuthService.CurrentSession == null) return;
        
        try
        {
            await using var db = new AppDbContext();
            
            Mailbox? mailbox;

            if (Mailbox == null)
            {
                var exists = await db.Mailboxes.AnyAsync(m => m.UserId == AuthService.CurrentSession.Id && m.Email == Email);
                if (exists) {
                    ErrorEmail = $"Почта {Email} уже добавлена!";
                    return;
                }
                
                mailbox = new Mailbox
                {
                    UserId = AuthService.CurrentSession.Id, CreatedAt = DateTime.UtcNow,
                    SearchFolder = "INBOX",
                    SearchCriteria = "FROM \"noreply\" OR FROM \"billing\" OR SUBJECT \"subscription\" OR SUBJECT \"payment\""
                };
                db.Mailboxes.Add(mailbox);
                    
                Console.WriteLine($"Ящик {mailbox.Email} сохранен, ID: {mailbox.Id}");
            }
            else
            {
                mailbox = await db.Mailboxes.FindAsync(Mailbox.Id);
                if (mailbox == null) return;
                mailbox.SearchFolder = Mailbox.SearchFolder;
                mailbox.SearchCriteria = Mailbox.SearchCriteria;
                    
                Console.WriteLine($"Ящик {mailbox.Email} изменен, ID: {mailbox.Id}");
            }
                
            mailbox.Email = Email;
            mailbox.PasswordEncrypted = Password;
            mailbox.ImapServer = ImapServer;
            mailbox.ImapPort = ImapPort;
            mailbox.Provider = SelectedProvider;
            mailbox.IsActive = AutoCheck;
            mailbox.CheckFrequency = FrequencyCheck;
            mailbox.UpdatedAt = DateTime.UtcNow;
                
            await db.SaveChangesAsync();
                
            ClearForm();
            Close();
            WeakReferenceMessenger.Default.Send(new RefreshMailboxMessage());
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private void ClearForm()
    {
        Email = string.Empty;
        Password = string.Empty;
        ImapServer = string.Empty;
        ImapPort = 0;
        FrequencyCheck = 60;
        AutoCheck = true;
    }
}