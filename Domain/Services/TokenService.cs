using Domain.Contracts;
using Domain.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Domain.Services;

public class TokenService : ITokenService
{
    private AuthenticationSettings _AuthenticationSettings { get; }

    public TokenService(IOptions<AuthenticationSettings> authenticationSettings)
    {
        _AuthenticationSettings = authenticationSettings.Value;
    }

    public Token GenerateSecurityToken(Claims claims)
    {
        JwtSecurityTokenHandler tokenHandler = new();
        var key = Encoding.UTF8.GetBytes(_AuthenticationSettings.Secret);

        SecurityTokenDescriptor tokenDescriptor = new()
        {
            Subject = GetClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(_AuthenticationSettings.ExpirationHours),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature),
            IssuedAt = DateTime.UtcNow,
            Issuer = _AuthenticationSettings.Issuer,
            Audience = _AuthenticationSettings.Audience
        };

        var securityToken = tokenHandler.CreateToken(tokenDescriptor);

        return new Token()
        {
            JWT = tokenHandler.WriteToken(securityToken)
        };
    }

    private static ClaimsIdentity GetClaimsIdentity(Claims claims)
    {
        List<Claim> claimsList = new()
        {
            new Claim(ClaimTypes.Email, claims.Email)
        };
        foreach (var role in claims.Roles)
        {
            claimsList.Add(
                new Claim(ClaimTypes.Role, role.Name));
        }
        return new ClaimsIdentity(claimsList);
    }

    public async Task<bool> ValidateTokenAsync(Token token)
    {
        byte[] secretKey = Encoding.ASCII.GetBytes(_AuthenticationSettings.Secret);
        JwtSecurityTokenHandler tokenHandler = new();
        TokenValidationParameters validationParams = new()
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = _AuthenticationSettings.Issuer,
            ValidAudience = _AuthenticationSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(secretKey)
        };
        TokenValidationResult result = await tokenHandler.ValidateTokenAsync(token.JWT, validationParams);
        return result.IsValid;
    }
}
