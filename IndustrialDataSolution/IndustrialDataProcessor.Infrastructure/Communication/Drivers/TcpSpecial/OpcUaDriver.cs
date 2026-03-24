using IndustrialDataProcessor.Domain.Communication.IConnection;
using IndustrialDataProcessor.Domain.Workstation.Configs;
using IndustrialDataProcessor.Domain.Workstation.Results;
using Opc.Ua.Client;

namespace IndustrialDataProcessor.Infrastructure.Communication.Drivers.TcpSpecial;

public class OpcUaDriver : BaseProtocolDriver<Session>
{
    protected override Task<PointResult> ReadPointCoreAsync(IConnectionHandle handle, ParameterConfig point, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    protected override Task<bool> WritePointCoreAsync(IConnectionHandle handle, ParameterConfig point, object value, CancellationToken token)
    {
        throw new NotImplementedException();
    }
}
