using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Victoria.Node;

namespace Lunaris2.Handler.MusicPlayer
{
    public static class Extensions
    {
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
        
        public static async Task EnsureConnected(this LavaNode lavaNode)
        {
            if(!lavaNode.IsConnected)
                await lavaNode.ConnectAsync();
        }
        
        public static async Task JoinVoiceChannel(this SocketSlashCommand context, LavaNode lavaNode)
        {
            try
            {
                var textChannel = context.Channel as ITextChannel;
                await lavaNode.JoinAsync(context.GetVoiceState().VoiceChannel, textChannel);
                await context.RespondAsync($"Joined {context.GetVoiceState().VoiceChannel.Name}!");
            }
            catch (Exception exception) {
                Console.WriteLine(exception);
            }
        }
        
        public static string GetOptionValueByName(this SocketSlashCommand command, string optionName)
        {
            return command.Data.Options.FirstOrDefault(option => option.Name == optionName)?.Value.ToString() ?? string.Empty;
        }
    }
}