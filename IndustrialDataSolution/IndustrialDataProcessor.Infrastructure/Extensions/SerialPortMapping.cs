using IndustrialDataProcessor.Domain.Enums;
using System.IO.Ports;

namespace IndustrialDataProcessor.Infrastructure.Extensions;

public static class SerialPortMapping
{
    public static Parity ToSystemParity(this DomainParity parity)
    {
        return parity switch
        {
            DomainParity.None => Parity.None,
            DomainParity.Odd => Parity.Odd,
            DomainParity.Even => Parity.Even,
            DomainParity.Mark => Parity.Mark,
            DomainParity.Space => Parity.Space,
            _ => Parity.None
        };
    }

    public static StopBits ToSystemStopBits(this DomainStopBits stopBits)
    {
        return stopBits switch
        {
            DomainStopBits.None => StopBits.None,
            DomainStopBits.One => StopBits.One,
            DomainStopBits.Two => StopBits.Two,
            DomainStopBits.OnePointFive => StopBits.OnePointFive,
            _ => StopBits.One
        };
    }
}
