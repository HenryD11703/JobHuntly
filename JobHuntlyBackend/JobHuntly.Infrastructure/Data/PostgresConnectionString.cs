using System.Globalization;
using Npgsql;

namespace JobHuntly.Infrastructure.Data;

/// <summary>
/// Accepts either an Npgsql key/value connection string or the <c>postgresql://</c> URI that
/// Neon shows in its dashboard, and returns an Npgsql key/value connection string.
/// </summary>
public static class PostgresConnectionString
{
    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is not configured. Set ConnectionStrings__DefaultConnection (see .env.example).");

        var value = raw.Trim();
        var isUri = value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)
                    || value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase);
        if (!isUri) return value;

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Host))
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection looks like a URI but is not valid.");

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort || uri.Port < 0 ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
        };

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            var parts = uri.UserInfo.Split(':', 2);
            builder.Username = Uri.UnescapeDataString(parts[0]);
            if (parts.Length == 2) builder.Password = Uri.UnescapeDataString(parts[1]);
        }

        foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = pair.Split('=', 2);
            var key = kv[0].ToLower(CultureInfo.InvariantCulture);
            var val = kv.Length == 2 ? Uri.UnescapeDataString(kv[1]) : string.Empty;
            if (key == "sslmode")
            {
                builder.SslMode = val.ToLower(CultureInfo.InvariantCulture) switch
                {
                    "disable" => SslMode.Disable,
                    "prefer" => SslMode.Prefer,
                    "verify-ca" => SslMode.VerifyCA,
                    "verify-full" => SslMode.VerifyFull,
                    _ => SslMode.Require,
                };
            }
            // Other URI options (e.g. channel_binding) are intentionally ignored.
        }

        return builder.ConnectionString;
    }
}
