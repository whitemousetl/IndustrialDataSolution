using IndustrialDataProcessor.Domain.Workstation.Configs;
using IndustrialDataProcessor.Infrastructure.Helpers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IndustrialDataProcessor.Infrastructure.Serialization.Converters;

/// <summary>
/// 工作站配置 JSON 转换器
/// </summary>
public class WorkstationJsonConverter : JsonConverter<WorkstationConfig>
{
    public override WorkstationConfig? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var namePrefix = "Workstation的";

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // Id
        var id = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, namePrefix, nameof(WorkstationConfig.Id), JsonValueKind.String);
        // IpAddress
        var ip = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, namePrefix, nameof(WorkstationConfig.IpAddress), JsonValueKind.String);
        // 协议列表
        var protocols = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<List<ProtocolConfig>>(root, namePrefix, nameof(WorkstationConfig.Protocols), JsonValueKind.Array);
        // Name
        var name = JsonValidateHelper.ValidateOptionalFields<string?>(root, namePrefix, nameof(WorkstationConfig.Name), JsonValueKind.String);

        var config = new WorkstationConfig(id, name ?? string.Empty, ip);
        
        foreach (var protocol in protocols)
        {
            config.AddProtocol(protocol);
        }
        
        return config;
    }

    public override void Write(Utf8JsonWriter writer, WorkstationConfig value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}
