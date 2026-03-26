using System.Text.Json;
using System.Text.RegularExpressions;

namespace IndustrialDataProcessor.Infrastructure.Communication.Extensions;

/// <summary>
/// JSON路径提取器
/// 支持两种取值模式：
///   1. 位置取值（原有）：按路径和数组下标定位，如 store.book[0].title
///   2. 条件取值（新增）：在数组中按字段值条件筛选后提取，格式为
///      [?key1=val1&amp;key2=val2&amp;key3=val3].TargetField
///      示例：[?StationCode=20&amp;StationName=Line 1&amp;Detail=ChangeProfileStatus].Value
///      条件间用 &amp; 分隔，key=value 精确匹配（忽略大小写），匹配第一个满足所有条件的元素
///      两种模式可以混合使用，如 data.[?Detail=Uptime].Value
/// </summary>
public static partial class JsonPathExtractor
{
    /// <summary>
    /// 从JSON字符串中按路径提取值
    /// 支持位置取值（store.book[0].title）和条件取值（[?StationCode=20&amp;Detail=Uptime].Value）
    /// </summary>
    /// <param name="json">JSON字符串</param>
    /// <param name="path">JSON路径</param>
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
    /// 根据路径导航到JSON元素，支持位置访问和条件筛选两种模式
    /// </summary>
    private static JsonElement? NavigateToElement(JsonElement root, string path)
    {
        var current = root;
        var segments = ParsePath(path);

        foreach (var segment in segments)
        {
            if (segment.IsConditionalAccess)
            {
                // 条件筛选模式：在数组中找到满足所有条件的第一个元素
                if (current.ValueKind != JsonValueKind.Array)
                    return null;

                JsonElement? matched = null;
                foreach (var item in current.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object) continue;

                    var allMatch = true;
                    foreach (var (condKey, condValue) in segment.Conditions!)
                    {
                        if (!item.TryGetProperty(condKey, out var condProp))
                        {
                            allMatch = false;
                            break;
                        }
                        var actualStr = ConvertToObject(condProp)?.ToString() ?? string.Empty;
                        if (!string.Equals(actualStr, condValue, StringComparison.OrdinalIgnoreCase))
                        {
                            allMatch = false;
                            break;
                        }
                    }

                    if (!allMatch) continue;
                    matched = item;
                    break;
                }

                if (!matched.HasValue) return null;
                current = matched.Value;
            }
            else if (segment.IsArrayAccess)
            {
                // 位置访问模式：按数组下标访问，如 book[0]
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
                // 普通属性访问
                if (current.ValueKind != JsonValueKind.Object)
                    return null;

                if (!current.TryGetProperty(segment.PropertyName, out current))
                    return null;
            }
        }

        return current;
    }

    /// <summary>
    /// 解析JSON路径为段列表，支持普通属性、数组下标、条件筛选三种段类型
    /// </summary>
    private static List<PathSegment> ParsePath(string path)
    {
        var segments = new List<PathSegment>();
        var parts = path.Split('.');

        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
                continue;

            // 优先检查条件筛选访问符 [?key=val&key2=val2]
            var conditionalMatch = ConditionalAccessRegex().Match(part);
            if (conditionalMatch.Success)
            {
                var conditionStr = conditionalMatch.Groups[1].Value;
                var conditions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var condition in conditionStr.Split('&'))
                {
                    var eqIdx = condition.IndexOf('=');
                    if (eqIdx <= 0) continue;
                    var key = condition[..eqIdx].Trim();
                    var value = condition[(eqIdx + 1)..].Trim();
                    if (!string.IsNullOrEmpty(key))
                        conditions[key] = value;
                }
                segments.Add(new PathSegment(conditions));
                continue;
            }

            // 检查是否包含数组下标访问符 [index]
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
    /// 匹配数组下标访问的正则表达式，如 book[0] 或 [0]
    /// </summary>
    [GeneratedRegex(@"^(\w*)\[(\d+)\]$")]
    private static partial Regex ArrayAccessRegex();

    /// <summary>
    /// 匹配条件筛选访问的正则表达式，如 [?StationCode=20&amp;Detail=Uptime]
    /// </summary>
    [GeneratedRegex(@"^\[\?(.+)\]$")]
    private static partial Regex ConditionalAccessRegex();

    /// <summary>
    /// 路径段结构，支持三种访问模式：普通属性、数组下标、条件筛选
    /// </summary>
    private readonly struct PathSegment
    {
        public string PropertyName { get; }
        public int ArrayIndex { get; }
        public bool IsArrayAccess { get; }
        /// <summary>是否为条件筛选模式 [?key=val&...]</summary>
        public bool IsConditionalAccess { get; }
        /// <summary>条件筛选模式时的条件字典（key忽略大小写）</summary>
        public Dictionary<string, string>? Conditions { get; }

        public PathSegment(string propertyName)
        {
            PropertyName = propertyName;
            ArrayIndex = -1;
            IsArrayAccess = false;
            IsConditionalAccess = false;
            Conditions = null;
        }

        public PathSegment(string propertyName, int arrayIndex)
        {
            PropertyName = propertyName;
            ArrayIndex = arrayIndex;
            IsArrayAccess = true;
            IsConditionalAccess = false;
            Conditions = null;
        }

        /// <summary>创建条件筛选段</summary>
        public PathSegment(Dictionary<string, string> conditions)
        {
            PropertyName = string.Empty;
            ArrayIndex = -1;
            IsArrayAccess = false;
            IsConditionalAccess = true;
            Conditions = conditions;
        }
    }
}
