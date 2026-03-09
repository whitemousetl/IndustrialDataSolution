using IndustrialDataProcessor.Domain.Enums;
using IndustrialDataProcessor.Domain.Workstation.Configs;
using IndustrialDataProcessor.Infrastructure.Helpers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IndustrialDataProcessor.Infrastructure.Serialization.Converters;

public class EquipmentJsonConverter : JsonConverter<EquipmentConfig>
{
    public override EquipmentConfig? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var namePrefix = "Equipment的";

        var id = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, namePrefix, nameof(EquipmentConfig.Id), JsonValueKind.String);

        var isCollect = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<bool>(root, namePrefix, nameof(EquipmentConfig.IsCollect), JsonValueKind.True);

        var parameters = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<List<ParameterConfig>>(root, namePrefix, nameof(EquipmentConfig.Parameters), JsonValueKind.Array);

        var equipmentType = JsonValidateHelper.EnsurePropertyExistsAndEnumIsRight<EquipmentType>(root, namePrefix, nameof(EquipmentConfig.EquipmentType));

        //Name如果存在，必须为字符串
        var name = JsonValidateHelper.ValidateOptionalFields<string?>(root, namePrefix, nameof(EquipmentConfig.Name), JsonValueKind.String) ?? string.Empty;

        return new EquipmentConfig
        {
            Id = id,
            IsCollect = isCollect,
            Name = name,
            EquipmentType = equipmentType,
            Parameters = parameters
        };
    }

    public override void Write(Utf8JsonWriter writer, EquipmentConfig value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
