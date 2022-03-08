using Domain.Infrastructure;
using Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Database.MsSQL;

public class UserRegistry : IUserRegistry
{
    private SignInManager<DbUser> _SignInManager { get; }
    private UserManager<DbUser> _UserManager { get; }

    public UserRegistry(SignInManager<DbUser> signInManager, UserManager<DbUser> userManager)
    {
        _SignInManager = signInManager;
        _UserManager = userManager;
    }

    public async Task<User?> GetAsync(Guid id)
    {
        var dbUser = await _UserManager
            .Users
            .FirstOrDefaultAsync(user => user.Id == id.ToString());

        return dbUser?.ToDomainUser();
    }

    public async Task<User?> GetAsync(string email)
    {
        var dbUser = await GetDbUserByEmailAsync(email);
        return dbUser?.ToDomainUser();
    }

    private async Task<DbUser?> GetDbUserByIdAsync(Guid id)
    {
        return await _UserManager
            .Users
            .FirstOrDefaultAsync(user => user.Id == id.ToString());
    }

    private async Task<DbUser?> GetDbUserByEmailAsync(string email)
    {
        return await _UserManager
            .Users
            .FirstOrDefaultAsync(user => user.Email == email);
    }

    public async Task<bool> CreateAsync(User user, string password)
    {
        var dbUser = new DbUser(user);
        var result = await _UserManager.CreateAsync(dbUser, password);

        return result.Succeeded;
    }

    public async Task<bool> AuthenticateAsync(string email, string password)
    {
        var user = await GetDbUserByEmailAsync(email);
        if (user == null)
        {
            return false;
        }
        var signInResult = await _SignInManager.CheckPasswordSignInAsync(user, password, false);
        return signInResult.Succeeded;
    }

    public async Task<ValidationResult> ValidatePasswordAsync(string password)
    {
        ValidationResult validationResult = new();
        foreach (var validator in _UserManager.PasswordValidators)
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            var result = await validator.ValidateAsync(_UserManager, null, password);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            if (!result.Succeeded)
            {
                validationResult.IsValid = false;
                foreach (var err in result.Errors)
                {
                    validationResult.Errors.Add(err.Description);
                }
            }
        }
        return validationResult;
    }

    public async Task DeleteAsync(Guid id)
    {
        var dbUser = await GetDbUserByIdAsync(id);
        if (dbUser != null)
        {
            await _UserManager.DeleteAsync(dbUser);
        }
    }
}