using Domain.Models;

namespace Domain.Infrastructure;

public interface IMailSender
{
    Task SendEmailAsync(Email email);
}