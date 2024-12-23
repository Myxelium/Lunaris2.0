﻿using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Lunaris2.Handler.ChatCommand;
using Lavalink4NET.Extensions;
using Lavalink4NET.Integrations.SponsorBlock.Extensions;
using Lunaris2.Handler.MusicPlayer;
using Lunaris2.Notification;
using Lunaris2.Service;
using Lunaris2.SlashCommand;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lunaris2;

public class Program
{
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
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
                    .AddSingleton<MusicEmbed>()
                    .AddSingleton<ChatSettings>()
                    .AddSingleton(client)
                    .AddSingleton<DiscordEventListener>()
                    .AddSingleton<VoiceChannelMonitorService>()
                    .AddSingleton(service => new InteractionService(service.GetRequiredService<DiscordSocketClient>()))
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
        
        new VoiceChannelMonitorService(client).StartMonitoring();
        return Task.CompletedTask;
    }
        
    private static Task Log(LogMessage arg)
    {
        Console.WriteLine(arg);
        return Task.CompletedTask;
    }
}