using Domain.Models;

namespace Domain.Contracts;

public interface IAccountService
{
    Task ResetPassword(Guid userId, ResetToken token, string newPassword);
    Task DeleteAccountAsync(Guid guid);
}
