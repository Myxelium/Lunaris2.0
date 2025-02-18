using Discord;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Microsoft.Extensions.Options;

namespace Lunaris2.Handler.MusicPlayer;

public static class Extensions
{
    public static async ValueTask<QueuedLavalinkPlayer?> GetPlayerAsync(
        this IAudioService audioService, 
        DiscordSocketClient client, 
        SocketSlashCommand context, 
        bool connectToVoiceChannel = true)
    {
        ArgumentNullException.ThrowIfNull(context);

        var retrieveOptions = new PlayerRetrieveOptions(
            ChannelBehavior: connectToVoiceChannel ? PlayerChannelBehavior.Join : PlayerChannelBehavior.None);

        var playerOptions = new QueuedLavalinkPlayerOptions { HistoryCapacity = 10000 };

        var result = await audioService.Players
            .RetrieveAsync(context.GetGuild(client).Id, context.GetGuild(client).GetUser(context.User.Id).VoiceChannel.Id, playerFactory: PlayerFactory.Queued, Options.Create(playerOptions), retrieveOptions)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            var errorMessage = result.Status switch
            {
                PlayerRetrieveStatus.UserNotInVoiceChannel => "You are not connected to a voice channel.",
                PlayerRetrieveStatus.BotNotConnected => "The bot is currently not connected.",
                _ => "Unknown error.",
            };

            return null;
        }
        
        return result.Player;
    }
    
    public static SocketGuild GetGuild(this SocketSlashCommand message, DiscordSocketClient client)
    {
        if (message.GuildId == null)
        {
            throw new Exception("Guild ID is null!");
        }
            
        return client.GetGuild(message.GuildId.Value);
    }
        
    public static IVoiceState GetVoiceState(this SocketSlashCommand message)
    {
        var voiceState = message.User as IVoiceState;
            
        if (voiceState?.VoiceChannel == null)
        {
            throw new Exception("You must be connected to a voice channel!");
        }

        return voiceState;
    }
        
    public static async Task RespondAsync(this SocketSlashCommand message, string content)
    {
        await message.RespondAsync(content, ephemeral: true);
    }
    
    public static T? GetOptionValueByName<T>(this SocketSlashCommand command, string optionName)
    {
        return (T?)(command.Data?.Options?
            .FirstOrDefault(option => option.Name == optionName)?.Value ?? default(T));
    }
}