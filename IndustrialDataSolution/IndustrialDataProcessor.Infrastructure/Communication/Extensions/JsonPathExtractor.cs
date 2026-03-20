using System.Text.Json;
using System.Text.RegularExpressions;

namespace IndustrialDataProcessor.Infrastructure.Communication.Extensions;

/// <summary>
/// JSON路径提取器
/// 支持点号表示法的JSON路径，如 store.book[0].title
/// </summary>
public static partial class JsonPathExtractor
{
    /// <summary>
    /// 从JSON字符串中按路径提取值
    /// </summary>
    /// <param name="json">JSON字符串</param>
    /// <param name="path">JSON路径，如 store.book[0].title</param>
    /// <returns>提取的值（作为object返回），未找到返回null</returns>
    public static object? ExtractValue(string json, string path)
    {
        if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(path))
            return null;

        try
        {
            using var document = JsonDocument.Parse(json);
            var element = NavigateToElement(document.RootElement, path);
            return element.HasValue ? ConvertToObject(element.Value) : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// 从JSON字符串中按路径提取值并转换为指定类型
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="json">JSON字符串</param>
    /// <param name="path">JSON路径</param>
    /// <returns>提取并转换后的值</returns>
    public static T? ExtractValue<T>(string json, string path)
    {
        var value = ExtractValue(json, path);
        if (value == null) return default;

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// 根据路径导航到JSON元素
    /// </summary>
    private static JsonElement? NavigateToElement(JsonElement root, string path)
    {
        var current = root;
        var segments = ParsePath(path);

        foreach (var segment in segments)
        {
            if (segment.IsArrayAccess)
            {
                // 处理数组访问，如 book[0]
                if (current.ValueKind != JsonValueKind.Object && 
                    current.ValueKind != JsonValueKind.Array)
                    return null;

                // 先获取属性（如果有）
                if (!string.IsNullOrEmpty(segment.PropertyName))
                {
                    if (current.ValueKind != JsonValueKind.Object)
                        return null;

                    if (!current.TryGetProperty(segment.PropertyName, out current))
                        return null;
                }

                // 再访问数组索引
                if (current.ValueKind != JsonValueKind.Array)
                    return null;

                var arrayLength = current.GetArrayLength();
                if (segment.ArrayIndex < 0 || segment.ArrayIndex >= arrayLength)
                    return null;

                current = current[segment.ArrayIndex];
            }
            else
            {
                // 处理普通属性访问
                if (current.ValueKind != JsonValueKind.Object)
                    return null;

                if (!current.TryGetProperty(segment.PropertyName, out current))
                    return null;
            }
        }

        return current;
    }

    /// <summary>
    /// 解析JSON路径为段列表
    /// </summary>
    private static List<PathSegment> ParsePath(string path)
    {
        var segments = new List<PathSegment>();
        var parts = path.Split('.');

        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
                continue;

            // 检查是否包含数组访问符 [index]
            var match = ArrayAccessRegex().Match(part);
            if (match.Success)
            {
                var propertyName = match.Groups[1].Value;
                var index = int.Parse(match.Groups[2].Value);
                segments.Add(new PathSegment(propertyName, index));
            }
            else
            {
                segments.Add(new PathSegment(part));
            }
        }

        return segments;
    }

    /// <summary>
    /// 将JsonElement转换为.NET对象
    /// </summary>
    private static object? ConvertToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => GetNumber(element),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.GetRawText(),
            JsonValueKind.Object => element.GetRawText(),
            _ => element.GetRawText()
        };
    }

    /// <summary>
    /// 从JsonElement获取数值类型
    /// </summary>
    private static object GetNumber(JsonElement element)
    {
        // 尝试按照精度从低到高的顺序解析
        if (element.TryGetInt32(out var intValue))
            return intValue;
        if (element.TryGetInt64(out var longValue))
            return longValue;
        if (element.TryGetDouble(out var doubleValue))
            return doubleValue;
        if (element.TryGetDecimal(out var decimalValue))
            return decimalValue;

        return element.GetRawText();
    }

    /// <summary>
    /// 匹配数组访问的正则表达式，如 book[0]
    /// </summary>
    [GeneratedRegex(@"^(\w*)\[(\d+)\]$")]
    private static partial Regex ArrayAccessRegex();

    /// <summary>
    /// 路径段结构
    /// </summary>
    private readonly struct PathSegment
    {
        public string PropertyName { get; }
        public int ArrayIndex { get; }
        public bool IsArrayAccess { get; }

        public PathSegment(string propertyName)
        {
            PropertyName = propertyName;
            ArrayIndex = -1;
            IsArrayAccess = false;
        }

        public PathSegment(string propertyName, int arrayIndex)
        {
            PropertyName = propertyName;
            ArrayIndex = arrayIndex;
            IsArrayAccess = true;
        }
    }
}
