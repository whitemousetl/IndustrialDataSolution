using IndustrialDataProcessor.Contracts.WorkstationDto;
using IndustrialDataProcessor.Domain.Enums;
using IndustrialDataProcessor.Domain.Workstation.Configs;
using IndustrialDataProcessor.Domain.Workstation.Configs.ProtocolSub;

namespace IndustrialDataProcessor.Application.Mappers;

/// <summary>
/// 工作站配置映射器
/// </summary>
public static class WorkstationConfigMapper
{
    /// <summary>
    /// 将 DTO 转换为领域对象
    /// </summary>
    public static WorkstationConfig ToDomain(this WorkstationConfigDto dto)
    {
        var config = new WorkstationConfig(dto.Id ?? string.Empty, dto.Name ?? string.Empty, dto.IpAddress ?? string.Empty);
        
        foreach (var protocolDto in dto.Protocols)
        {
            config.AddProtocol(protocolDto.ToDomain());
        }
        
        return config;
    }

    /// <summary>
    /// 将协议 DTO 转换为领域对象
    /// </summary>
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
        protocol.Account = dto.Account ?? string.Empty;
        protocol.Password = dto.Password ?? string.Empty;
        protocol.Remark = dto.Remark ?? string.Empty;
        protocol.AdditionalOptions = dto.AdditionalOptions ?? string.Empty;
        protocol.Equipments = [.. dto.Equipments.Select(e => e.ToDomain())];

        return protocol;
    }

    /// <summary>
    /// 将设备 DTO 转换为领域对象
    /// </summary>
    public static EquipmentConfig ToDomain(this EquipmentConfigDto dto)
    {
        var equipment = new EquipmentConfig(dto.Id, dto.IsCollect, dto.Name, dto.EquipmentType);
        
        if (dto.Parameters != null)
        {
            foreach (var paramDto in dto.Parameters)
            {
                equipment.AddParameter(paramDto.ToDomain());
            }
        }
        
        return equipment;
    }

    /// <summary>
    /// 将参数 DTO 转换为领域对象
    /// </summary>
    public static ParameterConfig ToDomain(this ParameterConfigDto dto)
    {
        return new ParameterConfig(dto.Label, dto.Address)
        {
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
