using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))] private string? _userName;
    [ObservableProperty] private string? _lastName;
    [ObservableProperty] private string? _firstName;
    [ObservableProperty] private string? _phone;
    [ObservableProperty] private string? _email;
    [ObservableProperty] private bool _emailNotifications;
    [ObservableProperty] private bool _pushNotifications;
    
    private readonly string _apiUrl = "http://10.0.2.2:8000/accounts/api/profile/";

    public bool IsActiveConfirmButton
        => !string.IsNullOrEmpty(UserName);
    
    private async Task LoadProfileAsync()
    {
        try
        {
            using var client = new HttpClient();
            
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", AuthService.CurrentSession?.Token);

            var data = await client.GetFromJsonAsync<UserProfileDto>(_apiUrl);
            if (data != null)
            {
                UserName = data.username;
                FirstName = data.first_name;
                LastName = data.last_name;
                Email = data.email;
                Phone = data.phone;
                EmailNotifications = data.email_notifications;
                PushNotifications = data.push_notifications;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        if (string.IsNullOrEmpty(UserName))
        {
            ErrorEdit = "Введите логин";
            return;
        }

        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", AuthService.CurrentSession?.Token);
            
            var updateData = new UserProfileDto {
                username = UserName,
                first_name = FirstName ?? "",
                last_name = LastName ?? "",
                email = Email ?? "",
                phone = Phone ?? "",
                email_notifications = EmailNotifications,
                push_notifications = PushNotifications
            };
            var json = System.Text.Json.JsonSerializer.Serialize(updateData);
            var buffer = System.Text.Encoding.UTF8.GetBytes(json);
            var content = new ByteArrayContent(buffer);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var request = new HttpRequestMessage(HttpMethod.Put, _apiUrl);
            request.Version = new Version(1, 1);
            request.Content = content;
            
            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode) 
            {
                Close();
            } 
            else 
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Ошибка сохранения: {error}");
                ErrorEdit = "Ошибка при сохранении";
            }
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

    public EditProfileUserControlViewModel()
    {
        _ = LoadProfileAsync();
    }
}
