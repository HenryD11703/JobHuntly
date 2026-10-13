using JobHuntly.Api.Infrastructure;

namespace JobHuntly.Tests.Unit;

public class DotEnvTests : IDisposable
{
    private readonly string _prefix = "JH_TEST_" + Guid.NewGuid().ToString("N")[..8];

    [Fact]
    public void Exports_values_skipping_comments_blanks_and_empty_values()
    {
        DotEnv.Apply(new[]
        {
            "# comment",
            "",
            $"{_prefix}_A=hello",
            $"{_prefix}_B=\"quoted value\"",
            $"{_prefix}_C=",
            $"{_prefix}_D=a=b=c",
        });

        Assert.Equal("hello", Environment.GetEnvironmentVariable($"{_prefix}_A"));
        Assert.Equal("quoted value", Environment.GetEnvironmentVariable($"{_prefix}_B"));
        Assert.Null(Environment.GetEnvironmentVariable($"{_prefix}_C"));
        Assert.Equal("a=b=c", Environment.GetEnvironmentVariable($"{_prefix}_D"));
    }

    [Fact]
    public void Never_overrides_a_variable_that_is_already_set()
    {
        Environment.SetEnvironmentVariable($"{_prefix}_E", "from-real-environment");

        DotEnv.Apply(new[] { $"{_prefix}_E=from-file" });

        Assert.Equal("from-real-environment", Environment.GetEnvironmentVariable($"{_prefix}_E"));
    }

    public void Dispose()
    {
        foreach (var letter in "ABCDE")
            Environment.SetEnvironmentVariable($"{_prefix}_{letter}", null);
    }
}
