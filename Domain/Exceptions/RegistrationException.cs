namespace Domain.Exceptions;

public class RegistrationException : ExplainedException
{
    public List<string> Details { get; set; } = new List<string>();

    public RegistrationException()
    {
    }

    public RegistrationException(string message, ExceptionCause cause = ExceptionCause.Unknown)
        : base(message, cause)
    {
    }

    public RegistrationException(string message, Exception innerException, ExceptionCause cause = ExceptionCause.Unknown)
        : base(message, innerException, cause)
    {
    }

    public RegistrationException(string message, List<string> details, ExceptionCause cause = ExceptionCause.Unknown)
        : base(message, cause)
    {
        Details = details;
    }

    public RegistrationException(string message, List<string> details, Exception innerException, ExceptionCause cause = ExceptionCause.Unknown)
        : base(message, innerException, cause)
    {
        Details = details;
    }
}