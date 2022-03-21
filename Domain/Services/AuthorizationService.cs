using Domain.Contracts;
using Domain.Exceptions;
using Domain.Infrastructure;
using Domain.Models;
using Microsoft.Extensions.Options;

namespace Domain.Services;

public class AuthorizationService : IAuthorizationService
{
    private IUserRegistry _UserRepository { get; }
    private IRoleRegistry _RoleRepository { get; }
    private AuthorizationSettings _AuthorizationSettings { get; }

    public AuthorizationService(
        IUserRegistry userRepository,
        IRoleRegistry roleRegistry,
        IOptions<AuthorizationSettings> authorizationSettings)
    {
        _UserRepository = userRepository;
        _RoleRepository = roleRegistry;
        _AuthorizationSettings = authorizationSettings.Value;
    }

    public async Task AddPredefinedRolesAsync()
    {
        foreach (var roleName in _AuthorizationSettings.PredefinedRoles)
        {
            Role role = new(roleName);
            if (!await _RoleRepository.CheckExistsAsync(role))
            {
                await _RoleRepository.CreateAsync(role);
            }
        }
    }

    public async Task AssignDefaultRoles(Guid userId)
    {
        await EnsureUserExists(userId);
        foreach (var roleName in _AuthorizationSettings.DefaultRoles)
        {
            Role role = new(roleName);
            await EnsureRoleExists(role);
            await _UserRepository.AddToRoleAsync(userId, role);
        }
    }

    public async Task<bool> DefineRoleAsync(Role role)
    {
        var created = await _RoleRepository.CreateAsync(role);
        if (!created)
        {
            throw new AuthorizationException(
                $"Cannot create new role '{role.Name}'.");
        }
        return created;
    }

    public async Task<bool> CanManageRoles(Guid userId)
    {
        var user = await _UserRepository.GetAsync(userId);
        if (user is null)
        {
            return false;
        }
        return user
            .Roles
            .Any(role => _AuthorizationSettings.ManagementRoles.Contains(role.Name));
    }

    public async Task AssignRoleAsync(Role role, Guid userId)
    {
        await EnsureUserExists(userId);
        await EnsureRoleExists(role);
        await _UserRepository.AddToRoleAsync(userId, role);
    }

    public async Task ReclaimRoleAsync(Role role, Guid userId)
    {
        await EnsureUserExists(userId);
        await EnsureRoleExists(role);

        var isDefaultRole = _AuthorizationSettings
            .DefaultRoles
            .Contains(role.Name);

        if (isDefaultRole)
        {
            throw new AuthorizationException(
                $"Cannot reclaim role '{role.Name}'. It is a default role.",
                ExceptionCause.IncorrectData);
        }

        await _UserRepository.RemoveFromRoleAsync(userId, role);
    }

    private async Task EnsureRoleExists(Role role)
    {
        if (!await _RoleRepository.CheckExistsAsync(role))
        {
            throw new AuthorizationException(
                $"No role '{role.Name}' available.",
                ExceptionCause.IncorrectData);
        }
    }

    private async Task EnsureUserExists(Guid userId)
    {
        if (!await _UserRepository.CheckExistsAsync(userId))
        {
            throw new AuthorizationException(
                $"No user with id '{userId}' found.",
                ExceptionCause.IncorrectData);
        }
    }

    public async Task<IEnumerable<Role>> GetAvailableRolesAsync()
    {
        return await _RoleRepository.GetRolesAsync();
    }
}
