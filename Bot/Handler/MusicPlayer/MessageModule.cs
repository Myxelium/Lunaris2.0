using Discord;
using Discord.WebSocket;

namespace Lunaris2.Handler.MusicPlayer;

public static class MessageModule
{
    private static readonly Dictionary<ulong, List<ulong>> GuildMessageIds = new();

    public static async Task SendMessageAsync(this SocketSlashCommand context, string message, DiscordSocketClient client)
    {
        try
        {
            var guildId = await StoreForRemoval(context, client);

            var sentMessage = await context.FollowupAsync(message);
            GuildMessageIds[guildId].Add(sentMessage.Id);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public static async Task SendMessageAsync(this SocketSlashCommand context, Embed message, DiscordSocketClient client)
    {
        try
        {
            var guildId = await StoreForRemoval(context, client);

            var sentMessage = await context.FollowupAsync(embed: message);
            GuildMessageIds[guildId].Add(sentMessage.Id);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private static async Task<ulong> StoreForRemoval(SocketSlashCommand context, DiscordSocketClient client)
    {
        var guildId = context.GetGuild(client).Id;

        if (GuildMessageIds.TryGetValue(guildId, out var value))
        {
            if (value.Count <= 0) 
                return guildId;
            
            foreach (var messageId in value)
            {
                var messageToDelete = await context.Channel.GetMessageAsync(messageId);
                if (messageToDelete != null)
                    await messageToDelete.DeleteAsync();
            }

            value.Clear();
        }
        else
        {
            GuildMessageIds.Add(guildId, new List<ulong>());
        }

        return guildId;
    }
}