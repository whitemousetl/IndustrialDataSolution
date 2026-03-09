using HslCommunication.Core;
using HslCommunication.ModBus;
using IndustrialDataProcessor.Domain.Communication.IConnection;
using IndustrialDataProcessor.Domain.Workstation.Configs;
using IndustrialDataProcessor.Domain.Workstation.Results;
using IndustrialDataProcessor.Infrastructure.Communication.Extensions;
using IndustrialDataProcessor.Infrastructure.Extensions;

namespace IndustrialDataProcessor.Infrastructure.Communication.Drivers.TcpCommon;

public class ModbusTcpNetDriver : BaseProtocolDriver<ModbusTcpNet>
{
    protected override async Task<PointResult> ReadPointCoreAsync(IConnectionHandle handle, ParameterConfig point, CancellationToken token)
    {
        // 1. 从句柄中获取强类型的 HSL 连接对象
        var conn = handle.GetRawConnection<ModbusTcpNet>();

        // 2. 设置当前点位所需的站号 (因为基类已经加锁，修改 Station 是线程安全的)
        conn.Station = byte.TryParse(point.StationNo, out var station) ? station : (byte)1;
        conn.DataFormat = point.DataFormat?.ToHslDataFormat() ?? DataFormat.CDAB;
        conn.AddressStartWithZero = point.AddressStartWithZero ?? true;

        // 3. 调用 HSL 通用扩展方法读取数据
        return await conn.ReadPointAsync(point);
    }

    protected override async Task<bool> WritePointCoreAsync(IConnectionHandle handle, ParameterConfig point, object value, CancellationToken token)
    {
        // 1. 从句柄中获取强类型的 HSL 连接对象
        var conn = handle.GetRawConnection<ModbusTcpNet>();

        // 2. 设置当前点位写入时所需的格式与站号
        conn.Station = byte.TryParse(point.StationNo, out var station) ? station : (byte)1;
        conn.DataFormat = point.DataFormat?.ToHslDataFormat() ?? DataFormat.CDAB;
        conn.AddressStartWithZero = point.AddressStartWithZero ?? true;

        // 3. 调用 HSL 通用扩展方法写入数据 (假设你在 Extensions 中也实现了 WritePointAsync)
        return await conn.WritePointAsync(point, value);
    }
}
