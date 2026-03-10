using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using SubApp.Data;
using SubApp.Models;
using SubApp.Scripts;

namespace SubApp.ViewModels.Components;

public partial class AddOrEditMailboxUserControlViewModel(Mailbox? mail = null) : ViewModelBase
{
    public List<string> MailProviders { get; } = [ "Gmail", "Yandex", "Mail.ru", "Outlook", "Другой (IMAP)" ];

    [ObservableProperty] private string _errorEmail = string.Empty;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))] private string _email = mail != null ? mail.Email : string.Empty;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))] private string _password = mail != null ? mail.PasswordEncrypted : string.Empty;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))] private string _imapServer = mail != null ? mail.ImapServer : string.Empty;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))] private int _imapPort = mail?.ImapPort ?? 0;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))] private int _frequencyCheck = mail?.CheckFrequency ?? 60;
    [ObservableProperty] private bool _autoCheck = mail == null || mail.IsActive;
    [ObservableProperty] private string _selectedProvider = mail != null ? mail.Provider : "Gmail";
    
    partial void OnSelectedProviderChanged(string value)
    {
        switch (value)
        {
            case "Gmail":
                ImapServer = "imap.gmail.com";
                ImapPort = 993;
                break;
            case "Yandex":
                ImapServer = "imap.yandex.ru";
                ImapPort = 993;
                break;
            case "Mail.ru":
                ImapServer = "imap.mail.ru";
                ImapPort = 993;
                break;
            case "Outlook":
                ImapServer = "outlook.office365.com";
                ImapPort = 993;
                break;
            default:
                ImapServer = string.Empty;
                ImapPort = 0;
                break;
        }
    }

    public bool IsActiveConfirmButton
        => !string.IsNullOrEmpty(Email)
           && !string.IsNullOrEmpty(Password)
           && !string.IsNullOrEmpty(ImapServer)
           && ImapPort > 0
           && FrequencyCheck > 0;

    public string ConfirmButtonText
        => mail == null ? "Добавить" : "Сохранить";
    
    [RelayCommand]
    public void Close()
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseAddOrEditEmailMessage());
    }

    [RelayCommand]
    public async Task SaveEmailAsync()
    {
        if (!IsActiveConfirmButton) return;
        
        try
        {
            await using var db = new AppDbContext();
            
            Mailbox? mailbox;

            if (mail == null)
            {
                var exists = await db.Mailboxes.AnyAsync(m => m.UserId == AuthService.CurrentSession!.Id && m.Email == Email);
                if (exists) {
                    ErrorEmail = $"Почта {Email} уже добавлена!";
                    return;
                }
                mailbox = new Mailbox { UserId = AuthService.CurrentSession!.Id, CreatedAt = DateTime.UtcNow };
                db.Mailboxes.Add(mailbox);
                    
                Console.WriteLine($"Ящик {mailbox.Email} сохранен, ID: {mailbox.Id}");
            }
            else
            {
                mailbox = await db.Mailboxes.FindAsync(mail.Id);
                if (mailbox == null) return;
                    
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