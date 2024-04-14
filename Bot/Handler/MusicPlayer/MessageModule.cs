using Discord;
using Discord.WebSocket;
using Lunaris2.Handler.MusicPlayer;

public static class MessageModule
{
    private static Dictionary<ulong, List<ulong>> guildMessageIds = new Dictionary<ulong, List<ulong>>();

    public static async Task SendMessageAsync(this SocketSlashCommand context, string message, DiscordSocketClient client)
    {
        var guildId = await StoreForRemoval(context, client);
        
        await context.RespondAsync(message);
        var sentMessage = await context.GetOriginalResponseAsync();
        
        guildMessageIds[guildId].Add(sentMessage.Id);
    }
    
    public static async Task SendMessageAsync(this SocketSlashCommand context, Embed message, DiscordSocketClient client)
    {
        var guildId = await StoreForRemoval(context, client);

        await context.RespondAsync(embed: message);
        
        var sentMessage = await context.GetOriginalResponseAsync();

        guildMessageIds[guildId].Add(sentMessage.Id);
    }

    private static async Task<ulong> StoreForRemoval(SocketSlashCommand context, DiscordSocketClient client)
    {
        var guildId = context.GetGuild(client).Id;
        
        if (guildMessageIds.ContainsKey(guildId))
        {
            foreach (var messageId in guildMessageIds[guildId])
            {
                var messageToDelete = await context.Channel.GetMessageAsync(messageId);
                if (messageToDelete != null) 
                    await messageToDelete.DeleteAsync();
            }
            
            guildMessageIds[guildId].Clear();
        }
        else
        {
            guildMessageIds.Add(guildId, []);
        }

        return guildId;
    }
}