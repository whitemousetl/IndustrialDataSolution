namespace IndustrialDataProcessor.WpfClient.Interfaces;

public interface IHttpClientService
{
    Task<T?> GetAsync<T>(string uri);
    Task<TResponse?> PostAsync<TRequest, TResponse>(string uri, TRequest data);
    Task<bool> PostAsync<TRequest>(string uri, TRequest data);
}
