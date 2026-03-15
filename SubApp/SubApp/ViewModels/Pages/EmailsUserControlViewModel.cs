using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using SubApp.Data;
using SubApp.Models;
using SubApp.Scripts;
using SubApp.ViewModels.Components;

namespace SubApp.ViewModels.Pages
{
    public partial class EmailsUserControlViewModel : ViewModelBase, 
        IRecipient<RefreshMailboxMessage>
    {
        [ObservableProperty] private ObservableCollection<CartMailboxesViewModel> _cartMailboxesViewModels = [];

        public EmailsUserControlViewModel()
        {
            WeakReferenceMessenger.Default.Register(this);

            _ = LoadEmailsAsync();
        }

        public async void Receive(RefreshMailboxMessage message)
        {
            try
            {
                await LoadEmailsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Не удалось обновить список почт: {ex}");
            }
        }

        [RelayCommand]
        public void OpenAddEmail()
        {
            WeakReferenceMessenger.Default.Send(new OpenOrCloseAddOrEditEmailMessage());
        }

        private async Task LoadEmailsAsync()
        {
            var session = AuthService.CurrentSession;
            if(session == null) return;
            
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Token", session.Token);
        
                var url = "http://10.0.2.2:8000/mail/api/mailboxes/";
                var content = await client.GetStringAsync(url);
        
                var cleanJson = content.Trim().Trim('\uFEFF');
                
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var mailboxes = JsonSerializer.Deserialize<List<Mailbox>>(cleanJson, options);
                if (mailboxes != null)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        CartMailboxesViewModels.Clear();
                        foreach (var mailbox in mailboxes)
                        {
                            CartMailboxesViewModels.Add(new CartMailboxesViewModel(mailbox));
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        
        ~EmailsUserControlViewModel()
        {
            WeakReferenceMessenger.Default.Unregister<RefreshMailboxMessage>(this);
        }
    }
    
    public class PaginatedResponse<T>
    {
        public int Count { get; set; }
        public string? Next { get; set; }
        public string? Previous { get; set; }
        public List<T> Results { get; set; } = new();
    }
}
