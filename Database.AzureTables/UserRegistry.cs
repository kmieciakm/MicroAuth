using Database.AzureTables.Helpers;
using Domain.Infrastructure;
using Domain.Models;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace Database.AzureTables;

public class UserRegistry : IUserRegistry
{
    private IUserTable _UserTable { get; }
    private IUsersRolesTable _UsersRolesTable { get; }
    private IRoleRegistry _RoleRegistry { get; }
    private ITokenTable _TokenTable { get; }
    private AuthenticationSettings _AuthenticationSettings { get; }

    public UserRegistry(
        IUserTable userTable,
        IUsersRolesTable usersRolesTable,
        ITokenTable tokenTable,
        IRoleRegistry roleRegistry,
        IOptions<AuthenticationSettings> authenticationOptions)
    {
        _UserTable = userTable;
        _UsersRolesTable = usersRolesTable;
        _TokenTable = tokenTable;
        _RoleRegistry = roleRegistry;
        _AuthenticationSettings = authenticationOptions.Value;
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
        var hasNumber = new Regex(@"[0-9]+");
        var hasUpperChar = new Regex(@"[A-Z]+");
        var hasMinimum8Chars = new Regex(@".{8,}");

        var isValid = hasNumber.IsMatch(password) &&
                        hasUpperChar.IsMatch(password) &&
                        hasMinimum8Chars.IsMatch(password);
        validationResult.IsValid = isValid;

        if (!isValid)
        {
            if (!hasNumber.IsMatch(password))
                validationResult.Errors.Add("Password must contin a number.");
            if (!hasUpperChar.IsMatch(password))
                validationResult.Errors.Add("Password must contin an uppercase character.");
            if (!hasMinimum8Chars.IsMatch(password))
                validationResult.Errors.Add("Password must be at least 8 characters long.");
        }

        return Task.FromResult(validationResult);
    }

    public async Task<ResetToken?> GeneratePasswordResetTokenAsync(string email)
    {
        var user = await GetAsync(email);
        if (user is not null)
        {
            var token = TokenHelper.GenerateToken();
            var dbToken = new DbToken(user.Guid, TokenType.RESET_PASSWORD_TOKEN, token);
            var existingToken = await _TokenTable.GetAsync(
                user.Guid.ToString(),
                TokenType.RESET_PASSWORD_TOKEN.GetStringValue());

            if (existingToken is not null)
            {
                _TokenTable.Update(dbToken);
            }
            else
            {
                _TokenTable.Insert(dbToken);
            }
            await _TokenTable.CommitAsync();
            return new ResetToken(token);
        }
        return null;
    }

    public async Task<bool> ResetPassword(Guid userId, ResetToken token, string newPassword)
    {
        var dbUser = await _UserTable.GetAsync(userId);
        if (dbUser is not null)
        {
            var resetToken = await _TokenTable.GetAsync(
                dbUser.Id.ToString(),
                TokenType.RESET_PASSWORD_TOKEN.GetStringValue());

            if (resetToken is not null && TokensMatch(token, resetToken) && !TokenExpired(resetToken))
            {
                var salt = PasswordHelper.GenerateSalt();
                dbUser.Salt = salt;
                dbUser.PasswordHash = PasswordHelper.HashPassword(newPassword, salt);

                _UserTable.Update(dbUser);
                await _UserTable.CommitAsync();
                _TokenTable.Delete(resetToken);
                await _TokenTable.CommitAsync();

                return true;
            }
        }
        return false;
    }

    private bool TokensMatch(ResetToken token1, DbToken token2)
    {
        return token1.Value == token2.Token;
    }

    private bool TokenExpired(DbToken resetToken)
    {
        if (resetToken.Timestamp is null) return true;

        var maxTimeOffset = resetToken
            .Timestamp.Value
            .AddHours(_AuthenticationSettings.ExpirationHours);

        var compareResult = DateTimeOffset.Compare(
            DateTimeOffset.UtcNow,
            maxTimeOffset
        );
        return compareResult > 0;
    }
}
