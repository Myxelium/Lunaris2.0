using Discord;
using Discord.WebSocket;
using Lunaris2.Handler.MusicPlayer;
using Lunaris2.Notification;
using Lunaris2.SlashCommand;
using Microsoft.Extensions.Hosting;

namespace Lunaris2;

public sealed class DiscordClientHostedService(
    DiscordSocketClient client,
    DiscordSettings settings,
    DiscordEventListener listener,
    PlaybackNotifier playbackNotifier) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Resolving the notifier subscribes to track events before the gateway connects.
        _ = playbackNotifier;

        if (string.IsNullOrWhiteSpace(settings.Token))
            throw new InvalidOperationException("Discord bot token is missing from appsettings.json.");

        client.Ready += OnReady;
        await listener.StartAsync();
        await client.LoginAsync(TokenType.Bot, settings.Token);
        await client.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        client.Ready -= OnReady;
        if (client.LoginState == LoginState.LoggedOut)
            return;

        await client.LogoutAsync();
        await client.StopAsync();
    }

    private Task OnReady()
    {
        client.RegisterCommands();
        return Task.CompletedTask;
    }
}
