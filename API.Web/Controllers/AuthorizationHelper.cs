namespace API.Web.Controllers;

public static class AuthorizationHelper
{
    private const string AuthenticationHeaderName = "API-KEY";

    public static string? GetAuthorizationApiKey(HttpRequest req)
        => req.Headers[AuthenticationHeaderName];
}