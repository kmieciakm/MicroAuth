using System.Net;
using System.Threading.Tasks;
using Domain.Contracts;
using Domain.Exceptions;
using Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace API.Functions;

public class IdentityFunctions : AuthorizedFunctionBase
{
    private readonly ILogger<IdentityFunctions> _logger;
    private IAuthenticationService _AuthenticationService { get; }
    private IAccountService _AccountService { get; }

    public IdentityFunctions(
        ILogger<IdentityFunctions> log,
        IAuthenticationService authenticationService,
        IAccountService accountService,
        ITokenService tokenService)
    :base(tokenService)
    {
        _logger = log;
        _AuthenticationService = authenticationService;
        _AccountService = accountService;
    }

    [FunctionName("Login")]
    [OpenApiOperation(operationId: "Login", tags: new[] { "identity" })]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(Credentials), Required = true, Description = "The sign in request")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response with JWT token")]
    public async Task<IActionResult> Login(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "identity/login")] Credentials credentials)
    {
        try
        {
            SignInRequest signIn = new()
            {
                Email = credentials.Email,
                Password = credentials.Password
            };
            var token = await _AuthenticationService.SignInAsync(signIn);
            return new OkObjectResult(token);
        }
        catch (AuthenticationException authEx) when (authEx.Cause == ExceptionCause.IncorrectData)
        {
            return new BadRequestObjectResult(authEx.Message);
        }
        catch (AuthenticationException authEx) when (authEx.Cause == ExceptionCause.Unknown)
        {
            _logger.LogError(authEx, "Authentication failed.");
            return new DetailedStatusCodeResult(500, authEx.Message);
        }
    }

    [FunctionName("Register")]
    [OpenApiParameter("API-KEY", In = ParameterLocation.Header, Description = "Authorization API Key")]
    [OpenApiOperation(operationId: "Register", tags: new[] { "identity" })]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(SignUpCredentials), Required = true, Description = "The sign up request")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response with account information")]
    public async Task<IActionResult> Register(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "identity/register")] HttpRequest req)
    {
        try
        {
            var registrationKey = GetAuthorizationApiKey(req);
            var credentials = await DeserializeBodyAsync<SignUpCredentials>(req);
            SignUpRequest signUp = new()
            {
                Firstname = credentials.Firstname,
                Lastname = credentials.Lastname,
                Email = credentials.Email,
                Password = credentials.Password,
                ConfirmationPassword = credentials.ConfirmationPassword
            };
            var user = await _AuthenticationService.SignUpAsync(signUp, registrationKey);
            return new OkObjectResult(user);
        }
        catch (RegistrationException registerEx) when (registerEx.Cause == ExceptionCause.IncorrectData)
        {
            return new BadRequestObjectResult(new { registerEx.Message, registerEx.Details });
        }
        catch (RegistrationException registerEx) when (registerEx.Cause == ExceptionCause.SystemConfiguration)
        {
            return new UnauthorizedResult();
        }
        catch (RegistrationException registerEx) when (registerEx.Cause == ExceptionCause.Unknown)
        {
            _logger.LogError(registerEx, "Registration failed.");
            return new DetailedStatusCodeResult(500, registerEx.Message);
        }
    }

    [FunctionName("Validate")]
    [OpenApiOperation(operationId: "Validate", tags: new[] { "identity" })]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(string), Required = true, Description = "The JWT token")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response with validation result")]
    public async Task<IActionResult> Validate(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "identity/validate")] string jwt)
    {
        var token = new Token(jwt);
        var isValid = await _TokenService.ValidateSecurityTokenAsync(token);
        return new OkObjectResult(isValid);
    }

    [FunctionName("ForgotPassword")]
    [OpenApiOperation(operationId: "ForgotPassword", tags: new[] { "identity" })]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(ForgotPasswordRequest), Required = true, Description = "Request with account email")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
    public async Task<IActionResult> ForgotPassword(
         [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "identity/forgotpassword")] ForgotPasswordRequest request)
    {
        try
        {
            var user = await _AuthenticationService.GetIdentityAsync(request.Email);
            if (user is null)
            {
                return new BadRequestResult();
            }
            await _AccountService.RequestPasswordReset(user.Guid);
            return new OkObjectResult(new { message = "Reset link sent successfully" });
        }
        catch (AccountException accountExc) when (accountExc.Cause == ExceptionCause.IncorrectData)
        {
            return new BadRequestObjectResult(new { accountExc.Message, accountExc.Details });
        }
        catch (AccountException accountExc) when (accountExc.Cause == ExceptionCause.Unknown)
        {
            _logger.LogError(accountExc, "Request password request failed to send.");
            return new DetailedStatusCodeResult(500, accountExc.Message);
        }
    }

    [FunctionName("ResetPassword")]
    [OpenApiOperation(operationId: "ResetPassword", tags: new[] { "identity" })]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(ChangePasswordRequest), Required = true, Description = "Request with new password")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
    public async Task<IActionResult> ResetPassword(
         [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "identity/resetpassword")] ChangePasswordRequest request)
    {
        try
        {
            var user = await _AuthenticationService.GetIdentityAsync(request.Email);
            if (user is null)
            {
                return new BadRequestResult();
            }
            await _AccountService.ResetPassword(user.Guid, new ResetToken(request.Token), request.NewPassword);
            return new OkObjectResult(new { message = "Password changed successfully" });
        }
        catch (AccountException accountExc) when (accountExc.Cause == ExceptionCause.IncorrectData)
        {
            return new BadRequestObjectResult(new { accountExc.Message, accountExc.Details });
        }
        catch (AccountException accountExc) when (accountExc.Cause == ExceptionCause.Unknown)
        {
            _logger.LogError(accountExc, "Reset password failed.");
            return new DetailedStatusCodeResult(500, accountExc.Message);
        }
    }
}

public class Credentials
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public class SignUpCredentials
{
    public string Firstname { get; set; }
    public string Lastname{ get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string ConfirmationPassword { get; set; }
}