using System;
using Domain.Contracts;
using Domain.Exceptions;
using Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;

namespace API.Functions;

/// <summary>
/// Base class for authenticated service which checks the incoming JWT token.
/// </summary>
public abstract class AuthorizedFunctionBase : FunctionBase
{
    private const string APIKeyHeaderName = "API-KEY";
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

        string jwt = req.Headers[AuthenticationHeaderName];
        if (jwt.StartsWith("Bearer"))
        {
            jwt = jwt.Substring(7);
        }

        Token token = new() { JWT = jwt };
        if (await _TokenService.ValidateSecurityTokenAsync(token))
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

    public string GetAuthorizationApiKey(HttpRequest req)
        => req.Headers[APIKeyHeaderName];
}

public abstract class FunctionBase
{
    public async Task<T> DeserializeBodyAsync<T>(HttpRequest req)
    {
        var json = await req.ReadAsStringAsync();
        var body = JsonConvert.DeserializeObject<T>(json);
        return body;
    }
}