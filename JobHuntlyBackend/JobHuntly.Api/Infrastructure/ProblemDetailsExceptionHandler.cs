using System.Diagnostics;
using JobHuntly.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace JobHuntly.Api.Infrastructure;

/// <summary>
/// Maps exceptions to RFC 7807 problem details. Only <see cref="AppException"/> messages reach the
/// client; anything else becomes a generic 500 with a correlation id and is logged.
/// </summary>
public sealed class ProblemDetailsExceptionHandler(ILogger<ProblemDetailsExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        var (status, title, detail) = exception switch
        {
            NotFoundException e => (StatusCodes.Status404NotFound, "Not found", e.Message),
            ConflictException e => (StatusCodes.Status409Conflict, "Conflict", e.Message),
            UnauthorizedException e => (StatusCodes.Status401Unauthorized, "Unauthorized", e.Message),
            ValidationException e => (StatusCodes.Status422UnprocessableEntity, "Validation failed", e.Message),
            QuotaExceededException e => (StatusCodes.Status429TooManyRequests, "AI allowance used", e.Message),
            AiInvalidOutputException e => (StatusCodes.Status502BadGateway, "AI answer unusable", e.Message),
            ExternalServiceException e => (StatusCodes.Status503ServiceUnavailable, "Service unavailable", e.Message),
            _ => (StatusCodes.Status500InternalServerError, "Server error", "Something went wrong. Quote the trace id if you report it.")
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception. TraceId {TraceId}", traceId);

        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail, Extensions = { ["traceId"] = traceId } };

        if (exception is AppException { Code: { } code }) problem.Extensions["code"] = code;
        if (exception is QuotaExceededException quota)
        {
            problem.Extensions["resetsAt"] = quota.ResetsAtUtc;
            var seconds = Math.Max(1, (int)(quota.ResetsAtUtc - DateTime.UtcNow).TotalSeconds);
            context.Response.Headers.RetryAfter = seconds.ToString();
        }

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }
}
