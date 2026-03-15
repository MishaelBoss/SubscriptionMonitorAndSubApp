using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SubApp.Models;
using SubApp.Scripts;

namespace SubApp.ViewModels.Components;

public partial class ConfirmationSubscriptionCancellationUserControlViewModel(Subscription? sub) : ViewModelBase
{
    [RelayCommand]
    public async Task CancelSubscription()
    {
        if (sub == null) {
            Console.WriteLine("DEBUG: Sub пустой");
            return;
        }

        try 
        {
            var session = AuthService.CurrentSession;
            if (session == null)
            {
                Console.WriteLine("DEBUG: Сессия пустая, загрузка отменена.");
                return;
            }
            
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", session.Token);
            
            var updateData = new { status = "cancelled", is_active = false };
            var json = JsonSerializer.Serialize(updateData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PatchAsync($"http://10.0.2.2:8000/subscriptions/api/subscriptions/{sub.Id}/", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[API ERROR] Не удалось отменить на сервере: {err}");
            }
            
            WeakReferenceMessenger.Default.Send(new RefreshSubscriptionMessage());
            WeakReferenceMessenger.Default.Send(new OpenOrCloseConfirmationSubscriptionCancellationMessage());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при отмене подписки: {ex.Message}");
        }
    }
    
    [RelayCommand]
    public void Close()
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseConfirmationSubscriptionCancellationMessage());
    }
}