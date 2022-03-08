namespace Domain.Exceptions;

public class AuthenticationException : Exception
{
    public ExceptionCause Cause { get; }

    public AuthenticationException()
    {
    }

    public AuthenticationException(string message, ExceptionCause cause = ExceptionCause.Unknown)
        : base(message)
    {
        Cause = cause;
    }

    public AuthenticationException(string message, Exception innerException, ExceptionCause cause = ExceptionCause.Unknown)
        : base(message, innerException)
    {
        Cause = cause;
    }
}
