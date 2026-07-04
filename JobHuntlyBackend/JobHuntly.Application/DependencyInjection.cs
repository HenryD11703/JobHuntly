using JobHuntly.Application.Interfaces;
using JobHuntly.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace JobHuntly.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IJobApplicationService, JobApplicationService>();
            services.AddScoped<IAnalyticsService, AnalyticsService>();
            return services;
        }
    }
}
