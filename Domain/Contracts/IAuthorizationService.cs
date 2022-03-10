using Domain.Models;

namespace Domain.Contracts;

public interface IAuthorizationService
{
    Task AssignDefaultRoles(Guid userId);
    Task AddPredefinedRolesAsync();
    Task<bool> DefineRoleAsync(Role role);
    Task<bool> CanManageRoles(Guid userId);
    Task AssignRoleAsync(Role role, Guid userId);
}
