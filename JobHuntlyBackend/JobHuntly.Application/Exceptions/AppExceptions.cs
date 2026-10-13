namespace JobHuntly.Application.Exceptions;

/// <summary>
/// Base type for errors that are safe to show to the client. <see cref="Code"/> is a stable,
/// machine-readable identifier (for example "ai_quota_exceeded") the frontend can switch on.
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(string message, string? code = null) : base(message) => Code = code;

    public string? Code { get; }
}

/// <summary>The resource does not exist or belongs to another user (deliberately indistinguishable).</summary>
public sealed class NotFoundException(string message = "Resource not found.") : AppException(message);

/// <summary>The request conflicts with current state (e.g. duplicate email).</summary>
public sealed class ConflictException(string message) : AppException(message);

/// <summary>Credentials are missing or wrong.</summary>
public sealed class UnauthorizedException(string message = "Invalid credentials.") : AppException(message);

/// <summary>Input is well-formed but breaks a business rule.</summary>
public sealed class ValidationException(string message) : AppException(message);

/// <summary>A third-party service we depend on for an optional feature failed or is not configured.</summary>
public sealed class ExternalServiceException(string message, string? code = null) : AppException(message, code);

/// <summary>The AI answered, but not in the shape we asked for, even after one retry.</summary>
public sealed class AiInvalidOutputException(string message = "The AI returned an answer we could not use. Please fill this in manually.")
    : AppException(message, "ai_invalid_output");

/// <summary>The daily AI allowance is used up. The manual flow keeps working.</summary>
public sealed class QuotaExceededException(DateTime resetsAtUtc)
    : AppException($"You have used today's AI allowance. It resets at {resetsAtUtc:HH:mm} UTC.", "ai_quota_exceeded")
{
    public DateTime ResetsAtUtc { get; } = resetsAtUtc;
}
