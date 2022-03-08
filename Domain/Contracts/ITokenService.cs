using Domain.Models;

namespace Domain.Contracts;

public interface ITokenService
{
    Token GenerateSecurityToken(string email);
}
