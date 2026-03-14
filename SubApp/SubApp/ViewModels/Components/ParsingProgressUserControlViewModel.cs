using System;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SubApp.Scripts;

namespace SubApp.ViewModels.Components;

public partial class ParsingProgressUserControlViewModel : ViewModelBase
    ,IRecipient<ParsingProgressMessage>
{
    [ObservableProperty] private double _progressValue;
    [ObservableProperty] private string _statusText = "Инициализация...";
    [ObservableProperty] private int _processedCount;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private bool _isFinished;
    [ObservableProperty] private ObservableCollection<string> _logs = [];
    [ObservableProperty] private bool _canClose;
    
    public ParsingProgressUserControlViewModel()
    {
        WeakReferenceMessenger.Default.Register(this);
    }
    
    public void Receive(ParsingProgressMessage message)
    {
        Dispatcher.UIThread.Post(() => 
        {
            StatusText = message.Status;
            ProcessedCount = message.Processed;
            TotalCount = message.Total;

            if (message.Total > 0)
            {
                ProgressValue = (double)message.Processed / message.Total * 100;
            }

            if (!string.IsNullOrEmpty(message.Log))
            {
                Logs.Add($"[{DateTime.Now:HH:mm:ss}] {message.Log}");
                if (Logs.Count > 100) Logs.RemoveAt(0);
            }

            if (ProgressValue >= 100 || message.Status.Contains("Завершен") || message.Status.Contains("Ошибка"))
            {
                IsFinished = true;
            }
        });
    }

    [RelayCommand]
    public void Close()
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseProgressModalMessage());
        
        Logs.Clear();
        ProgressValue = 0;
        IsFinished = false;
    }
}