using Azure;
using AzureTables;
using Microsoft.Extensions.Options;

namespace Database.AzureTables;

public class UserInRole : IEntity<string>
{
    public UserInRole()
    {
    }

    public UserInRole(Guid userId, string roleName)
    {
        PartitionKey = userId.ToString();
        RowKey = roleName;
    }

    public string Id
    {
        get { return $"{PartitionKey}:{RowKey}"; }
        set
        {
            PartitionKey = value.Split(':')[0];
            RowKey = value.Split(':')[1];
        }
    }
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public ETag ETag { get; set; }
}

public interface IUsersRolesTable : ITable<UserInRole, string>
{
}

[Table("UsersInRole")]
public class UsersRolesTable : Table<UserInRole, string>, IUsersRolesTable
{
    public UsersRolesTable(IOptions<AzureStorageSettings> storageOptions)
        : base(storageOptions.Value)
    {
    }

    public UsersRolesTable(AzureStorageSettings storageSettings)
        : base(storageSettings)
    {
    }
}
