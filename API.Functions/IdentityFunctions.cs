using System.Net;
using System.Threading.Tasks;
using Domain.Contracts;
using Domain.Exceptions;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace API.Functions;

public class IdentityFunctions
{
    private readonly ILogger<IdentityFunctions> _logger;
    private IAuthenticationService _AuthenticationService { get; }
    private ITokenService _TokenService { get; }

    public IdentityFunctions(
        ILogger<IdentityFunctions> log,
        IAuthenticationService authenticationService,
        ITokenService tokenService)
    {
        _logger = log;
        _AuthenticationService = authenticationService;
        _TokenService = tokenService;
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
    [OpenApiOperation(operationId: "Register", tags: new[] { "identity" })]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(SignUpCredentials), Required = true, Description = "The sign up request")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response with account information")]
    public async Task<IActionResult> Register(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "identity/register")] SignUpCredentials credentials)
    {
        try
        {
            SignUpRequest signUp = new()
            {
                Firstname = credentials.Firstname,
                Lastname = credentials.Lastname,
                Email = credentials.Email,
                Password = credentials.Password,
                ConfirmationPassword = credentials.ConfirmationPassword
            };
            var user = await _AuthenticationService.SignUpAsync(signUp);
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