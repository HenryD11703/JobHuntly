using JobHuntly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JobHuntly.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connection = PostgresConnectionString.Normalize(configuration.GetConnectionString("DefaultConnection"));

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(
                    connection,
                    b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

            services.AddScoped<JobHuntly.Application.Interfaces.IUserRepository, JobHuntly.Infrastructure.Repositories.UserRepository>();
            services.AddScoped<JobHuntly.Application.Interfaces.IJobApplicationRepository, JobHuntly.Infrastructure.Repositories.JobApplicationRepository>();
            services.AddScoped<JobHuntly.Application.Interfaces.IJwtTokenGenerator, JobHuntly.Infrastructure.Auth.JwtTokenGenerator>();

            return services;
        }
    }
}
