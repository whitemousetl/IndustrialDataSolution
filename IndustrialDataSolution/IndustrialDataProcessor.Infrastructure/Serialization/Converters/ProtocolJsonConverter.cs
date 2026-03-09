using HslCommunication.Core;
using IndustrialDataProcessor.Domain.Attributes;
using IndustrialDataProcessor.Domain.Enums;
using IndustrialDataProcessor.Domain.Workstation.Configs;
using IndustrialDataProcessor.Domain.Workstation.Configs.ProtocolSub;
using IndustrialDataProcessor.Infrastructure.Helpers;
using System.IO.Ports;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IndustrialDataProcessor.Infrastructure.Serialization.Converters;

public class ProtocolJsonConverter : JsonConverter<ProtocolConfig>
{
    public override ProtocolConfig? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var namePrefix = "Protocol的";

        //Id
        var id = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, namePrefix, nameof(ProtocolConfig.Id), JsonValueKind.String);
        //interfaceType
        var interfaceType = JsonValidateHelper.EnsurePropertyExistsAndEnumIsRight<InterfaceType>(root, namePrefix, nameof(ProtocolConfig.InterfaceType));
        //protocolType
        var protocolType = JsonValidateHelper.EnsurePropertyExistsAndEnumIsRight<ProtocolType>(root, namePrefix, nameof(ProtocolConfig.ProtocolType));

        namePrefix = protocolType.ToString() + "的";

        //字段间关系，根据接口类型限制协议类型，比如接口类型是LAN时，如果协议类型是ModbusRtu，则不被支持
        if (!ProtocolTypeHelper.IsProtocolTypeValidForInterface(interfaceType, protocolType))
            throw new JsonException($"接口类型{interfaceType}下不支持协议类型{protocolType}");

        //equipments
        var equipments = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<List<EquipmentConfig>>(root, namePrefix, nameof(ProtocolConfig.Equipments), JsonValueKind.Array);

        //非必须存在字段，如果存在，校验其类型
        JsonValidateHelper.ValidateOptionalFields(
            root,
            namePrefix,
            (nameof(ProtocolConfig.CommunicationDelay), JsonValueKind.Number),
            (nameof(ProtocolConfig.ReceiveTimeOut), JsonValueKind.Number),
            (nameof(ProtocolConfig.ConnectTimeOut), JsonValueKind.Number),
            (nameof(ProtocolConfig.Account), JsonValueKind.String),
            (nameof(ProtocolConfig.Password), JsonValueKind.String),
            (nameof(ProtocolConfig.Remark), JsonValueKind.String),
            (nameof(ProtocolConfig.AdditionalOptions), JsonValueKind.String));

        // 跨对象校验：直接遍历JSON
        CrossObjectValidatePoints(root, protocolType, namePrefix);

        // 子类特有字段存在性校验和字段类型校验
        switch (interfaceType)
        {
            case InterfaceType.LAN:
                JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, namePrefix, nameof(NetworkProtocolConfig.IpAddress), JsonValueKind.String);
                JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<int>(root, namePrefix, nameof(NetworkProtocolConfig.ProtocolPort), JsonValueKind.Number);
                JsonValidateHelper.ValidateOptionalFields<string?>(root, namePrefix, nameof(NetworkProtocolConfig.Gateway), JsonValueKind.String);
                break;
            case InterfaceType.COM:
                JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, namePrefix, nameof(SerialPortConfig.SerialPortName), JsonValueKind.String);
                JsonValidateHelper.EnsurePropertyExistsAndEnumIsRight<BaudRateType>(root, namePrefix, nameof(SerialPortConfig.BaudRate));
                JsonValidateHelper.EnsurePropertyExistsAndEnumIsRight<DataBitsType>(root, namePrefix, nameof(SerialPortConfig.DataBits));
                JsonValidateHelper.EnsurePropertyExistsAndEnumIsRight<Parity>(root, namePrefix, nameof(SerialPortConfig.Parity));
                JsonValidateHelper.EnsurePropertyExistsAndEnumIsRight<StopBits>(root, namePrefix, nameof(SerialPortConfig.StopBits));
                break;
            case InterfaceType.API:
                JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, namePrefix, nameof(HttpApiInterfaceConfig.AccessApiString), JsonValueKind.String);
                JsonValidateHelper.EnsurePropertyExistsAndEnumIsRight<RequestMethod>(root, namePrefix, nameof(HttpApiInterfaceConfig.RequestMethod));
                JsonValidateHelper.ValidateOptionalFields<string?>(root, namePrefix, nameof(HttpApiInterfaceConfig.Gateway), JsonValueKind.String);
                break;
            case InterfaceType.DATABASE:
                JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, namePrefix, nameof(DatabaseInterfaceConfig.QuerySqlString), JsonValueKind.String);
                JsonValidateHelper.ValidateOptionalFields(
                    root,
                    namePrefix,
                    (nameof(DatabaseInterfaceConfig.Gateway), JsonValueKind.String),
                    (nameof(DatabaseInterfaceConfig.IpAddress), JsonValueKind.String),
                    (nameof(DatabaseInterfaceConfig.DatabaseName), JsonValueKind.String),
                    (nameof(DatabaseInterfaceConfig.DatabaseConnectString), JsonValueKind.String));
                JsonValidateHelper.ValidateOptionalFields<int?>(root, namePrefix, nameof(DatabaseInterfaceConfig.ProtocolPort), JsonValueKind.Number);
                break;
        }

        // 分派到具体协议类型
        ProtocolConfig dto = interfaceType switch
        {
            InterfaceType.LAN => root.Deserialize<NetworkProtocolConfig>(options)!,
            InterfaceType.COM => root.Deserialize<SerialPortConfig>(options)!,
            InterfaceType.API => root.Deserialize<HttpApiInterfaceConfig>(options)!,
            InterfaceType.DATABASE => root.Deserialize<DatabaseInterfaceConfig>(options)!,
            _ => throw new JsonException($"不支持的接口类型: {interfaceType}")
        };

        return dto;
    }

    private static void CrossObjectValidatePoints(JsonElement root, ProtocolType protocolType, string namePrefix)
    {
        var equipments = root.GetProperty(nameof(ProtocolConfig.Equipments));

        // 获取枚举字段上的特性
        var fieldInfo = typeof(ProtocolType).GetField(protocolType.ToString());
        var attr = fieldInfo?.GetCustomAttribute<ProtocolValidateParameterAttribute>();

        if (attr == null) return;

        foreach (var equipment in equipments.EnumerateArray())
        {
            if (!equipment.TryGetProperty(nameof(EquipmentConfig.Parameters), out var parameters) || parameters.ValueKind != JsonValueKind.Array)
                throw new JsonException("设备缺少参数列表");

            foreach (var parameter in parameters.EnumerateArray())
            {
                // 站号校验
                if (attr.RequireStationNo)
                    JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(parameter, namePrefix, nameof(ParameterConfig.StationNo), JsonValueKind.String);
                // 数据格式校验
                if (attr.RequireDataFormat)
                    JsonValidateHelper.EnsurePropertyExistsAndEnumIsRight<DataFormat>(parameter, namePrefix, nameof(ParameterConfig.DataFormat));
                // 数据类型校验
                if (attr.RequireDataType)
                    JsonValidateHelper.EnsurePropertyExistsAndEnumIsRight<DataType>(parameter, namePrefix, nameof(ParameterConfig.DataType));
                // 地址从0开始校验
                if (attr.RequireAddressStartWithZero)
                    JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<bool>(parameter, namePrefix, nameof(ParameterConfig.AddressStartWithZero), JsonValueKind.True);
                // 仪表类型校验
                if (attr.RequireInstrumentType)
                    JsonValidateHelper.EnsurePropertyExistsAndEnumIsRight<InstrumentType>(parameter, namePrefix, nameof(ParameterConfig.InstrumentType));
            }
        }
    }

    public override void Write(Utf8JsonWriter writer, ProtocolConfig value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
