using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Lunaris2.Handler.ChatCommand;
using Lavalink4NET.Extensions;
using Lunaris2.Handler.MusicPlayer;
using Lunaris2.Notification;
using Lunaris2.SlashCommand;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Victoria.Node;

namespace Lunaris2;

public class Program
{
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
        {
            Console.WriteLine(eventArgs.ExceptionObject);
        };
        CreateHostBuilder(args).Build().Run();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((_, services) =>
            {
                var config = new DiscordSocketConfig
                {
                    GatewayIntents = GatewayIntents.All
                };
                
                var client = new DiscordSocketClient(config);
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                services
                    .AddSingleton(client)
                    .AddMediatR(mediatRServiceConfiguration => mediatRServiceConfiguration.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()))
                    .AddSingleton<DiscordEventListener>()
                    .AddSingleton(service => new InteractionService(service.GetRequiredService<DiscordSocketClient>()))
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
                    .AddSingleton<LavaNode>()
                    .AddSingleton<MusicEmbed>()
                    .AddSingleton<ChatSettings>()
                    .Configure<ChatSettings>(configuration.GetSection("LLM"));

                client.Ready += () => Client_Ready(client);
                client.Log += Log;
                
                client
                    .LoginAsync(TokenType.Bot, configuration["Token"])
                    .GetAwaiter()
                    .GetResult();
                    
                client
                    .StartAsync()
                    .GetAwaiter()
                    .GetResult();

                var listener = services
                    .BuildServiceProvider()
                    .GetRequiredService<DiscordEventListener>();
                    
                listener
                    .StartAsync()
                    .GetAwaiter()
                    .GetResult();
            });

    private static Task Client_Ready(DiscordSocketClient client)
    {
        client.RegisterCommands();
        return Task.CompletedTask;
    }
        
    private static Task Log(LogMessage arg)
    {
        Console.WriteLine(arg);
        return Task.CompletedTask;
    }
}