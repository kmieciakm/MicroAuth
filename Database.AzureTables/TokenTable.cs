using Azure;
using AzureTables;
using Database.AzureTables.Helpers;
using Microsoft.Extensions.Options;

namespace Database.AzureTables;

public enum TokenType
{
    [StringValue("RESET_PASSWORD")]
    RESET_PASSWORD_TOKEN = 0
}

public class DbToken : IEntity<string>
{
    public DbToken()
    {
    }

    public DbToken(Guid userId, TokenType tokenType, string token)
    {
        PartitionKey = userId.ToString();
        RowKey = tokenType.GetStringValue() ?? "GENERAL_TOKEN";
        Token = token;
    }

    public string Token { get; set; }

    public string Id
    {
        get { return $"{PartitionKey}:{RowKey}"; }
        set {
            PartitionKey = value.Split(':')[0];
            RowKey = value.Split(':')[1];
        }
    }

    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public ETag ETag { get; set; }
}

public interface ITokenTable : ITable<DbToken, string>
{
}

[Table("Tokens")]
public class TokenTable : Table<DbToken, string>, ITokenTable
{
    public TokenTable(IOptions<AzureStorageSettings> storageOptions)
        : base(storageOptions.Value)
    {
    }

    public TokenTable(AzureStorageSettings storageSettings)
        : base(storageSettings)
    {
    }
}
