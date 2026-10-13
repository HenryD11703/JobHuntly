namespace JobHuntly.Api.Infrastructure;

/// <summary>
/// Minimal `.env` loader for local development. Walks up from the working directory to find a
/// `.env` file and exports its entries as environment variables, without overriding variables
/// that are already set (so real environment variables, e.g. Fly secrets, always win).
/// </summary>
public static class DotEnv
{
    public static void Load()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null)
        {
            var file = Path.Combine(dir.FullName, ".env");
            if (File.Exists(file))
            {
                Apply(File.ReadAllLines(file));
                return;
            }
            dir = dir.Parent;
        }
    }

    public static void Apply(IEnumerable<string> lines)
    {
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;

            var separator = line.IndexOf('=');
            if (separator <= 0) continue;

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();
            if (value.Length >= 2 && (value[0] == '"' || value[0] == '\'') && value[^1] == value[0])
                value = value[1..^1];

            // Empty values in the file mean "not set yet"; never export them.
            if (value.Length == 0) continue;
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key))) continue;

            Environment.SetEnvironmentVariable(key, value);
        }
    }
}
