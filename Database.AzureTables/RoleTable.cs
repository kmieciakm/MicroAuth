using Azure;
using AzureTables;
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