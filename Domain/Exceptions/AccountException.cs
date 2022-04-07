namespace Domain.Exceptions;

public class AccountException : ExplainedException
{
    public List<string> Details { get; set; } = new List<string>();

    public AccountException()
    {
    }

    public AccountException(string message, List<string> details, ExceptionCause cause = ExceptionCause.Unknown)
        : base(message, cause)
    {
        Details = details;
    }

    public AccountException(string message, Exception innerException = null, ExceptionCause cause = ExceptionCause.Unknown)
        : base(message, innerException, cause)
    {
    }
}