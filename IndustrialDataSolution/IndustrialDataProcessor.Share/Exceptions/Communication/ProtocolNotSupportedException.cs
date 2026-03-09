namespace IndustrialDataProcessor.Share.Exceptions.Communication;

public class ProtocolNotSupportedException(string message, Exception? inner = null) : Exception(message, inner)
{
}
