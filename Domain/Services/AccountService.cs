using Domain.Contracts;
using Domain.Exceptions;
using Domain.Infrastructure;
using Domain.Models;

namespace Domain.Services;

public class AccountService : IAccountService
{
    private IUserRegistry _UserRepository { get; }
    private IMailingService _MailingService { get; }

    public AccountService(IUserRegistry userRepository, IMailingService mailingService)
    {
        _UserRepository = userRepository;
        _MailingService = mailingService;
    }

    public async Task RequestPasswordReset(Guid userId)
    {
        var user = await _UserRepository.GetAsync(userId);
        if (user is null)
        {
            throw new AccountException($"Cannot reset password. Cannot find user with id ${userId}.");
        }

        var token = await _UserRepository.GeneratePasswordResetTokenAsync(userId);
        if (token is null)
        {
            throw new AccountException($"Cannot reset password. Token generation failed.");
        }
        await _MailingService.SendResetPasswordEmailAsync(user.Email, token.Value);
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