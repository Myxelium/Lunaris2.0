using Hangfire;
using Hangfire.AspNetCore;
using Lunaris2.Handler.Scheduler;

namespace Lunaris2.Registration;

public static class SchedulerRegistration
{
    public static IServiceCollection AddScheduler(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHangfire((serviceProvider, config) =>
        {
            config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer();
            
            config.UseSqlServerStorage(configuration.GetValue<string>("HangfireConnectionString"));
        });
        
        services.AddHangfireServer();
        
        // Register your handler
        // services.AddScoped<ScheduleMessageHandler>();
        
        return services;
    }
}