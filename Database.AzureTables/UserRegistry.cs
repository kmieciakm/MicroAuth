using AzureTables;
using Domain.Infrastructure;
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

public class UserRegistry : IUserRegistry
{
    private IUserTable _UserTable { get; }
    private IUsersRolesTable _UsersRolesTable { get; }
    private IRoleRegistry _RoleRegistry { get; }

    public UserRegistry(
        IUserTable userTable,
        IUsersRolesTable usersRolesTable,
        IRoleRegistry roleRegistry)
    {
        _UserTable = userTable;
        _UsersRolesTable = usersRolesTable;
        _RoleRegistry = roleRegistry;
    }

    public async Task<bool> AuthenticateAsync(string email, string password)
    {
        var dbUser = (await _UserTable
            .QueryAsync(user => user.Email == email))
            .FirstOrDefault();

        if (dbUser is not null)
        {
            return PasswordHelper.Validate(password, dbUser.Salt, dbUser.PasswordHash);
        }
        return false;
    }

    public async Task<bool> CheckExistsAsync(Guid userId)
    {
        var user = await GetAsync(userId);
        return user is not null;
    }

    public async Task<bool> CreateAsync(User user, string password)
    {
        var salt = PasswordHelper.GenerateSalt();
        var dbUser = new DbUser(user)
        {
            PasswordHash = PasswordHelper.HashPassword(password, salt),
            Salt = salt
        };
        _UserTable.Insert(dbUser);
        await _UserTable.CommitAsync();
        return true;
    }

    public async Task DeleteAsync(Guid id)
    {
        var dbUser = await _UserTable.GetAsync(id);
        _UserTable.Delete(dbUser);
        await _UserTable.CommitAsync();
    }

    public async Task<User?> GetAsync(Guid id)
    {
        var dbUser = await _UserTable.GetAsync(id);
        var roles = await _RoleRegistry.GetUserAssignedRolesAsync(id);
        return dbUser?.ToDomainUser(roles.ToList());
    }

    public async Task<User?> GetAsync(string email)
    {
        var dbUser = (await _UserTable
            .QueryAsync(user => user.Email == email))
            .FirstOrDefault();

        if (dbUser is not null)
        {
            var roles = await _RoleRegistry.GetUserAssignedRolesAsync(dbUser.Id);
            return dbUser?.ToDomainUser(roles.ToList());
        }
        return null;
    }

    public async Task AddToRoleAsync(Guid userId, Role role)
    {
        _UsersRolesTable.Insert(
            new UserInRole(userId, role.Name)
        );
        await _UsersRolesTable.CommitAsync();
    }

    public async Task RemoveFromRoleAsync(Guid userId, Role role)
    {
        _UsersRolesTable.Delete(
            new UserInRole(userId, role.Name)
        );
        await _UsersRolesTable.CommitAsync();
    }

    public Task<ValidationResult> ValidatePasswordAsync(string password)
    {
        ValidationResult validationResult = new() { IsValid = true, Errors = new() };
        return Task.FromResult(validationResult);
    }
}
