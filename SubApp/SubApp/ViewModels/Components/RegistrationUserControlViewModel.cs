using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SubApp.Data;
using SubApp.Models;
using SubApp.Scripts;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Devices;

namespace SubApp.ViewModels.Components;

public partial class RegistrationUserControlViewModel : ViewModelBase
{
    [ObservableProperty] private string _registrationError = string.Empty;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))] private string _username = string.Empty;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))] private string _email = string.Empty;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))] private string _password = string.Empty;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))] private string _confirmPassword = string.Empty;

    public bool IsActiveConfirmButton
        => !string.IsNullOrEmpty(Username)
        && !string.IsNullOrEmpty(Password)
        && Password == ConfirmPassword;

    [RelayCommand]
    public async Task RegisterAsync()
    {
        if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
        {
            RegistrationError = "Введите логин и пароль!";

            return;
        }
        
        if (Password != ConfirmPassword)
        {
            RegistrationError = "Пароли не совпадают!";
            return;
        }
        
        Console.WriteLine($"Начало регистрации пользователя: {Username}");

        try
        {
            using var client = new HttpClient();
            var url = DeviceInfo.Platform == DevicePlatform.Android 
                ? "http://10.0.2.2:8000/accounts/api/register/" 
                : "http://127.0.0.1:8000/accounts/api/register/";
            
            var regData = new
            {
                username = Username, 
                password = Password, 
                email = Email
            };
            
            var json = JsonSerializer.Serialize(regData);
            var request = new HttpRequestMessage(HttpMethod.Post, url);

            request.Version = new Version(1, 0); 
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<AuthService.TokenResponse>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (result != null)
                {
                    Console.WriteLine($"Регистрация успешна для {Username}");
                    
                    AuthService.SetSession(result.Token, Username); 
                    WeakReferenceMessenger.Default.Send(new OpenOrCloseRegistrationMessage());
                    WeakReferenceMessenger.Default.Send(new UserLoggedInMessage());
                    ClearForm();
                }
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine(error);
                RegistrationError = "Ошибка: " + error;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            RegistrationError = "Нет связи с сервером";
        }
    }

    [RelayCommand]
    public void OpenLogin() 
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseLoginMessage());
    }

    private void ClearForm() 
    {
        RegistrationError = string.Empty;
        Username = string.Empty;
        Password = string.Empty;
        ConfirmPassword = string.Empty;
    }
}
