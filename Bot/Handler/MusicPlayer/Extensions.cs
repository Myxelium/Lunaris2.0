using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Microsoft.Extensions.Options;

namespace Lunaris2.Handler.MusicPlayer;

public readonly record struct PlayerRetrieval(QueuedLavalinkPlayer? Player, string? Error)
{
    public static PlayerRetrieval Ok(QueuedLavalinkPlayer player) => new(player, null);

    public static PlayerRetrieval Fail(string error) => new(null, error);
}

public static class Extensions
{
    public static async ValueTask<PlayerRetrieval> GetPlayerAsync(
        this IAudioService audioService,
        DiscordSocketClient client,
        SocketSlashCommand context,
        PlayerChannelBehavior channelBehavior = PlayerChannelBehavior.Join)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.GuildId is not ulong guildId)
            return PlayerRetrieval.Fail("This command only works in a server.");

        var guild = client.GetGuild(guildId);
        if (guild is null)
            return PlayerRetrieval.Fail("I can't see that server.");

        var voiceChannelId = guild.GetUser(context.User.Id)?.VoiceChannel?.Id;
        var retrieveOptions = new PlayerRetrieveOptions(ChannelBehavior: channelBehavior);
        var playerOptions = new QueuedLavalinkPlayerOptions { HistoryCapacity = 10000 };

        // Lavalink4NET.Discord.NET 4.2.2 swaps selfDeaf and selfMute on the gateway call.
        // Leaving both false keeps playback audible.
        var result = await audioService.Players
            .RetrieveAsync(
                guildId,
                voiceChannelId,
                playerFactory: PlayerFactory.Queued,
                Options.Create(playerOptions),
                retrieveOptions)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            var errorMessage = result.Status switch
            {
                PlayerRetrieveStatus.UserNotInVoiceChannel => "You are not connected to a voice channel.",
                PlayerRetrieveStatus.BotNotConnected => "The bot is currently not connected.",
                PlayerRetrieveStatus.VoiceChannelMismatch => "You must be in the same voice channel as the bot.",
                _ => "Couldn't connect to the voice channel."
            };

            return PlayerRetrieval.Fail(errorMessage);
        }

        return PlayerRetrieval.Ok(result.Player);
    }

    public static async Task<QueuedLavalinkPlayer?> GetPlayerOrNotifyAsync(
        this IAudioService audioService,
        DiscordSocketClient client,
        SocketSlashCommand context,
        PlayerChannelBehavior channelBehavior)
    {
        var retrieval = await audioService.GetPlayerAsync(client, context, channelBehavior);
        if (retrieval.Player is not null)
            return retrieval.Player;

        await context.SendMessageAsync(retrieval.Error ?? "Couldn't connect to the voice channel.", client);
        return null;
    }

    public static SocketGuild GetGuild(this SocketSlashCommand message, DiscordSocketClient client)
    {
        if (message.GuildId == null)
            throw new Exception("Guild ID is null!");

        return client.GetGuild(message.GuildId.Value);
    }

    public static async Task RespondAsync(this SocketSlashCommand message, string content)
    {
        await message.RespondAsync(content, ephemeral: true);
    }

    public static string GetOptionValueByName(this SocketSlashCommand command, string optionName)
    {
        return command.Data.Options.FirstOrDefault(option => option.Name == optionName)?.Value?.ToString() ?? string.Empty;
    }
}
