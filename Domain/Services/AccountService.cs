using Domain.Contracts;
using Domain.Exceptions;
using Domain.Infrastructure;
using Domain.Models;

namespace Domain.Services;

public class AccountService : IAccountService
{
    private IUserRegistry _UserRepository { get; }

    public AccountService(IUserRegistry userRepository)
    {
        _UserRepository = userRepository;
    }

    public async Task ResetPassword(Guid userId, ResetToken token, string newPassword)
    {
        try
        {
            var userExists = await _UserRepository.CheckExistsAsync(userId);
            if (!userExists)
            {
                throw new AccountException($"Cannot reset password. Cannot find user with id ${userId}.");
            }

            var passwordValidation = await _UserRepository.ValidatePasswordAsync(newPassword);
            if (!passwordValidation.IsValid)
            {
                throw new AccountException("Cannot reset password. New password is weak.", passwordValidation.Errors);
            }

            var succes = await _UserRepository.ResetPassword(userId, token, newPassword);
            if (!succes)
            {
                throw new AccountException("Cannot reset password. Reset token is not valid.");
            }
        }
        catch (Exception ex) when (ex.GetType() != typeof(AccountException))
        {
            throw new AccountException("Cannot reset password due to error.", ex);
        }
    }

    public async Task DeleteAccountAsync(Guid guid)
    {
        await _UserRepository.DeleteAsync(guid);
    }
}