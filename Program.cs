using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Lunaris2.Notification;
using Lunaris2.SlashCommand;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lunaris2;

public class Program
{
    private DiscordSocketClient? _client;
    private CommandService? _commands;
    private IServiceProvider? _services;
    private IConfiguration? _config;

    private static void Main(string[] args) => new Program()
        .RunBotAsync()
        .GetAwaiter()
        .GetResult();

    private async Task RunBotAsync()
    {
        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.All
        };
        
        _client = new DiscordSocketClient(config);
        _client.Ready += Client_Ready;
        _commands = new CommandService();
        _services = new ServiceCollection()
            .AddSingleton(_client)
            .AddSingleton(_commands)
            .AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()))
            .AddSingleton<DiscordEventListener>()
            .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
            .BuildServiceProvider();
        
        _client.Log += Log;
        _config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();
        
        await _client.LoginAsync(TokenType.Bot, _config["Token"]);
        await _client.StartAsync();

        var listener = _services.GetRequiredService<DiscordEventListener>();
        await listener.StartAsync();
        
        await Task.Delay(Timeout.Infinite);
    }

    private async Task Client_Ready()
    {
        _client.RegisterCommands();
    }
    
    private Task Log(LogMessage arg)
    {
        Console.WriteLine(arg);
        return Task.CompletedTask;
    }
}