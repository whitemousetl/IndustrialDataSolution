namespace IndustrialDataProcessor.Share.Exceptions.Communication;

public class SerialPortBusyException(string message, Exception? inner = null) : Exception(message, inner)
{
}
