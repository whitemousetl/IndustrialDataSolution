using System.Text.Json.Serialization;

namespace IndustrialDataProcessor.Api.Models;

/// <summary>
/// 统一 API 响应格式
/// </summary>
/// <typeparam name="T">响应数据类型</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// 是否成功
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// 状态码
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// 消息
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 响应数据
    /// </summary>
    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; set; }

    /// <summary>
    /// 时间戳
    /// </summary>
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    /// <summary>
    /// 创建成功响应
    /// </summary>
    public static ApiResponse<T> Ok(string message = "操作成功", T? data = default)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Code = 200,
            Message = message,
            Data = data
        };
    }

    /// <summary>
    /// 创建失败响应
    /// </summary>
    public static ApiResponse<T> Fail(string message, int code = 400)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Code = code,
            Message = message
        };
    }

    /// <summary>
    /// 创建服务器错误响应
    /// </summary>
    public static ApiResponse<T> Error(string message = "服务器内部错误")
    {
        return new ApiResponse<T>
        {
            Success = false,
            Code = 500,
            Message = message
        };
    }
}

/// <summary>
/// 无数据的 API 响应
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    /// <summary>
    /// 创建成功响应（无数据）
    /// </summary>
    public static ApiResponse Ok(string message = "操作成功")
    {
        return new ApiResponse
        {
            Success = true,
            Code = 200,
            Message = message
        };
    }

    /// <summary>
    /// 创建失败响应（无数据）
    /// </summary>
    public new static ApiResponse Fail(string message, int code = 400)
    {
        return new ApiResponse
        {
            Success = false,
            Code = code,
            Message = message
        };
    }

    /// <summary>
    /// 创建服务器错误响应（无数据）
    /// </summary>
    public new static ApiResponse Error(string message = "服务器内部错误")
    {
        return new ApiResponse
        {
            Success = false,
            Code = 500,
            Message = message
        };
    }
}
