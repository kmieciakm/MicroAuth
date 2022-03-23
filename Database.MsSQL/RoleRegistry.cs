using Domain.Infrastructure;
using Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Database.MsSQL;

public class RoleRegistry : IRoleRegistry
{
    private RoleManager<DbRole> _RoleManager { get; }
    private UserManager<DbUser> _UserManager { get; }

    public RoleRegistry(RoleManager<DbRole> roleManager, UserManager<DbUser> userManager)
    {
        _RoleManager = roleManager;
        _UserManager = userManager;
    }

    public async Task<IEnumerable<Role>> GetRolesAsync()
    {
        return await _RoleManager
            .Roles
            .Select(r => new Role(r.Name))
            .ToListAsync();
    }

    public async Task<IEnumerable<Role>> GetUserAssignedRolesAsync(Guid id)
    {
        DbUser? dbUser = await GetDbUserByIdAsync(id);
        if (dbUser is not null)
        {
            var roles = await _UserManager.GetRolesAsync(dbUser);
            return roles.Select(r => new Role(r));
        }
        return null;
    }

    public async Task<bool> CheckExistsAsync(Role role)
    {
        return await _RoleManager.RoleExistsAsync(role.Name);
    }

    public async Task<bool> CreateAsync(Role role)
    {
        DbRole dbRole = new() { Name = role.Name };
        var result = await _RoleManager.CreateAsync(dbRole);
        return result.Succeeded;
    }

    public async Task DeleteAsync(Role role)
    {
        if (await CheckExistsAsync(role))
        {
            DbRole dbRole = new() { Name = role.Name };
            await _RoleManager.DeleteAsync(dbRole);
        }
    }

    private async Task<DbUser?> GetDbUserByIdAsync(Guid id)
    {
        return await _UserManager
            .Users
            .FirstOrDefaultAsync(user => user.Id == id.ToString());
    }
}
