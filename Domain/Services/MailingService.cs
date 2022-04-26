using Domain.Contracts;
using Domain.Infrastructure;
using Domain.Models;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Domain.Services;

public class ResetPasswordEmailSettings
{
    public string ApplicationName { get; set; }
    public string ResetPasswordUrl { get; set; }
    public string ResetPasswordTemplate { get; set; }
}

public class MailingService : IMailingService
{
    private static char DS = Path.DirectorySeparatorChar;
    private static string TEMPLATES_DIR = "Templates";
    private static string EMAIL_TEMPLATES_DIR = $"{Directory.GetCurrentDirectory()}{DS}{TEMPLATES_DIR}{DS}";

    private ResetPasswordEmailSettings _ResetPasswordEmailSettings { get; set; }
    private IMailSender _MailSender { get; set; }

    public MailingService(
        IOptions<ResetPasswordEmailSettings> resetPasswordEmailOptions,
        IMailSender mailSender)
    {
        _ResetPasswordEmailSettings = resetPasswordEmailOptions.Value;
        _MailSender = mailSender;
    }

    public async Task SendResetPasswordEmailAsync(string address, ResetToken resetToken)
    {
        var resetUrl = string.Format(_ResetPasswordEmailSettings.ResetPasswordUrl, resetToken.Value);
        var emailPath = $"{EMAIL_TEMPLATES_DIR}{_ResetPasswordEmailSettings.ResetPasswordTemplate}";
        var emailTemplate = File.ReadAllText(emailPath);
        var emailMessage = string.Format(emailTemplate, _ResetPasswordEmailSettings.ApplicationName, resetUrl);
        var emailSubject = $"{_ResetPasswordEmailSettings.ApplicationName} - Password Reset";

        Email email = new()
        {
            To = address,
            Subject = emailSubject,
            Message = emailMessage
        };
        await _MailSender.SendEmailAsync(email);
    }
}
