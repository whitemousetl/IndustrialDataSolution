using IndustrialDataProcessor.Application.Dtos.WorkstationDto;
using IndustrialDataProcessor.Domain.Enums;
using IndustrialDataProcessor.Domain.Workstation.Configs;
using IndustrialDataProcessor.Domain.Workstation.Configs.ProtocolSub;

namespace IndustrialDataProcessor.Application.Mappers;

public static class WorkstationConfigMapper
{
    public static WorkstationConfig ToDomain(this WorkstationConfigDto dto)
    {
        return new WorkstationConfig
        {
            Id = dto.Id,
            Name = dto.Name ?? string.Empty,
            IpAddress = dto.IpAddress,
            Protocols = dto.Protocols.Select(p => p.ToDomain()).ToList()
        };
    }

    public static ProtocolConfig ToDomain(this ProtocolConfigDto dto)
    {
        ProtocolConfig protocol = dto.InterfaceType switch
        {
            InterfaceType.COM => new SerialPortConfig
            {
                SerialPortName = dto.SerialPortName!,
                BaudRate = dto.BaudRate!.Value,
                DataBits = dto.DataBits!.Value,
                Parity = dto.Parity!.Value,
                StopBits = dto.StopBits!.Value
            },
            InterfaceType.LAN => new NetworkProtocolConfig
            {
                IpAddress = dto.IpAddress!,
                ProtocolPort = dto.ProtocolPort!.Value,
                Gateway = dto.Gateway ?? string.Empty
            },
            InterfaceType.DATABASE => new DatabaseInterfaceConfig
            {
                IpAddress = dto.IpAddress ?? string.Empty,
                ProtocolPort = dto.ProtocolPort ?? 0,
                DatabaseName = dto.DatabaseName ?? string.Empty,
                DatabaseConnectString = dto.DatabaseConnectString ?? string.Empty,
                QuerySqlString = dto.QuerySqlString!,
                Gateway = dto.Gateway ?? string.Empty
            },
            InterfaceType.API => new HttpApiInterfaceConfig
            {
                RequestMethod = dto.RequestMethod!.Value,
                AccessApiString = dto.AccessApiString!,
                Gateway = dto.Gateway ?? string.Empty
            },
            _ => throw new ArgumentException($"不支持的接口类型: {dto.InterfaceType}")
        };

        // 填充通用属性
        protocol.Id = dto.Id;
        protocol.ProtocolType = dto.ProtocolType;
        protocol.CommunicationDelay = dto.CommunicationDelay;
        protocol.ReceiveTimeOut = dto.ReceiveTimeOut;
        protocol.ConnectTimeOut = dto.ConnectTimeOut;
        protocol.Account = dto.Account;
        protocol.Password = dto.Password;
        protocol.Remark = dto.Remark;
        protocol.AdditionalOptions = dto.AdditionalOptions;
        protocol.Equipments = dto.Equipments.Select(e => e.ToDomain()).ToList();

        return protocol;
    }

    public static EquipmentConfig ToDomain(this EquipmentConfigDto dto)
    {
        return new EquipmentConfig
        {
            Id = dto.Id,
            Name = dto.Name,
            IsCollect = dto.IsCollect,
            EquipmentType = dto.EquipmentType,
            Parameters = dto.Parameters?.Select(p => p.ToDomain()).ToList()
        };
    }

    public static ParameterConfig ToDomain(this ParameterConfigDto dto)
    {
        return new ParameterConfig
        {
            Label = dto.Label,
            Address = dto.Address,
            IsMonitor = dto.IsMonitor,
            StationNo = dto.StationNo,
            DataType = dto.DataType,
            Length = dto.Length ?? default,
            DefaultValue = dto.DefaultValue,
            Cycle = dto.Cycle ?? default,
            PositiveExpression = dto.PositiveExpression,
            MinValue = dto.MinValue,
            MaxValue = dto.MaxValue,
            DataFormat = dto.DataFormat,
            AddressStartWithZero = dto.AddressStartWithZero,
            InstrumentType = dto.InstrumentType,
            Value = dto.Value
        };
    }
}
