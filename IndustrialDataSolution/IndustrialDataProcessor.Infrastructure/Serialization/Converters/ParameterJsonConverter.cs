using HslCommunication.Core;
using IndustrialDataProcessor.Domain.Enums;
using IndustrialDataProcessor.Domain.Workstation.Configs;
using IndustrialDataProcessor.Infrastructure.Helpers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IndustrialDataProcessor.Infrastructure.Serialization.Converters;

public class ParameterJsonConverter : JsonConverter<ParameterConfig>
{
    public override ParameterConfig? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var namePrefix = "Parameter的";
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        //存在性校验
        var label = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, namePrefix, nameof(ParameterConfig.Label), JsonValueKind.String);
        var address = JsonValidateHelper.EnsurePropertyExistsAndTypeIsRight<string>(root, namePrefix, nameof(ParameterConfig.Address), JsonValueKind.String);
        var isMonitor = JsonValidateHelper.ValidateOptionalFields<bool>(root, namePrefix, nameof(ParameterConfig.IsMonitor), JsonValueKind.True);

        //Name如果存在，必须为字符串
        var stationNo = JsonValidateHelper.ValidateOptionalFields<string>(root, namePrefix, nameof(ParameterConfig.StationNo), JsonValueKind.String) ?? string.Empty;
        var defaultValue = JsonValidateHelper.ValidateOptionalFields<string>(root, namePrefix, nameof(ParameterConfig.DefaultValue), JsonValueKind.String) ?? string.Empty;
        var positiveExpression = JsonValidateHelper.ValidateOptionalFields<string>(root, namePrefix, nameof(ParameterConfig.PositiveExpression), JsonValueKind.String) ?? string.Empty;
        var minValue = JsonValidateHelper.ValidateOptionalFields<string>(root, namePrefix, nameof(ParameterConfig.MinValue), JsonValueKind.String) ?? string.Empty;
        var maxValue = JsonValidateHelper.ValidateOptionalFields<string>(root, namePrefix, nameof(ParameterConfig.MaxValue), JsonValueKind.String) ?? string.Empty;
        var value = JsonValidateHelper.ValidateOptionalFields<string>(root, namePrefix, nameof(ParameterConfig.Value), JsonValueKind.String) ?? string.Empty;
        var length = JsonValidateHelper.ValidateOptionalFields<ushort>(root, namePrefix, nameof(ParameterConfig.Length), JsonValueKind.Number);
        var cycle = JsonValidateHelper.ValidateOptionalFields<int>(root, namePrefix, nameof(ParameterConfig.Cycle), JsonValueKind.Number);
        var addressStartWithZero = JsonValidateHelper.ValidateOptionalFields<bool?>(root, namePrefix, nameof(ParameterConfig.AddressStartWithZero), JsonValueKind.True);

        var dataType = JsonValidateHelper.GetOptionalEnum<DataType>(root, namePrefix, nameof(ParameterConfig.DataType));
        var dataFormat = JsonValidateHelper.GetOptionalEnum<DomainDataFormat>(root, namePrefix, nameof(ParameterConfig.DataFormat));
        var instrumentType = JsonValidateHelper.GetOptionalEnum<InstrumentType>(root, namePrefix, nameof(ParameterConfig.InstrumentType));

        return new ParameterConfig
        {
            Label = label,
            IsMonitor = isMonitor,
            StationNo = stationNo,
            DataType = dataType,
            Address = address,
            Length = length,
            DefaultValue = defaultValue,
            Cycle = cycle,
            PositiveExpression = positiveExpression,
            MinValue = minValue,
            MaxValue = maxValue,
            DataFormat = dataFormat,
            AddressStartWithZero = addressStartWithZero,
            InstrumentType = instrumentType,
            Value = value,
        };
    }

    public override void Write(Utf8JsonWriter writer, ParameterConfig value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
