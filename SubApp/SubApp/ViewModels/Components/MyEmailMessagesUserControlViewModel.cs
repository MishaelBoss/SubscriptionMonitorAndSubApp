using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using SubApp.Data;
using SubApp.Models;
using SubApp.Scripts;

namespace SubApp.ViewModels.Components;

public partial class MyEmailMessagesUserControlViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<ParsedEmail> _emails = [];
    
    public MyEmailMessagesUserControlViewModel()
    {
        _ = LoadData();
    }
    
    private async Task LoadData()
    {
        var session = AuthService.CurrentSession;
        if (session == null) return;
        
        try
        {
            var api = new ApiService(session.Token);
            var allEmails = await api.GetParsedEmailsAsync();
            
            var data = allEmails
                .OrderByDescending(e => e.ReceivedDate)
                .Take(50)
                .ToList();

            Dispatcher.UIThread.Post(() =>
            {
                Emails.Clear();
                
                foreach (var item in data)
                {
                    Emails.Add(item);
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки подписок: {ex.Message}");
        }
    }
}