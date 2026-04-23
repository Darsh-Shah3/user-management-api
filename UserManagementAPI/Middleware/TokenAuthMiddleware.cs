namespace UserManagementAPI.Middleware;

public sealed class TokenAuthMiddleware(RequestDelegate next, IConfiguration config)
{
    private readonly string? _token = config["Auth:ApiToken"];

    public async Task Invoke(HttpContext context)
    {
        if (ShouldSkipAuth(context.Request))
        {
            await next(context);
            return;
        }

        if (string.IsNullOrWhiteSpace(_token))
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Server misconfiguration",
                message = "Auth token not configured."
            });
            return;
        }

        var header = context.Request.Headers.Authorization.ToString();
        if (!TryGetBearerToken(header, out var bearer) || !FixedTimeEquals(bearer, _token))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.WWWAuthenticate = "Bearer";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Unauthorized",
                message = "Missing or invalid bearer token."
            });
            return;
        }

        await next(context);
    }

    private static bool ShouldSkipAuth(HttpRequest request)
    {
        var path = request.Path.Value ?? "";

        if (path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase)) return true;
        if (path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)) return true;
        if (path.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase)) return true;

        // Protect only the API endpoints for this activity.
        if (path.StartsWith("/users", StringComparison.OrdinalIgnoreCase)) return false;

        return true;
    }

    private static bool TryGetBearerToken(string headerValue, out string token)
    {
        token = "";
        if (string.IsNullOrWhiteSpace(headerValue)) return false;

        const string prefix = "Bearer ";
        if (!headerValue.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;

        token = headerValue[prefix.Length..].Trim();
        return token.Length > 0;
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        // Avoid leaking token validity via timing differences.
        if (a.Length != b.Length) return false;
        var diff = 0;
        for (var i = 0; i < a.Length; i++)
        {
            diff |= a[i] ^ b[i];
        }
        return diff == 0;
    }
}

