using Domain.Contracts;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace API.Web.Controllers;

[UserFilter]
public class IdentityControllerBase : ControllerBase
{
    protected User? CurrentUser { get; private set; }
    protected IAuthorizationService _AuthorizationService { get; }
    protected IAuthenticationService _AuthenticationService { get; }
    protected IHttpContextAccessor _HttpContextAccessor { get; }

    public IdentityControllerBase(
        IAuthorizationService authorizationService,
        IAuthenticationService authenticationService,
        IHttpContextAccessor httpContextAccessor)
    {
        _AuthorizationService = authorizationService;
        _AuthenticationService = authenticationService;
        _HttpContextAccessor = httpContextAccessor;
    }

    protected async Task<User?> GetLoggedInUserAsync()
    {
        ClaimsPrincipal? userClaims = _HttpContextAccessor.HttpContext?.User;
        string email = userClaims.FindFirstValue(ClaimTypes.Email);
        return await _AuthenticationService.GetIdentityAsync(email);
    }

    private class UserFilterAttribute : TypeFilterAttribute
    {
        public UserFilterAttribute() : base(typeof(UserFilter))
        {
        }
    }

    private class UserFilter : IAsyncActionFilter
    {
        private IAuthenticationService _AuthenticationService { get; }

        public UserFilter(IAuthenticationService authenticationService)
        {
            _AuthenticationService = authenticationService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var controller = context.Controller as IdentityControllerBase;
            var email = controller?.User.FindFirst(ClaimTypes.Email)?.Value ?? "";
            var user = await _AuthenticationService.GetIdentityAsync(email);
            if (controller is not null)
                controller.CurrentUser = user;

            await next();
        }
    }

    protected class UserCanManageRolesAttribute : TypeFilterAttribute
    {
        public UserCanManageRolesAttribute() : base(typeof(RoleManagerFilter))
        {
        }
    }

    private class RoleManagerFilter : IAsyncActionFilter
    {
        private IAuthenticationService _AuthenticationService { get; }
        private IAuthorizationService _AuthorizationService { get; }

        public RoleManagerFilter(
            IAuthenticationService authenticationService,
            IAuthorizationService authorizationService)
        {
            _AuthenticationService = authenticationService;
            _AuthorizationService = authorizationService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var controller = context.Controller as IdentityControllerBase;
            var email = controller?.User.FindFirst(ClaimTypes.Email)?.Value ?? "";
            var user = await _AuthenticationService.GetIdentityAsync(email);

            if (user is null)
            {
                context.Result = new ConflictResult();
                return;
            }

            var cannotManageRoles = !await _AuthorizationService.CanManageRoles(user.Guid);
            if (cannotManageRoles)
            {
                context.Result = new ForbidResult();
                return;
            }

            controller.CurrentUser = user;

            await next();
        }
    }
}
