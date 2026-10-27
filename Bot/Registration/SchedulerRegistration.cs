using Hangfire;
using Hangfire.SqlServer;
using Lunaris2.Handler.Scheduler;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

namespace Lunaris2.Registration;

public static class SchedulerRegistration
{
    public static IServiceCollection AddLunarisScheduler(
        this IServiceCollection services,
        SchedulerSettings settings,
        bool prepareDatabase = false)
    {
        if (!settings.Enabled)
        {
            services.AddSingleton<IMessageScheduler, DisabledMessageScheduler>();
            return services;
        }

        if (string.IsNullOrWhiteSpace(settings.ConnectionString))
            throw new InvalidOperationException("Scheduler is enabled. Set Scheduler:ConnectionString in appsettings.json.");

        _ = settings.ResolveTimeZone();

        if (prepareDatabase)
            SchedulerDatabase.EnsureExists(settings.ConnectionString);

        services.AddHangfire((_, config) => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(settings.ConnectionString, new SqlServerStorageOptions
            {
                SqlClientFactory = SqlClientFactory.Instance,
                PrepareSchemaIfNecessary = true
            }));

        services.AddHangfireServer();
        services.AddSingleton<IMessageScheduler, HangfireMessageScheduler>();
        return services;
    }
}
