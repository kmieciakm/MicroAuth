using Domain.Contracts;
using Domain.Exceptions;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace API.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : IdentityControllerBase
{
    public UsersController(
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
    [Authorize]
    public IActionResult GetIdentity()
    {
        if (CurrentUser == null)
        {
            return NotFound();
        }
        return Ok(CurrentUser);
    }

    [HttpPost("{userId}/role")]
    [Authorize]
    [UserCanManageRoles]
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
    [Authorize]
    [UserCanManageRoles]
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