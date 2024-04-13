﻿using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Lunaris2.Notification;
using Lunaris2.SlashCommand;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lunaris2
{
    public class Program
    {
        public static void Main(string[] args)
        {
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
                    var commands = new CommandService();
                    var configuration = new ConfigurationBuilder()
                        .SetBasePath(AppContext.BaseDirectory)
                        .AddJsonFile("appsettings.json")
                        .Build();
                    
                    services.AddSingleton(client)
                        .AddSingleton(commands)
                        .AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()))
                        .AddSingleton<DiscordEventListener>()
                        .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()));

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
}