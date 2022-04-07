using Domain.Contracts;
using Domain.Exceptions;
using Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace API.Functions;

/// <summary>
/// Base class for authenticated service which checks the incoming JWT token.
/// </summary>
public abstract class AuthorizedFunctionBase
{
    private const string AuthenticationHeaderName = "Authorization";
    protected ITokenService _TokenService { get; }

    public AuthorizedFunctionBase(ITokenService tokenService)
    {
        _TokenService = tokenService;
    }

    public async Task<IActionResult> ExecuteAuthorized(HttpRequest req, Func<Task<IActionResult>> azureFunction)
    {
        if (req is null || !req.Headers.ContainsKey(AuthenticationHeaderName))
        {
            throw new AuthenticationException("No Authorization header was present");
        }

        Token token = new() { JWT = req.Headers[AuthenticationHeaderName] };
        if (await _TokenService.ValidateTokenAsync(token))
        {
            try
            {
                return await azureFunction();
            }
            catch (Exception e)
            {
                return new DetailedStatusCodeResult(500, e.Message);
            }
        }
        throw new AuthenticationException("Invalid token");
    }
}
