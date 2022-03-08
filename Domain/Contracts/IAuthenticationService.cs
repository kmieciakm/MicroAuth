using Domain.Models;

namespace Domain.Contracts;

public interface IAuthenticationService
{
    Task<User?> GetIdentityAsync(string email);
    Task<Token> SignInAsync(SignInRequest signIn);
    Task<User> SignUpAsync(SignUpRequest signUp);
}
