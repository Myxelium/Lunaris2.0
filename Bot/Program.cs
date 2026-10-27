using System.Reflection;
using Discord;
using Discord.WebSocket;
using Hangfire;
using Lavalink4NET.Extensions;
using Lavalink4NET.Integrations.SponsorBlock.Extensions;
using Lunaris2.Handler.ChatCommand;
using Lunaris2.Handler.MusicPlayer;
using Lunaris2.Handler.Scheduler;
using Lunaris2.Notification;
using Lunaris2.Registration;
using Lunaris2.Service;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lunaris2;

public class Program
{
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
        {
            Console.WriteLine(eventArgs.ExceptionObject);
        };

        var configuration = BuildConfiguration();
        var schedulerSettings = configuration.GetSection("Scheduler").Get<SchedulerSettings>() ?? new SchedulerSettings();
        var app = CreateHostBuilder(args, configuration, schedulerSettings).Build();
        app.UseSponsorBlock();

        if (schedulerSettings.Enabled)
            Console.WriteLine($"Scheduler enabled. Dashboard: {schedulerSettings.ListenUrl}{SchedulerSettings.DashboardPath}");

        app.Run();
    }

    private static IConfiguration BuildConfiguration() =>
        new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

    private static IHostBuilder CreateHostBuilder(
        string[] args,
        IConfiguration configuration,
        SchedulerSettings schedulerSettings)
    {
        var builder = Host.CreateDefaultBuilder(args)
            .ConfigureServices((_, services) =>
            {
                var socketConfig = new DiscordSocketConfig
                {
                    GatewayIntents = GatewayIntents.All,
                    LogLevel = LogSeverity.Info
                };

                var client = new DiscordSocketClient(socketConfig);

                services
                    .AddMediatR(mediatRServiceConfiguration => mediatRServiceConfiguration.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()))
                    .AddLavalink()
                    .ConfigureLavalink(options =>
                    {
                        options.BaseAddress = new Uri(
                            $"http://{configuration["LavaLinkHostname"]}:{configuration["LavaLinkPort"]}"
                        );
                        options.WebSocketUri = new Uri($"ws://{configuration["LavaLinkHostname"]}:{configuration["LavaLinkPort"]}/v4/websocket");
                        options.Passphrase = configuration["LavaLinkPassword"] ?? "youshallnotpass";
                        options.Label = "Node";
                    })
                    .AddSingleton(client)
                    .AddSingleton(new DiscordSettings(configuration["Token"]))
                    .AddSingleton<MusicEmbed>()
                    .AddSingleton<PlaybackNotifier>()
                    .AddSingleton<ChatSettings>()
                    .AddSingleton<ChatSessionStore>()
                    .AddSingleton<AloneLeaveScheduler>()
                    .AddSingleton<VoiceChannelMonitorService>()
                    .AddSingleton<DiscordEventListener>()
                    .AddSingleton(service => new Discord.Interactions.InteractionService(service.GetRequiredService<DiscordSocketClient>()))
                    .AddHostedService(service => service.GetRequiredService<VoiceChannelMonitorService>())
                    .AddHostedService<DiscordClientHostedService>()
                    .Configure<ChatSettings>(configuration.GetSection("LLM"))
                    .Configure<SchedulerSettings>(configuration.GetSection("Scheduler"))
                    .AddLunarisScheduler(schedulerSettings, prepareDatabase: schedulerSettings.Enabled);

                client.Log += message =>
                {
                    Console.WriteLine(message);
                    return Task.CompletedTask;
                };
            });

        if (!schedulerSettings.Enabled)
            return builder;

        return builder.ConfigureWebHostDefaults(web =>
        {
            web.UseUrls(schedulerSettings.ListenUrl);
            web.Configure(app =>
            {
                app.UseHangfireDashboard(SchedulerSettings.DashboardPath, new DashboardOptions
                {
                    DashboardTitle = "Lunaris Jobs",
                    DarkModeEnabled = true,
                    Authorization = [new Hangfire.Dashboard.LocalRequestsOnlyAuthorizationFilter()]
                });
            });
        });
    }
}
