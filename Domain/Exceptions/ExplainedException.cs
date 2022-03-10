namespace Domain.Exceptions;

public abstract class ExplainedException : Exception
{
    public ExceptionCause Cause { get; }

    public ExplainedException()
    {
    }

    public ExplainedException(string message, ExceptionCause cause = ExceptionCause.Unknown)
        : base(message)
    {
        Cause = cause;
    }

    public ExplainedException(string message, Exception innerException, ExceptionCause cause = ExceptionCause.Unknown)
        : base(message, innerException)
    {
        Cause = cause;
    }
}
