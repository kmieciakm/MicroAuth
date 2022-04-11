using Domain.Models;

namespace Domain.Infrastructure;

public interface IMailingService
{
    Task SendResetPasswordEmailAsync(string email, ResetToken resetToken);
}
