using Domain.Models;

namespace Domain.Contracts;

public interface ITokenService
{
    Token GenerateSecurityToken(Claims claims);
    Task<ResetToken> GeneratePasswordResetTokenAsync(string email);
    Task<bool> ValidateSecurityTokenAsync(Token token);
}