using IndustrialDataProcessor.Domain.Workstation;
using IndustrialDataProcessor.Domain.Workstation.Configs;
using IndustrialDataProcessor.Domain.Workstation.Results;

namespace IndustrialDataProcessor.Domain.Communication.IConnection;

public interface IProtocolDriver : IDisposable
{
    Task<PointResult?> ReadAsync(IConnectionHandle handle, ProtocolConfig protocol, string equipmentId, ParameterConfig point, CancellationToken token);
    Task<ProtocolResult?> ReadAsync(IConnectionHandle handle, ProtocolConfig protocol, CancellationToken token);
    Task<bool> WriteAsync(IConnectionHandle handle, WriteTask writeTask, object value, CancellationToken token);
    string GetProtocolName();
}
