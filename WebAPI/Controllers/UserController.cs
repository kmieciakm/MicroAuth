using Domain.Contracts;
using Domain.Exceptions;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController : IdentityControllerBase
{
    public UserController(
        IAuthorizationService authorizationService,
        IAuthenticationService authenticationService,
        IHttpContextAccessor httpContextAccessor)
        : base(
            authorizationService,
            authenticationService,
            httpContextAccessor)
    {
    }

    [HttpGet("")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public IActionResult GetIdentity()
    {
        if (CurrentUser == null)
        {
            return NotFound();
        }
        return Ok(CurrentUser);
    }

    [HttpPost("{userId}/role")]
    [UserCanManageRoles]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> AddRoleToUser(Guid userId, [FromBody] string roleName)
    {
        try
        {
            var role = new Role(roleName);
            await _AuthorizationService.AssignRoleAsync(role, userId);
            return Ok();
        }
        catch (AuthorizationException authEx) when (authEx.Cause == ExceptionCause.IncorrectData)
        {
            return BadRequest(new { authEx.Message });
        }
    }

    [HttpDelete("{userId}/role")]
    [UserCanManageRoles]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> DeleteRoleFromUser(Guid userId, [FromBody] string roleName)
    {
        try
        {
            var role = new Role(roleName);
            await _AuthorizationService.ReclaimRoleAsync(role, userId);
            return Ok();
        }
        catch (AuthorizationException authEx) when (authEx.Cause == ExceptionCause.IncorrectData)
        {
            return BadRequest(new { authEx.Message });
        }
    }
}