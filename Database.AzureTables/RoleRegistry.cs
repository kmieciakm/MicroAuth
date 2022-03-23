using Azure;
using AzureTables;
using Domain.Infrastructure;
using Domain.Models;
using Microsoft.Extensions.Options;

namespace Database.AzureTables;

public class DbRole : IEntity<string>
{
    public string Id
    {
        get { return RowKey; }
        set { RowKey = value; }
    }
    public static string PARTITION_KEY = "Roles";
    public string PartitionKey { get; set; } = PARTITION_KEY;
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public DbRole()
    {
    }

    public DbRole(string name)
    {
        Id = name;
    }
}

public interface IRoleTable : ITable<DbRole, string>
{
}

[Table("Roles")]
public class RolesTable : Table<DbRole, string>, IRoleTable
{
    public RolesTable(IOptions<AzureStorageSettings> storageOptions)
    : base(storageOptions.Value)
    {
    }

    public RolesTable(AzureStorageSettings storageSettings)
        : base(storageSettings)
    {
    }
}

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