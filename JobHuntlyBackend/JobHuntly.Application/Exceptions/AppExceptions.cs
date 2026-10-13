namespace JobHuntly.Application.Exceptions;

/// <summary>Base type for errors that are safe to show to the client.</summary>
public abstract class AppException : Exception
{
    protected AppException(string message) : base(message) { }
}

/// <summary>The resource does not exist or belongs to another user (deliberately indistinguishable).</summary>
public sealed class NotFoundException(string message = "Resource not found.") : AppException(message);

/// <summary>The request conflicts with current state (e.g. duplicate email).</summary>
public sealed class ConflictException(string message) : AppException(message);

/// <summary>Credentials are missing or wrong.</summary>
public sealed class UnauthorizedException(string message = "Invalid credentials.") : AppException(message);

/// <summary>Input is well-formed but breaks a business rule.</summary>
public sealed class ValidationException(string message) : AppException(message);

/// <summary>A third-party service we depend on for an optional feature failed.</summary>
public sealed class ExternalServiceException(string message) : AppException(message);
