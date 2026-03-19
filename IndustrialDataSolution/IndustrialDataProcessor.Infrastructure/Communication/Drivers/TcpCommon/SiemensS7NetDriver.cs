using HslCommunication.Profinet.Siemens;
using IndustrialDataProcessor.Domain.Communication.IConnection;
using IndustrialDataProcessor.Domain.Workstation.Configs;
using IndustrialDataProcessor.Domain.Workstation.Results;
using IndustrialDataProcessor.Infrastructure.Communication.Extensions;

namespace IndustrialDataProcessor.Infrastructure.Communication.Drivers.TcpCommon;

public class SiemensS7NetDriver : BaseProtocolDriver<SiemensS7Net>
{
    protected override async Task<PointResult> ReadPointCoreAsync(IConnectionHandle handle, ParameterConfig point, CancellationToken token)
    {
        var conn = handle.GetRawConnection<SiemensS7Net>();
        return await conn.ReadPointAsync(point);
    }

    protected override async Task<bool> WritePointCoreAsync(IConnectionHandle handle, ParameterConfig point, object value, CancellationToken token)
    {
        var conn = handle.GetRawConnection<SiemensS7Net>();
        return await conn.WritePointAsync(point, value);
    }
}

// 增加以下基于同一实现的薄包装类，依赖注入扫描时会把这些类名(去除Driver)和ProtocolType匹配上
public class SiemensS1200Driver : SiemensS7NetDriver { }
public class SiemensS1500Driver : SiemensS7NetDriver { }
public class SiemensS200Driver : SiemensS7NetDriver { }
public class SiemensS300Driver : SiemensS7NetDriver { }
public class SiemensS400Driver : SiemensS7NetDriver { }
public class SiemensS200SmartDriver : SiemensS7NetDriver { }

