using Microsoft.AspNetCore.Mvc;

namespace JobHuntly.Api.Infrastructure;

/// <summary>
/// The auth token lives in an HttpOnly cookie, so state-changing requests must prove they come
/// from our own frontend: browsers will not add a custom header to a cross-site form post.
/// </summary>
public sealed class CsrfGuardMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Requested-With";
    public const string HeaderValue = "jobhuntly";

    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;
        var isSafe = HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method);

        if (!isSafe
            && context.Request.Path.StartsWithSegments("/api")
            && context.Request.Headers[HeaderName] != HeaderValue)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Detail = $"Missing {HeaderName} header."
            });
            return;
        }

        await next(context);
    }
}
