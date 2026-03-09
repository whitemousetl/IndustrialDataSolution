using IndustrialDataProcessor.Domain.Workstation.Configs;
using IndustrialDataProcessor.Infrastructure.Helpers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IndustrialDataProcessor.Infrastructure.Serialization.Converters;

public class WorkstationJsonConverter : JsonConverter<WorkstationConfig>
{
    public override WorkstationConfig? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var namePrefix = "Workstatoin的";

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        //Id
        var id = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, namePrefix, nameof(WorkstationConfig.Id), JsonValueKind.String);
        //IpAddress
        var ip = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, namePrefix, nameof(WorkstationConfig.IpAddress), JsonValueKind.String);
        //协议列表
        var protocols = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<List<ProtocolConfig>>(root, namePrefix, nameof(WorkstationConfig.Protocols), JsonValueKind.Array);
        //Name
        var name = JsonValidateHelper.ValidateOptionalFields<string?>(root, namePrefix, nameof(WorkstationConfig.Name), JsonValueKind.String);

        return new WorkstationConfig
        {
            Id = id,
            Name = name ?? string.Empty,
            IpAddress = ip,
            Protocols = protocols
        };
    }

    public override void Write(Utf8JsonWriter writer, WorkstationConfig value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
