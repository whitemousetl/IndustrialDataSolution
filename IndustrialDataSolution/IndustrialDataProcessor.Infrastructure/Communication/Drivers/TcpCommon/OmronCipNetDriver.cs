using HslCommunication.Core;
using HslCommunication.Profinet.Omron;
using IndustrialDataProcessor.Domain.Communication.IConnection;
using IndustrialDataProcessor.Domain.Workstation.Configs;
using IndustrialDataProcessor.Domain.Workstation.Results;
using IndustrialDataProcessor.Infrastructure.Communication.Extensions;

namespace IndustrialDataProcessor.Infrastructure.Communication.Drivers.TcpCommon;

public class OmronCipNetDriver : BaseProtocolDriver<OmronCipNet>
{
    protected override async Task<PointResult> ReadPointCoreAsync(IConnectionHandle handle, ParameterConfig point, CancellationToken token)
    {
        // 1. 从句柄中获取强类型的 HSL 连接对象
        var conn = handle.GetRawConnection<OmronCipNet>();

        // 2. 调用 HSL 通用扩展方法读取数据
        return await conn.ReadPointAsync(point);
    }

    protected override async Task<bool> WritePointCoreAsync(IConnectionHandle handle, ParameterConfig point, object value, CancellationToken token)
    {
        // 1. 从句柄中获取强类型的 HSL 连接对象
        var conn = handle.GetRawConnection<OmronCipNet>();

        // 2. 调用 HSL 通用扩展方法写入数据 (假设你在 Extensions 中也实现了 WritePointAsync)
        return await conn.WritePointAsync(point, value);
    }
}