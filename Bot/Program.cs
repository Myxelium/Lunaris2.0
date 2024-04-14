using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Lunaris2.Notification;
using Lunaris2.SlashCommand;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Victoria;
using Victoria.Node;
using RunMode = Discord.Commands.RunMode;

namespace Lunaris2
{
    public class Program
    {
        public static LavaNode _lavaNode;
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

                    var commandServiceConfig = new CommandServiceConfig{ DefaultRunMode = RunMode.Async };

                    var client = new DiscordSocketClient(config);
                    var commands = new CommandService(commandServiceConfig);
                    var configuration = new ConfigurationBuilder()
                        .SetBasePath(AppContext.BaseDirectory)
                        .AddJsonFile("appsettings.json")
                        .Build();

                    services.AddSingleton(client)
                        .AddSingleton(commands)
                        .AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()))
                        .AddSingleton<DiscordEventListener>()
                        .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                        .AddLavaNode(x =>
                        {
                            x.SelfDeaf = false;
                            x.Hostname = "lavalink.devamop.in";
                            x.Port = 80;
                            x.Authorization = "DevamOP";
                        })
                        // .AddLavaNode(x =>
                        // {
                        //     x.SelfDeaf = false;
                        //     x.Hostname = "localhost";
                        //     x.Port = 2333;
                        // })
                        .AddSingleton<LavaNode>();

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
                    
                    _lavaNode = services.BuildServiceProvider().GetRequiredService<LavaNode>();

                    var listener = services
                        .BuildServiceProvider()
                        .GetRequiredService<DiscordEventListener>();
                    
                    listener
                        .StartAsync()
                        .GetAwaiter()
                        .GetResult();
                });

        private static async Task Client_Ready(DiscordSocketClient client)
        {
            await _lavaNode.ConnectAsync();
            client.RegisterCommands();
        }
        
        private static Task Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }
    }
}
