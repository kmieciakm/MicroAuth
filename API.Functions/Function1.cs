using System.IO;
using System.Net;
using System.Threading.Tasks;
using Domain.Contracts;
using Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace API.Functions;

public class Function1 : AuthorizedFunctionBase
{
    private readonly ILogger<Function1> _logger;

    public Function1(ILogger<Function1> log, ITokenService tokenService)
     : base(tokenService)
    {
        _logger = log;
    }

    [FunctionName("Function1")]
    [OpenApiSecurity("bearer", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Header, Name = "Authorization", BearerFormat = "JWT", Description = "Enter JWT Bearer token", Scheme = OpenApiSecuritySchemeType.Bearer)]
    [OpenApiOperation(operationId: "Hello", tags: new[] { "test" })]
    [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "text/plain", bodyType: typeof(string), Description = "Invalid authorization")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "test/hello")] HttpRequest req)
    {
        try
        {
            return await ExecuteAuthorized(req, async () => {
                _logger.LogInformation("C# HTTP trigger function processed a request.");

                string name = req.Query["name"];

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                name = name ?? data?.name;

                string responseMessage = string.IsNullOrEmpty(name)
                    ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                    : $"Hello, {name}. This HTTP triggered function executed successfully.";

                return new OkObjectResult(responseMessage);
            });
        }
        catch (AuthenticationException authExc)
        {
            _logger.LogError(authExc, "Authentication failed.");
            return new UnauthorizedResult();
        }
    }
}

