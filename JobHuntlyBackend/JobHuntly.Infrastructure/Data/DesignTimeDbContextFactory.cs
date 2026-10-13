using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace JobHuntly.Infrastructure.Data;

/// <summary>
/// Lets `dotnet ef` create migrations without starting the API or needing a real database.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var raw = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        var connection = string.IsNullOrWhiteSpace(raw)
            ? "Host=localhost;Database=jobhuntly_design;Username=design;Password=design"
            : PostgresConnectionString.Normalize(raw);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connection)
            .Options;
        return new AppDbContext(options);
    }
}
