using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
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
        
        Email = mail?.Email ?? string.Empty;
        Password = mail?.PasswordEncrypted ?? string.Empty;
        FrequencyCheck = mail?.CheckFrequency ?? 60;
        AutoCheck = mail?.IsActive ?? true;
        ImapServer = mail?.ImapServer ?? string.Empty;
        ImapPort = mail?.ImapPort ?? 0;

        if (mail != null)
        {
            string providerFromDb = mail.Provider switch
            {
                "gmail" => "Gmail",
                "yandex" => "Yandex",
                "mailru" => "Mail.ru",
                "outlook" => "Outlook",
                "other" => "Другой (IMAP)",
                _ => "Gmail"
            };
        
            SelectedProvider = providerFromDb;
        }
        else
        {
            SelectedProvider = "Gmail";
            if (Providers.TryGetValue(SelectedProvider, out var config))
            {
                ImapServer = config.Server;
                ImapPort = config.Port;
            }
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
        
        var session = AuthService.CurrentSession;
        if(session == null) return;
        
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Token", session.Token);
            
            var baseUrl = "http://10.0.2.2:8000/mail/api/mailboxes/";
            var requestUrl = Mailbox == null ? baseUrl : $"{baseUrl}{Mailbox.Id}/";
            var method = Mailbox == null ? HttpMethod.Post : HttpMethod.Put;
            
            var mailboxData = new {
                email = Email,
                provider = SelectedProvider.ToLower().Replace(" (imap)", ""),
                password_encrypted = Password,
                imap_server = ImapServer,
                imap_port = ImapPort,
                is_active = AutoCheck,
                check_frequency = FrequencyCheck,
                search_folder = "INBOX",
                search_criteria = "FROM \"noreply\" OR FROM \"billing\" OR SUBJECT \"subscription\" OR SUBJECT \"payment\""
            };
            
            var json = System.Text.Json.JsonSerializer.Serialize(mailboxData);
            var buffer = System.Text.Encoding.UTF8.GetBytes(json);

            var content = new ByteArrayContent(buffer);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            var request = new HttpRequestMessage(method, requestUrl);
            request.Version = new Version(1, 1);
            request.Content = content;

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                ClearForm();
                Close();
                WeakReferenceMessenger.Default.Send(new RefreshMailboxMessage());
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Ошибка Django: {error}");
                ErrorEmail = "Ошибка: " + error;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            ErrorEmail = "Ошибка сети";
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