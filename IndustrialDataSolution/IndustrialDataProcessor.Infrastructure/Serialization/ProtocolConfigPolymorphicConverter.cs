using IndustrialDataProcessor.Domain.Enums;
using IndustrialDataProcessor.Domain.Workstation.Configs;
using IndustrialDataProcessor.Domain.Workstation.Configs.ProtocolSub;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IndustrialDataProcessor.Infrastructure.Serialization;

/// <summary>
/// ProtocolConfig 多态 JSON 转换器
/// 根据 InterfaceType 鉴别器字段，将 JSON 反序列化为对应的派生类型
/// </summary>
public class ProtocolConfigPolymorphicConverter : JsonConverter<ProtocolConfig>
{
    public override ProtocolConfig? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // 将 JSON 读取为 JsonDocument 以便检查属性
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // 获取鉴别器字段 InterfaceType (兼容大小写)
        if (!root.TryGetProperty("InterfaceType", out var typeElement) &&
            !root.TryGetProperty("interfaceType", out typeElement))
            throw new JsonException("反序列化失败: JSON 中缺少鉴别器属性 'InterfaceType'。");

        // 解析 InterfaceType (兼容数字和字符串格式的枚举)
        InterfaceType interfaceType;
        if (typeElement.ValueKind == JsonValueKind.Number)
            interfaceType = (InterfaceType)typeElement.GetInt32();
        else if (typeElement.ValueKind == JsonValueKind.String)
        {
            if (!Enum.TryParse(typeElement.GetString(), true, out interfaceType))
                throw new JsonException($"未知的 InterfaceType 值: {typeElement.GetString()}");
        }
        else
            throw new JsonException("InterfaceType 格式无效。");

        var rawText = root.GetRawText();

        // 根据 InterfaceType 反序列化为具体的派生类
        return interfaceType switch
        {
            InterfaceType.LAN => JsonSerializer.Deserialize<NetworkProtocolConfig>(rawText, options),
            InterfaceType.COM => JsonSerializer.Deserialize<SerialPortConfig>(rawText, options),
            InterfaceType.DATABASE => JsonSerializer.Deserialize<DatabaseInterfaceConfig>(rawText, options),
            InterfaceType.API => JsonSerializer.Deserialize<HttpApiInterfaceConfig>(rawText, options),
            _ => throw new NotSupportedException($"不支持的接口类型: {interfaceType}")
        };
    }

    public override void Write(Utf8JsonWriter writer, ProtocolConfig value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        // 将 value 作为其运行时类型进行序列化，从而包含派生类型的所有属性
        JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
    }
}