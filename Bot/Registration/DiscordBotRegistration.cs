using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Lunaris2.Notification;
using Lunaris2.Service;
using Lunaris2.SlashCommand;

namespace Lunaris2.Registration;

public static class DiscordBotRegistration
{
    public static IServiceCollection AddDiscordBot(this IServiceCollection services, IConfiguration configuration)
    {
        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.All
        };

        var client = new DiscordSocketClient(config);

        services
            .AddMediatR(mediatRServiceConfiguration =>
                mediatRServiceConfiguration.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()))
            .AddMusicPlayer(configuration)
            .AddSingleton(client)
            .AddSingleton<DiscordEventListener>()
            .AddSingleton(service => new InteractionService(service.GetRequiredService<DiscordSocketClient>()))
            .AddChat(configuration);

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

        return services;
    }

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