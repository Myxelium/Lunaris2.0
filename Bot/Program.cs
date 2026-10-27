using System.Reflection;
using Discord;
using Discord.WebSocket;
using Lunaris2.Handler.ChatCommand;
using Lavalink4NET.Extensions;
using Lavalink4NET.Integrations.SponsorBlock.Extensions;
using Lunaris2.Handler.MusicPlayer;
using Lunaris2.Notification;
using Lunaris2.Service;
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

        var app = CreateHostBuilder(args).Build();
        app.UseSponsorBlock();
        app.Run();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((_, services) =>
            {
                var socketConfig = new DiscordSocketConfig
                {
                    GatewayIntents = GatewayIntents.All,
                    LogLevel = LogSeverity.Info
                };

                var client = new DiscordSocketClient(socketConfig);
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false)
                    .Build();

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
                    .Configure<ChatSettings>(configuration.GetSection("LLM"));

                client.Log += message =>
                {
                    Console.WriteLine(message);
                    return Task.CompletedTask;
                };
            });
}
