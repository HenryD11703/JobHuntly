using JobHuntly.Application.Interfaces;
using JobHuntly.Infrastructure.Data;
using JobHuntly.Infrastructure.Rates;
using JobHuntly.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JobHuntly.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = PostgresConnectionString.Normalize(configuration.GetConnectionString("DefaultConnection"));

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                connection,
                b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IJobApplicationRepository, JobApplicationRepository>();
        services.AddScoped<IOfferRepository, OfferRepository>();
        services.AddScoped<IStoryRepository, StoryRepository>();
        services.AddScoped<IJwtTokenGenerator, Auth.JwtTokenGenerator>();

        services.AddMemoryCache();
        services.AddHttpClient<IExchangeRateProvider, OpenErApiRateProvider>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("JobHuntly/1.0 (portfolio project)");
        });

        return services;
    }
}
