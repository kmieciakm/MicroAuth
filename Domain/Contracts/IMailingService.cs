using Domain.Models;

namespace Domain.Contracts;

public interface IMailingService
{
    Task SendResetPasswordEmailAsync(string address, ResetToken resetToken);
}
