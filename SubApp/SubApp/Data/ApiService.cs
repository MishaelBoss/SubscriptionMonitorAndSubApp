using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Devices;
using SubApp.Models;

namespace SubApp.Data;

public class ApiService
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private readonly string _baseUrl = DeviceInfo.Platform == DevicePlatform.Android 
        ? "http://10.0.2.2:8000" 
        : "http://127.0.0.1:8000";

    public ApiService(string token)
    {
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", token);
    }

    public async Task<List<Subscription>> GetSubscriptionsAsync()
    {
        var url = $"{_baseUrl}/subscriptions/api/subscriptions/";
        return await _http.GetFromJsonAsync<List<Subscription>>(url) ?? [];
        
    }
    
    public async Task<List<ParsedEmail>> GetParsedEmailsAsync()
    {        
        var url = $"{_baseUrl}/mail/api/emails/";
        return await _http.GetFromJsonAsync<List<ParsedEmail>>(url) ?? [];
    }

    public async Task UpdateSubscriptionAsync(Subscription sub)
    {
        var url = $"{_baseUrl}/subscriptions/api/subscriptions/{sub.Id}/";
        await _http.PutAsJsonAsync(url, sub);
    }
}