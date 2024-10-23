using System.Net;
using Discord;
using Discord.Net;
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
    
    public static async Task RemoveMessages(this SocketSlashCommand context, DiscordSocketClient client)
    {
        var guildId = context.GetGuild(client).Id;

        if (GuildMessageIds.TryGetValue(guildId, out var value))
        {
            if (value.Count <= 0) 
                return;
            
            foreach (var messageId in value)
            {
                var messageToDelete = await context.Channel.GetMessageAsync(messageId);
                if (messageToDelete != null)
                    await messageToDelete.DeleteAsync();
            }

            value.Clear();
        }
    }

    private static async Task<ulong> StoreForRemoval(SocketSlashCommand context, DiscordSocketClient client)
    {
        var guildId = context.GetGuild(client).Id;

        if (GuildMessageIds.TryGetValue(guildId, out var value))
        {
            if (value.Count <= 0)
                return guildId;

            // Create a copy of the list to avoid modifying it during iteration
            var messagesToDelete = new List<ulong>(value);

            foreach (var messageId in messagesToDelete)
            {
                try
                {
                    var messageToDelete = await context.Channel.GetMessageAsync(messageId);
                    if (messageToDelete != null)
                    {
                        await messageToDelete.DeleteAsync();
                    }
                }
                catch (HttpException ex)
                {
                    if (ex.HttpCode != HttpStatusCode.NotFound)
                        throw;
                }
            }

            // Clear the list after we're done with the iteration
            value.Clear();
        }
        else
        {
            // If the guildId does not exist, add it to the dictionary
            GuildMessageIds.Add(guildId, new List<ulong>());
        }

        return guildId;
    }
}