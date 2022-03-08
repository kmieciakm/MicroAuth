using Domain.Models;

namespace Domain.Infrastructure;

public interface IUserRegistry
{
    Task<User?> GetAsync(Guid id);
    Task<User?> GetAsync(string email);
    Task<bool> CreateAsync(User user, string password);
    Task<bool> AuthenticateAsync(string email, string password);
    Task DeleteAsync(Guid id);
    Task<ValidationResult> ValidatePasswordAsync(string password);
}
