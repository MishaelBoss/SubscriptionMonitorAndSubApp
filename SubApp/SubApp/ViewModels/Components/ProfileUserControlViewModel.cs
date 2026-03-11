using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SubApp.Data;
using SubApp.Scripts;
using System;
using System.Linq;
using System.Threading.Tasks;
using SubApp.Models;

namespace SubApp.ViewModels.Components;

public partial class ProfileUserControlViewModel : ViewModelBase
{
    [ObservableProperty] private string? _userName;
    [ObservableProperty] private string? _email;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(FullName))] private string? _firstName;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(FullName))] private string? _lastName;
    [ObservableProperty] private string? _phone;
    [ObservableProperty] private DateTime? _dateJoined;

    private User? User { get; set; }
    private Profile? Profile { get; set; }

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

    [RelayCommand]
    public void Edit()
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseEditUserAndProfileMessage(User, Profile));
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

        await using var db = new AppDbContext();

        User = db.Users.FirstOrDefault(u => u.Id == currentUserId);
        Profile = db.Profiles.FirstOrDefault(p => p.UserId == currentUserId);

        if (User != null)
        {
            UserName = !string.IsNullOrWhiteSpace(User?.Username) ? User.Username : "Нет указано";
            FirstName = User?.FirstName ?? "Не указано";
            LastName = User?.LastName ?? "Не указано";
            Email = !string.IsNullOrWhiteSpace(User?.Email) ? User.Email : "Нет указано";
            DateJoined = User?.DateJoined;
        }

        if (Profile != null)
        {
            Phone = !string.IsNullOrWhiteSpace(Profile.Phone) ? Profile.Phone : "Нет указано";
        }
    }
}
