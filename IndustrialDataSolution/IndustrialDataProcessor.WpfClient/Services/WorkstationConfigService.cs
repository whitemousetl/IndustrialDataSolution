using IndustrialDataProcessor.Contracts.WorkstationDto;
using IndustrialDataProcessor.WpfClient.Interfaces;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace IndustrialDataProcessor.WpfClient.Services;

public class WorkstationConfigService : IWorkstationConfigService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl = "http://localhost:8899";

    public WorkstationConfigService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    public async Task<WorkstationConfigDto?> GetConfigAsync()
    {
        try
        {
            // 注意：后端需要添加 GET 接口
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/workstation-config");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<WorkstationConfigDto>>();
                return result?.Data;
            }
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<bool> SaveConfigAsync(WorkstationConfigDto config)
    {
        try
        {
            var json = JsonSerializer.Serialize(config);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/workstation-config", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/workstation-config");
            return true;
        }
        catch
        {
            return false;
        }
    }

    private class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }
}