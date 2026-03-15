using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SubApp.Data;
using SubApp.Scripts;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
        WeakReferenceMessenger.Default.Send(new OpenOrCloseConfirmLogoutMessage());
    }

    [RelayCommand]
    public void Edit()
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseEditUserAndProfileMessage());
    }

    public ProfileUserControlViewModel()
    {
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await AuthService.TryAutoLoginAsync();

        var currentUserId = AuthService.CurrentSession?.Id;
        if (currentUserId == null) return;

        var client = new HttpClient();
        const string url = "http://10.0.2.2:8000/accounts/api/profile/";
        
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", AuthService.CurrentSession?.Token);
        
        var data = await client.GetFromJsonAsync<UserProfileDto>(url);
        if (data != null)
        {
            UserName = data.username;
            FirstName = data.first_name;
            LastName = data.last_name;
            Email = data.email;
            Phone = data.phone;
            DateJoined = data.date_joined;
        }
    }
}
