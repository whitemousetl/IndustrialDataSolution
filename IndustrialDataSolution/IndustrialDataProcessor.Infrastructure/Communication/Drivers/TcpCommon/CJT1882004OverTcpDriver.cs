using HslCommunication.Instrument.CJT;
using HslCommunication.Instrument.DLT;
using IndustrialDataProcessor.Domain.Communication.IConnection;
using IndustrialDataProcessor.Domain.Enums;
using IndustrialDataProcessor.Domain.Workstation.Configs;
using IndustrialDataProcessor.Domain.Workstation.Results;
using IndustrialDataProcessor.Infrastructure.Communication.Extensions;

namespace IndustrialDataProcessor.Infrastructure.Communication.Drivers.TcpCommon;



public class CJT1882004OverTcpDriver : BaseProtocolDriver<CJT188OverTcp>
{
    protected override async Task<PointResult> ReadPointCoreAsync(IConnectionHandle handle, ParameterConfig point, CancellationToken token)
    {
        var conn = handle.GetRawConnection<CJT188OverTcp>();
        conn.Station = point.StationNo;
        if (point.InstrumentType.HasValue)
            conn.InstrumentType = (byte)point.InstrumentType.Value;
        return await conn.ReadPointAsync(point);
    }

    protected override async Task<bool> WritePointCoreAsync(IConnectionHandle handle, ParameterConfig point, object value, CancellationToken token)
    {
        var conn = handle.GetRawConnection<CJT188OverTcp>();
        conn.Station = point.StationNo;
        if (point.InstrumentType.HasValue)
            conn.InstrumentType = (byte)point.InstrumentType.Value;
        return await conn.WritePointAsync(point, value);
    }
}
