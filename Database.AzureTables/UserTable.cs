using AzureTables;
using Domain.Models;
using Microsoft.Extensions.Options;

namespace Database.AzureTables;

public class DbUser : Entity
{
    public DbUser()
    {
    }

    public DbUser(string firstname, string lastname, string email, string passwordHash, string salt)
    {
        Firstname = firstname;
        Lastname = lastname;
        Email = email;
        PasswordHash = passwordHash;
        Salt = salt;
    }

    public string Firstname { get; set; }
    public string Lastname { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string Salt { get; set; }

    public DbUser(User user) : base()
    {
        Id = user.Guid;
        Firstname = user.Firstname;
        Lastname = user.Lastname;
        Email = user.Email;
    }

    public User ToDomainUser(List<Role> roles)
    {
        return new User(
            Id,
            Firstname,
            Lastname,
            Email,
            roles
        );
    }
}

public interface IUserTable : ITable<DbUser, Guid>
{
}

[Table("Users")]
public class UsersTable : Table<DbUser, Guid>, IUserTable
{
    public UsersTable(IOptions<AzureStorageSettings> storageOptions)
    : base(storageOptions.Value)
    {
    }

    public UsersTable(AzureStorageSettings storageSettings)
        : base(storageSettings)
    {
    }
}