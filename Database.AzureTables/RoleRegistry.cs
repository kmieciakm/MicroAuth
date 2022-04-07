using Domain.Infrastructure;
using Domain.Models;

namespace Database.AzureTables;

public class RoleRegistry : IRoleRegistry
{
    private IRoleTable _RoleTable { get; }
    private IUsersRolesTable _UsersRolesTable { get; }

    public RoleRegistry(IRoleTable roleTable, IUsersRolesTable usersRolesTable)
    {
        _RoleTable = roleTable;
        _UsersRolesTable = usersRolesTable;
    }

    public async Task<IEnumerable<Role>> GetRolesAsync()
    {
        var entities = await _RoleTable.QueryAsync(
            $"PartitionKey eq '{DbRole.PARTITION_KEY}'");
        return entities.Select(r => new Role(r.Id));
    }

    public async Task<IEnumerable<Role>> GetUserAssignedRolesAsync(Guid id)
    {
        var userRoles = await _UsersRolesTable.QueryAsync(
            ur => ur.PartitionKey == id.ToString());
        return userRoles.Select(r => new Role(r.RowKey));
    }

    public async Task<bool> CheckExistsAsync(Role role)
    {
        var entity = await _RoleTable.GetAsync(role.Name);
        return entity is not null;
    }

    public async Task<bool> CreateAsync(Role role)
    {
        _RoleTable.Insert(
            new DbRole(role.Name)
        );
        await _RoleTable.CommitAsync();
        return true;
    }

    public async Task DeleteAsync(Role role)
    {
        _RoleTable.Delete(
            new DbRole(role.Name)
        );
        await _RoleTable.CommitAsync();
    }
}