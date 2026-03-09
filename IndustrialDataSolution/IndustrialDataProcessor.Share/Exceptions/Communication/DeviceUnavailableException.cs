namespace IndustrialDataProcessor.Share.Exceptions.Communication;

public class DeviceUnavailableException(string message, Exception? inner = null) : Exception(message, inner)
{
}
