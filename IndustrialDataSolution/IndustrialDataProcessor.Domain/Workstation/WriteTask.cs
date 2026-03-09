using IndustrialDataProcessor.Domain.Workstation.Configs;

namespace IndustrialDataProcessor.Domain.Workstation;

public class WriteTask
{
    public string UUID { get; set; } = Guid.NewGuid().ToString("N");
    public ParameterConfig WritePoint { get; set; } = new();
    //public ProtocolConfig Protocol { get; set; } = null!;
}
