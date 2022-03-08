namespace Domain.Contracts;

public interface IAccountService
{
    Task DeleteAccountAsync(Guid guid);
}
