namespace UserManagementAPI.Middleware;

public sealed class RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        var method = context.Request.Method;
        var path = context.Request.Path.ToString();

        await next(context);

        var status = context.Response.StatusCode;
        logger.LogInformation("HTTP {Method} {Path} => {StatusCode}", method, path, status);
    }
}

