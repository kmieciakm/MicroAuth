using Domain.Contracts;
using Domain.Exceptions;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace API.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
[UserCanManageRoles]
public class RolesController : IdentityControllerBase
{
    public RolesController(
        IAuthorizationService authorizationService,
        IAuthenticationService authenticationService,
        IHttpContextAccessor httpContextAccessor)
        : base(
            authorizationService,
            authenticationService,
            httpContextAccessor)
    {
    }

    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] string roleName)
    {
        try
        {
            var role = new Role(roleName);
            await _AuthorizationService.DefineRoleAsync(role);
            return Ok();
        }
        catch (AuthorizationException authEx) when (authEx.Cause == ExceptionCause.IncorrectData)
        {
            return BadRequest(new { authEx.Message });
        }
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _AuthorizationService.GetAvailableRolesAsync();
        return Ok(roles);
    }
}