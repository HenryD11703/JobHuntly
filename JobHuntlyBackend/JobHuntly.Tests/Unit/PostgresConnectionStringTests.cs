using JobHuntly.Infrastructure.Data;
using Npgsql;

namespace JobHuntly.Tests.Unit;

public class PostgresConnectionStringTests
{
    [Fact]
    public void Converts_a_neon_style_uri_to_a_key_value_string()
    {
        var result = PostgresConnectionString.Normalize(
            "postgresql://alice:p%40ss@ep-cool-1.example.aws.neon.tech/mydb?sslmode=require&channel_binding=require");

        var b = new NpgsqlConnectionStringBuilder(result);
        Assert.Equal("ep-cool-1.example.aws.neon.tech", b.Host);
        Assert.Equal(5432, b.Port);
        Assert.Equal("mydb", b.Database);
        Assert.Equal("alice", b.Username);
        Assert.Equal("p@ss", b.Password);
        Assert.Equal(SslMode.Require, b.SslMode);
    }

    [Fact]
    public void Accepts_the_postgres_scheme_and_an_explicit_port()
    {
        var b = new NpgsqlConnectionStringBuilder(
            PostgresConnectionString.Normalize("postgres://u:p@localhost:6543/db"));

        Assert.Equal("localhost", b.Host);
        Assert.Equal(6543, b.Port);
    }

    [Fact]
    public void Leaves_key_value_strings_untouched()
    {
        const string raw = "Host=localhost;Database=x;Username=u;Password=p";
        Assert.Equal(raw, PostgresConnectionString.Normalize(raw));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Throws_a_helpful_error_when_missing(string? raw)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => PostgresConnectionString.Normalize(raw));
        Assert.Contains(".env.example", ex.Message);
    }
}
