namespace Domain.Exceptions;

public class RegistrationException : Exception
{
    public List<string> Details { get; set; } = new List<string>();
    public ExceptionCause Cause { get; }

    public RegistrationException()
    {
    }

    public RegistrationException(string message, ExceptionCause cause = ExceptionCause.Unknown)
        : base(message)
    {
        Cause = cause;
    }

    public RegistrationException(string message, Exception innerException, ExceptionCause cause = ExceptionCause.Unknown)
        : base(message, innerException)
    {
        Cause = cause;
    }

    public RegistrationException(string message, List<string> details, ExceptionCause cause = ExceptionCause.Unknown)
        : base(message)
    {
        Details = details;
        Cause = cause;
    }

    public RegistrationException(string message, List<string> details, Exception innerException, ExceptionCause cause = ExceptionCause.Unknown)
        : base(message, innerException)
    {
        Details = details;
        Cause = cause;
    }
}