namespace Domain.Exceptions;

public class AuthenticationException : ExplainedException
{
    public AuthenticationException()
    {
    }

    public AuthenticationException(string message, ExceptionCause cause = ExceptionCause.Unknown)
        : base(message, cause)
    {
    }

    public AuthenticationException(string message, Exception innerException, ExceptionCause cause = ExceptionCause.Unknown)
        : base(message, innerException, cause)
    {
    }
}
