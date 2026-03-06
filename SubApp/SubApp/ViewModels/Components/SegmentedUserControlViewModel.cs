using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SubApp.Scripts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SubApp.ViewModels.Components;

public partial class SegmentedUserControlViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<SegmentedButtonViewModel> _buttons = [];

    [RelayCommand]
    public void OpenHome()
    {
        WeakReferenceMessenger.Default.Send(new OpenHomePageMessage());
    }

    [RelayCommand]
    public void OpenSubscriptions() 
    {
        WeakReferenceMessenger.Default.Send(new OpenSubscriptionsPageMessage());
    }

    [RelayCommand]
    public void OpenEmails() 
    {
        WeakReferenceMessenger.Default.Send(new OpenEmailsPageMessage());
    }

    [RelayCommand]
    public void OpenAnalytics() 
    {
        WeakReferenceMessenger.Default.Send(new OpenAnalyticsPageMessage());
    }

    public SegmentedUserControlViewModel() 
    {
        Buttons.Clear();

        var newButtons = new List<SegmentedButtonViewModel>
        {
            new("Главная",OpenHomeCommand,  LoadBitmap("avares://SubApp/Assets/speedometer-64.png")),
            new("Подписки", OpenSubscriptionsCommand, LoadBitmap("avares://SubApp/Assets/list-64.png")),
            new("Почта", OpenEmailsCommand, LoadBitmap("avares://SubApp/Assets/email-64.png")),
            new("Аналитика", OpenAnalyticsCommand, LoadBitmap("avares://SubApp/Assets/schedule-64.png")),
        };

        foreach (var btn in newButtons)
        {
            Buttons.Add(btn);
        }
    }

    private static Bitmap LoadBitmap(string uriString)
    {
        using var stream = AssetLoader.Open(new Uri(uriString));
        return new Bitmap(stream);
    }
}
