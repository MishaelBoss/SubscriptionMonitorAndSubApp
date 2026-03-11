using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using SubApp.Data;
using SubApp.Models;
using SubApp.Scripts;

namespace SubApp.ViewModels.Components;

public partial class EditProfileUserControlViewModel : ViewModelBase
{
    [ObservableProperty] private string? _errorEdit = string.Empty;
    [ObservableProperty] private string? _userName;
    [ObservableProperty] private string? _lastName;
    [ObservableProperty] private string? _firstName;
    [ObservableProperty] private string? _phone;
    [ObservableProperty] private string? _email;
    [ObservableProperty] private bool? _emailNotifications;
    [ObservableProperty] private bool? _pushNotifications;
    
    private User? User { get; }
    private Profile? Profile { get; }

    public bool IsActiveConfirmButton
        => !string.IsNullOrEmpty(UserName);

    [RelayCommand]
    public async Task SaveAsync()
    {
        if(User == null || Profile == null) return;
        
        if (string.IsNullOrEmpty(UserName))
        {
            ErrorEdit = "Введите логин";
            return;
        }

        try
        {
            await using var db = new AppDbContext();
            
            db.Users.Attach(User);
            db.Profiles.Attach(Profile);
            
            User?.Username = UserName;
            User?.LastName = LastName ?? string.Empty;
            User?.FirstName = FirstName ?? string.Empty;
            Profile?.Phone = Phone ?? string.Empty;
            User?.Email = Email ?? string.Empty;
            Profile?.EmailNotifications = EmailNotifications ?? false;
            Profile?.PushNotifications = PushNotifications ?? false;
        
            await db.SaveChangesAsync();
            Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    [RelayCommand]
    public void Close()
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseEditUserAndProfileMessage());
    }

    public EditProfileUserControlViewModel(User? user, Profile? profile)
    {
        User = user;
        Profile = profile;
        
        if (User != null)
        {
            UserName = !string.IsNullOrWhiteSpace(User?.Username) ? User.Username : "Нет указано";
            FirstName = User?.FirstName ?? "Не указано";
            LastName = User?.LastName ?? "Не указано";
            Email = !string.IsNullOrWhiteSpace(User?.Email) ? User.Email : "Нет указано";
        }

        if (Profile != null)
        {
            Phone = !string.IsNullOrWhiteSpace(Profile.Phone) ? Profile.Phone : "Нет указано";
            EmailNotifications = Profile.EmailNotifications || false;
            PushNotifications = Profile.PushNotifications || false;
        }
    }
}
