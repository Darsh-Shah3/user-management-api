using Microsoft.AspNetCore.Mvc;

namespace UserManagementAPI.Middleware;

public sealed class ErrorHandlingMiddleware(RequestDelegate next, IHostEnvironment env)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            var includeDetails = env.IsDevelopment();

            var problem = new ProblemDetails
            {
                Title = "Unhandled exception.",
                Status = StatusCodes.Status500InternalServerError,
                Detail = includeDetails ? ex.Message : "An unexpected error occurred.",
                Instance = context.Request.Path,
                Extensions =
                {
                    ["traceId"] = context.TraceIdentifier
                }
            };

            if (includeDetails)
            {
                problem.Extensions["exception"] = ex.GetType().FullName ?? "Exception";
            }

            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}

