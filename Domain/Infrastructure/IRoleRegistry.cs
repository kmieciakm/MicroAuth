using Domain.Models;

namespace Domain.Infrastructure;

public interface IRoleRegistry
{
    Task<IEnumerable<Role>> GetRolesAsync();
    Task<IEnumerable<Role>> GetUserAssignedRolesAsync(Guid id);
    Task<bool> CheckExistsAsync(Role role);
    Task<bool> CreateAsync(Role role);
    Task DeleteAsync(Role role);
}
