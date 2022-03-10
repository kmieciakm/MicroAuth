namespace Domain.Exceptions;

public class AuthorizationException : ExplainedException
{
    public AuthorizationException()
    {
    }

    public AuthorizationException(string message, ExceptionCause cause = ExceptionCause.Unknown)
        : base(message, cause)
    {
    }

    public AuthorizationException(string message, Exception innerException, ExceptionCause cause = ExceptionCause.Unknown)
        : base(message, innerException, cause)
    {
    }
}
