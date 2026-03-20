using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using IndustrialDataProcessor.Domain.Communication.IConnection;
using IndustrialDataProcessor.Domain.Enums;
using IndustrialDataProcessor.Domain.Workstation.Configs;
using IndustrialDataProcessor.Domain.Workstation.Results;
using IndustrialDataProcessor.Infrastructure.Communication.Connection;
using IndustrialDataProcessor.Infrastructure.Communication.Drivers.TcpCommon;
using IndustrialDataProcessor.Infrastructure.Communication.Extensions;

namespace IndustrialDataProcessor.Infrastructure.Communication.Drivers.Api;

/// <summary>
/// HTTP API 协议驱动
/// 将API视为一种"协议"进行数据采集，根据JSON路径提取数据
/// </summary>
public class ApiDriver : BaseProtocolDriver<HttpClient>
{
    /// <summary>
    /// JSON响应缓存（协议ID -> 缓存条目）
    /// 用于在同一采集周期内避免重复请求
    /// </summary>
    private readonly ConcurrentDictionary<string, JsonCacheEntry> _jsonCache = new();

    /// <summary>
    /// 缓存有效期（毫秒），默认与通讯延时一致
    /// </summary>
    private const int DefaultCacheExpiryMs = 1000;

    /// <summary>
    /// 读取单个点位
    /// </summary>
    protected override async Task<PointResult> ReadPointCoreAsync(
        IConnectionHandle handle,
        ParameterConfig point,
        CancellationToken token)
    {
        var result = new PointResult
        {
            Address = point.Address,
            Label = point.Label,
            DataType = point.DataType
        };

        try
        {
            // 1. 获取API连接句柄
            if (handle is not HttpApiConnectionHandle apiHandle)
            {
                result.ReadIsSuccess = false;
                result.ErrorMsg = "连接句柄类型不正确，期望 HttpApiConnectionHandle";
                return result;
            }

            // 2. 获取JSON响应（带缓存）
            var jsonResponse = await GetJsonResponseWithCacheAsync(apiHandle, token);

            if (string.IsNullOrWhiteSpace(jsonResponse))
            {
                result.ReadIsSuccess = false;
                result.ErrorMsg = "API返回空响应";
                return result;
            }

            // 3. 按Address（JSON路径）提取值
            var extractedValue = JsonPathExtractor.ExtractValue(jsonResponse, point.Address);

            if (extractedValue == null)
            {
                result.ReadIsSuccess = false;
                result.ErrorMsg = $"无法从JSON中提取路径 '{point.Address}' 的值";
                return result;
            }

            result.ReadIsSuccess = true;
            result.Value = extractedValue;
            return result;
        }
        catch (HttpRequestException ex)
        {
            result.ReadIsSuccess = false;
            result.ErrorMsg = $"HTTP请求失败: {ex.Message}";
            return result;
        }
        catch (TaskCanceledException ex) when (!token.IsCancellationRequested)
        {
            result.ReadIsSuccess = false;
            result.ErrorMsg = $"HTTP请求超时: {ex.Message}";
            return result;
        }
        catch (Exception ex)
        {
            result.ReadIsSuccess = false;
            result.ErrorMsg = $"API读取异常: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// 写入单个点位（API协议通常不支持写入，返回false）
    /// </summary>
    protected override Task<bool> WritePointCoreAsync(
        IConnectionHandle handle,
        ParameterConfig point,
        object value,
        CancellationToken token)
    {
        // API协议通常不支持写入，如需支持可在此实现POST/PUT请求
        return Task.FromResult(false);
    }

    /// <summary>
    /// 获取JSON响应（带缓存机制）
    /// </summary>
    private async Task<string> GetJsonResponseWithCacheAsync(
        HttpApiConnectionHandle apiHandle,
        CancellationToken token)
    {
        var cacheKey = apiHandle.AccessApiString;

        // 检查缓存是否有效
        if (_jsonCache.TryGetValue(cacheKey, out var cacheEntry) && !cacheEntry.IsExpired)
        {
            return cacheEntry.JsonContent;
        }

        // 发起HTTP请求
        var httpClient = apiHandle.GetRawConnection<HttpClient>();
        var jsonResponse = await SendRequestAsync(httpClient, apiHandle, token);

        // 更新缓存
        _jsonCache[cacheKey] = new JsonCacheEntry(jsonResponse, DefaultCacheExpiryMs);

        return jsonResponse;
    }

    /// <summary>
    /// 发送HTTP请求
    /// </summary>
    private static async Task<string> SendRequestAsync(
        HttpClient httpClient,
        HttpApiConnectionHandle apiHandle,
        CancellationToken token)
    {
        var url = apiHandle.AccessApiString;

        // 如果有网关，使用网关作为基础URL
        if (!string.IsNullOrWhiteSpace(apiHandle.Gateway))
        {
            url = $"{apiHandle.Gateway.TrimEnd('/')}/{url.TrimStart('/')}";
        }

        HttpResponseMessage response;

        switch (apiHandle.RequestMethod)
        {
            case RequestMethod.Get:
                response = await httpClient.GetAsync(url, token);
                break;

            case RequestMethod.Post:
                // POST请求，如果需要请求体可以扩展
                var postContent = new StringContent("{}", Encoding.UTF8, "application/json");
                response = await httpClient.PostAsync(url, postContent, token);
                break;

            case RequestMethod.Put:
                var putContent = new StringContent("{}", Encoding.UTF8, "application/json");
                response = await httpClient.PutAsync(url, putContent, token);
                break;

            case RequestMethod.Delete:
                response = await httpClient.DeleteAsync(url, token);
                break;

            default:
                response = await httpClient.GetAsync(url, token);
                break;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(token);
    }

    /// <summary>
    /// 清除指定协议的JSON缓存
    /// </summary>
    public void ClearCache(string apiUrl)
    {
        _jsonCache.TryRemove(apiUrl, out _);
    }

    /// <summary>
    /// 清除所有JSON缓存
    /// </summary>
    public void ClearAllCache()
    {
        _jsonCache.Clear();
    }

    /// <summary>
    /// 释放资源时清除缓存
    /// </summary>
    public override void Dispose()
    {
        _jsonCache.Clear();
        base.Dispose();
    }

    /// <summary>
    /// JSON缓存条目
    /// </summary>
    private class JsonCacheEntry
    {
        public string JsonContent { get; }
        public DateTime ExpiryTime { get; }
        public bool IsExpired => DateTime.UtcNow > ExpiryTime;

        public JsonCacheEntry(string jsonContent, int expiryMs)
        {
            JsonContent = jsonContent;
            ExpiryTime = DateTime.UtcNow.AddMilliseconds(expiryMs);
        }
    }
}
