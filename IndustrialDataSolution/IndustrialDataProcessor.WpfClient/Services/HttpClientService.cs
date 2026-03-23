using IndustrialDataProcessor.WpfClient.Interfaces;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace IndustrialDataProcessor.WpfClient.Services;

public class HttpClientService : IHttpClientService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUri = "http://localhost:8899";

    public HttpClientService()
    {
        _httpClient = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }
    public async Task<T?> GetAsync<T>(string uri)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUri}/{uri}");

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>();
        }
        catch (Exception)
        {
            return default;
        }
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string uri, TRequest data)
    {
        try
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUri}{uri}", content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TResponse>();
        }
        catch (Exception)
        {
            return default;
        }
    }

    public async Task<bool> PostAsync<TRequest>(string uri, TRequest data)
    {
        try
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUri}/{uri}", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
        
    }
}
