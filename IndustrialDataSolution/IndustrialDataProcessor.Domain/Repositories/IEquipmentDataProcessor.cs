using IndustrialDataProcessor.Domain.Workstation.Configs;
using IndustrialDataProcessor.Domain.Workstation.Results;
using System.Collections.Concurrent;

namespace IndustrialDataProcessor.Domain.Repositories;

public interface IEquipmentDataProcessor
{
    ConcurrentDictionary<string, string> Process(ProtocolResult protocolResult, ProtocolConfig protocol, CancellationToken token);
}
