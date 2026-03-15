using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using SubApp.Models;

namespace SubApp.Data;

public class ApiService
{
    private readonly HttpClient _http = new();
    private const string BaseUrl = "http://10.0.2.2:8000";

    public ApiService(string token)
    {
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", token);
    }

    public async Task<List<Subscription>> GetSubscriptionsAsync()
    {
        var url = $"{BaseUrl}/subscriptions/api/subscriptions/";
        return await _http.GetFromJsonAsync<List<Subscription>>(url) ?? [];
        
    }
    
    public async Task<List<ParsedEmail>> GetParsedEmailsAsync()
    {        
        var url = $"{BaseUrl}/mail/api/emails/";
        return await _http.GetFromJsonAsync<List<ParsedEmail>>(url) ?? [];
    }

    public async Task UpdateSubscriptionAsync(Subscription sub)
    {
        var url = $"{BaseUrl}/subscriptions/api/subscriptions/{sub.Id}/";
        await _http.PutAsJsonAsync(url, sub);
    }
}