namespace Visprech.Core.Exceptions
{
    public class ProcessingException : Exception
    {
        public ProcessingException(string? message, Exception innerException) : base(message, innerException)
        {
        }

        public ProcessingException(string? message) : base(message)
        {
        }
    }

    public class WrongConfigurationException : Exception
    {
        public WrongConfigurationException(string? message) : base(message)
        {
        }
    }
}
