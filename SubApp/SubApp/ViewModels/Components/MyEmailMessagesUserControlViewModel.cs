using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using SubApp.Data;
using SubApp.Models;

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
        try
        {
            await using var db = new AppDbContext();
            
            var data = await db.ParsedEmails
                .OrderByDescending(e => e.ReceivedDate)
                .Take(50)
                .ToListAsync();

            Emails.Clear();
            foreach (var item in data)
            {
                Emails.Add(item);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки подписок: {ex.Message}");
        }
    }
}