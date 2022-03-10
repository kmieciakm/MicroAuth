using Domain.Models;

namespace Domain.Infrastructure;

public interface IUserRegistry
{
    Task<User?> GetAsync(Guid id);
    Task<User?> GetAsync(string email);
    Task<bool> CheckExistsAsync(Guid userId);
    Task<bool> CreateAsync(User user, string password);
    Task<bool> AuthenticateAsync(string email, string password);
    Task<ValidationResult> ValidatePasswordAsync(string password);
    Task AddToRoleAsync(Guid userId, Role role);
    Task DeleteAsync(Guid id);
}
