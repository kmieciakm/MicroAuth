using Domain.Models;

namespace Domain.Contracts;

public interface ITokenService
{
    Token GenerateSecurityToken(Claims claims);
    Task<bool> ValidateTokenAsync(Token token);
}