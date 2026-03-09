namespace IndustrialDataProcessor.Share.Exceptions.Communication;

public class TransientCommunicationException(string message, Exception? inner = null) : Exception(message, inner)
{
}
