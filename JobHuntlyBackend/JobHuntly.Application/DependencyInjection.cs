using JobHuntly.Application.Interfaces;
using JobHuntly.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace JobHuntly.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IJobApplicationService, JobApplicationService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<INudgeService, NudgeService>();
        services.AddScoped<IOfferService, OfferService>();
        services.AddScoped<IStoryService, StoryService>();
        services.AddScoped<ISettingsService, SettingsService>();
        return services;
    }
}
