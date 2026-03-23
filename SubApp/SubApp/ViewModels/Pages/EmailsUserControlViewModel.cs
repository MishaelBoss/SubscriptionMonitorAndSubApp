using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SubApp.Data;
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
            if(session == null) {
                Console.WriteLine("DEBUG: Сессия пустая, загрузка отменена.");
                return;
            }
            
            try
            {
                var api = new ApiService(session.Token);
                
                var mailboxes = await api.GetMailBoxAsync();

                Dispatcher.UIThread.Post(() =>
                {
                    CartMailboxesViewModels.Clear();
                    foreach (var mailbox in mailboxes)
                    {
                        CartMailboxesViewModels.Add(new CartMailboxesViewModel(mailbox));
                    }
                });
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
