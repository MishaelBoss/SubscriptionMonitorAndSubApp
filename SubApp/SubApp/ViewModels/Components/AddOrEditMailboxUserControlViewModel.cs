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

public partial class AddOrEditMailboxUserControlViewModel : ViewModelBase
{
    public List<string> MailProviders { get; } = new()
    { 
        "Gmail", "Yandex", "Mail.ru", "Outlook", "Другой (IMAP)" 
    };
    
    [ObservableProperty] private string _errorEmail = string.Empty;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))] private string _email = string.Empty;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))] private string _password = string.Empty;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))] private string _imapServer = string.Empty;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))] private int _imapPort;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))] private int _frequencyCheck = 60;
    [ObservableProperty] private bool _autoCheck = true;
    [ObservableProperty] private string _selectedProvider = "Gmail";
    
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
    
    [RelayCommand]
    public void Close()
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseAddOrEditEmailMessage());
    }

    [RelayCommand]
    public async Task AddEmailAsync()
    {
        if (!IsActiveConfirmButton) return;
        
        try
        {
            await using var db = new AppDbContext();
            
            var exists = await db.Mailboxes.AnyAsync(m => m.UserId == AuthService.CurrentSession!.Id && m.Email == Email);
            
            if (exists)
            {
                ErrorEmail = $"Почта {Email} уже добавлен для этого пользователя!";
                return;
            }
            
            await using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                var newMailbox = new Mailbox
                {
                    Email = Email,
                    PasswordEncrypted = Password,
                    ImapServer = ImapServer,
                    ImapPort = ImapPort,
                    UserId = AuthService.CurrentSession!.Id, 
                    Provider = SelectedProvider,
                    IsActive = AutoCheck,
                    CheckFrequency = FrequencyCheck,
                    SearchFolder = "INBOX",
                    SearchCriteria = "FROM \"noreply\" OR FROM \"billing\" OR SUBJECT \"subscription\" OR SUBJECT \"payment\"",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                db.Mailboxes.Add(newMailbox);
                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                Console.WriteLine($"Ящик {newMailbox.Email} сохранен, ID: {newMailbox.Id}");

                ClearForm();
                Close();
                WeakReferenceMessenger.Default.Send(new RefreshMailboxMessage());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
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