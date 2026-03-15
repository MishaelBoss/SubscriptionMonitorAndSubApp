using CommunityToolkit.Mvvm.Messaging;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.EntityFrameworkCore;
using SubApp.Data;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace SubApp.Scripts;

public static class AuthService
{
    public static UserSession? CurrentSession { get; private set; }
    public static bool IsLoggedIn => CurrentSession != null;

    public static async Task<bool> LoginAsync(string username, string password)
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.TransferEncodingChunked = false;
            
            var url = "http://10.0.2.2:8000/api-token-auth/";
            
            var loginData = new
            {
                username = username, 
                password = password
            };
            
            var json = JsonSerializer.Serialize(loginData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content);

            if (!response.IsSuccessStatusCode) 
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Ошибка сервера: {errorContent}");
                return false;
            }
            
            var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
            if (result == null || string.IsNullOrEmpty(result.Token)) return false;
            
            CurrentSession = new UserSession(0, username, result.Token);

            await SecureStorage.SetAsync("auth_token", result.Token);
            await SecureStorage.SetAsync("username", username);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return false;
        }
    }

    public static async Task<bool> TryAutoLoginAsync()
    {
        try
        {
            var token = await SecureStorage.GetAsync("auth_token");
            var username = await SecureStorage.GetAsync("username");
            var userIdStr = await SecureStorage.GetAsync("current_user_id");
            
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(username))
                return false;

            CurrentSession = new UserSession(long.Parse(userIdStr ?? "0"), username, token);

            return true;
        }
        catch
        {
            return false;
        }
    }
    
    public static void SetSession(string token, string username)
    {
        CurrentSession = new UserSession(0, username, token);

        Task.Run(async () => {
            await SecureStorage.SetAsync("auth_token", token);
            await SecureStorage.SetAsync("username", username);
        });
    }

    public static void Logout()
    {
        SecureStorage.Remove("auth_token");
        SecureStorage.Remove("username");
        SecureStorage.Remove("current_user_id");
        CurrentSession = null;
        WeakReferenceMessenger.Default.Send(new OpenOrCloseLoginMessage());
        WeakReferenceMessenger.Default.Send(new UserLoggedInMessage());
    }

    public class TokenResponse
    {
        [JsonPropertyName("token")]
        public string Token
        {
            get; set;
        }
    }
}
