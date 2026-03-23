using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks; 
using SubApp.Models;
using SubApp.Scripts;

namespace SubApp.Data;

public class ApiService
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };

    public ApiService(string token)
    {
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", token);
    }

    public async Task<List<Subscription>> GetSubscriptionsAsync()
    {
        var url = $"{AppConfig.BaseUrl}/subscriptions/api/subscriptions/";
        return await _http.GetFromJsonAsync<List<Subscription>>(url) ?? [];
    }
    
    public async Task<List<ParsedEmail>> GetParsedEmailsAsync()
    {        
        var url = $"{AppConfig.BaseUrl}/mail/api/emails/";
        return await _http.GetFromJsonAsync<List<ParsedEmail>>(url) ?? [];
    }
    
    public async Task<List<Mailbox>> GetMailBoxAsync()
    {        
        var url = $"{AppConfig.BaseUrl}/mail/api/mailboxes/";
        return await _http.GetFromJsonAsync<List<Mailbox>>(url) ?? [];
    }
    
    public async Task<List<ParsedEmail>> GetEmailsForSubscriptionAsync(int subscriptionId)
    {
        var url = $"{AppConfig.BaseUrl}/mail/api/emails/?subscription_id={subscriptionId}";
        return await _http.GetFromJsonAsync<List<ParsedEmail>>(url) ?? [];
    }

    public async Task UpdateSubscriptionAsync(Subscription sub)
    {
        var url = $"{AppConfig.BaseUrl}/subscriptions/api/subscriptions/{sub.Id}/";
        await _http.PutAsJsonAsync(url, sub);
    }
    
    public async Task<bool> DeleteMailboxAsync(int mailboxId)
    {
        var url = $"{AppConfig.BaseUrl}/mail/api/mailboxes/{mailboxId}/";
        var response = await _http.DeleteAsync(url);
        return response.IsSuccessStatusCode;
    }
}