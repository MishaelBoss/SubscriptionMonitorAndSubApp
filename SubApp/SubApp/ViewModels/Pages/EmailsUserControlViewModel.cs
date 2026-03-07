using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
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
            try
            {
                CartMailboxesViewModels.Clear();

                await using var db = new AppDbContext();
                
                var mailboxes = await db.Mailboxes
                    .Where(m => m.UserId == AuthService.CurrentSession!.Id)
                    .ToListAsync();
                
                foreach (var mailbox in mailboxes)
                {
                    var viewModel = new CartMailboxesViewModel(mailbox.Id)
                    {
                        Email = mailbox.Email,
                        LastCheck = mailbox.LastChecked?.ToString("g") ?? "Никогда",
                        Provider = mailbox.Provider ?? "Другой",
                        Status = mailbox.IsActive ? "Активен" : "Отключен"
                    };
            
                    CartMailboxesViewModels.Add(viewModel);
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
}
