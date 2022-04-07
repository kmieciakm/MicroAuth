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

    public IdentityFunctions(ILogger<IdentityFunctions> log, IAuthenticationService authenticationService)
    {
        _logger = log;
        _AuthenticationService = authenticationService;
    }

    [FunctionName("Login")]
    [OpenApiOperation(operationId: "Login")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(Credentials), Required = true, Description = "The sign in request")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response with JWT token")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "login")]
            Credentials credentials)
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
            return new DetailedStatusCodeResult(500, authEx.Message);
        }
    }
}

public class Credentials
{
    public string Email { get; set; }
    public string Password { get; set; }
}