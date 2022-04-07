using Domain.Contracts;
using Domain.Exceptions;
using Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class IdentityController : ControllerBase
{
    private ITokenService _TokenService { get; }
    private IAuthenticationService _AuthenticationService { get; }

    public IdentityController(
        ITokenService tokenService,
        IAuthenticationService authenticationService)
    {
        _TokenService = tokenService;
        _AuthenticationService = authenticationService;
    }

    [HttpPost("validate")]
    public async Task<IActionResult> Validate([FromBody] string jwt)
    {
        var token = new Token(jwt);
        var isValid = await _TokenService.ValidateTokenAsync(token);
        return Ok(isValid);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] SignInRequest signIn)
    {
        try
        {
            var token = await _AuthenticationService.SignInAsync(signIn);
            return Ok(token);
        }
        catch (AuthenticationException authEx) when (authEx.Cause == ExceptionCause.IncorrectData)
        {
            return BadRequest(authEx.Message);
        }
        catch (AuthenticationException authEx) when (authEx.Cause == ExceptionCause.Unknown)
        {
            return Problem(statusCode: 500, detail: authEx.Message);
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] SignUpRequest signUp)
    {
        try
        {
            var user = await _AuthenticationService.SignUpAsync(signUp);
            return Ok(user);
        }
        catch (RegistrationException registerEx) when (registerEx.Cause == ExceptionCause.IncorrectData)
        {
            return BadRequest(new { registerEx.Message, registerEx.Details });
        }
        catch (RegistrationException registerEx) when (registerEx.Cause == ExceptionCause.SystemConfiguration)
        {
            return Unauthorized();
        }
    }
}
