using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SubApp.Data;
using SubApp.Scripts;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SubApp.ViewModels.Components;

public partial class ProfileUserControlViewModel : ViewModelBase
{
    [ObservableProperty] private string? _userName;
    [ObservableProperty] private string? _email;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(FullName))] private string? _firstName;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(FullName))] private string? _lastName;
    [ObservableProperty] private string? _phone;
    [ObservableProperty] private DateTime? _dateJoined;

    public string FullName => (string.IsNullOrEmpty(FirstName) && string.IsNullOrEmpty(LastName))
        ? "Имя не указано"
        : $"{LastName} {FirstName}";

    [RelayCommand]
    public void CloseProfile()
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseProfileMessage());
    }

    [RelayCommand]
    public void Logout()
    {
        AuthService.Logout();
    }

    public ProfileUserControlViewModel() 
    {
        Task.Run(async () => await InitializeAsync());
    }

    private async Task InitializeAsync()
    {
        await AuthService.TryAutoLoginAsync();

        var currentUserId = AuthService.CurrentSession?.Id;
        if (currentUserId == null) return;

        using var db = new AppDbContext();

        var user = db.Users.FirstOrDefault(u => u.Id == currentUserId);
        var profile = db.Profiles.FirstOrDefault(p => p.UserId == currentUserId);

        if (user != null)
        {
            UserName = !string.IsNullOrWhiteSpace(user.Username) ? user.Username : "Нет указано";
            FirstName = user.FirstName ?? "Не указано";
            LastName = user.LastName ?? "Не указано";
            Email = !string.IsNullOrWhiteSpace(user.Email) ? user.Email : "Нет указано";
            DateJoined = user.DateJoined;
        }

        if (profile != null)
        {
            Phone = !string.IsNullOrWhiteSpace(profile.Phone) ? profile.Phone : "Нет указано";
        }
    }
}
