namespace IndustrialDataProcessor.Share.Exceptions.Communication;

public class CommunicationException(string message, Exception? inner = null) : Exception(message, inner)
{
}
