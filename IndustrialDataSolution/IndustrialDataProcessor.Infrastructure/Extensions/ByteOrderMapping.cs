using HslCommunication.Core;
using IndustrialDataProcessor.Domain.Enums;

namespace IndustrialDataProcessor.Infrastructure.Extensions;

public static class ByteOrderMapping
{
    public static DataFormat ToHslDataFormat(this DomainDataFormat format)
    {
        return format switch
        {
            DomainDataFormat.CDAB => DataFormat.CDAB,
            DomainDataFormat.ABCD => DataFormat.ABCD,
            DomainDataFormat.BADC => DataFormat.BADC,
            DomainDataFormat.DCBA => DataFormat.DCBA,
            _ => DataFormat.CDAB
        };
    }
}
