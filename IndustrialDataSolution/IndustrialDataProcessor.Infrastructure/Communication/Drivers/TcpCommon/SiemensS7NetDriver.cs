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

