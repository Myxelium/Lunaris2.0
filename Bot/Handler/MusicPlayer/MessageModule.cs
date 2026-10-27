using System.Net;
using Discord;
using Discord.Net;
using Discord.WebSocket;

namespace Lunaris2.Handler.MusicPlayer;

public static class MessageModule
{
    private static readonly object Gate = new();
    private static readonly Dictionary<ulong, List<ulong>> GuildMessageIds = new();

    public static async Task SendMessageAsync(this SocketSlashCommand context, string message, DiscordSocketClient client)
    {
        try
        {
            var guildId = context.GetGuild(client).Id;
            await DeleteTrackedAsync(context, guildId);

            var sentMessage = await context.FollowupAsync(message);
            Track(guildId, sentMessage.Id);
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
            var guildId = context.GetGuild(client).Id;
            await DeleteTrackedAsync(context, guildId);

            var sentMessage = await context.FollowupAsync(embed: message);
            Track(guildId, sentMessage.Id);
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
        await DeleteTrackedAsync(context, guildId);
    }

    private static void Track(ulong guildId, ulong messageId)
    {
        lock (Gate)
        {
            if (!GuildMessageIds.TryGetValue(guildId, out var messages))
            {
                messages = [];
                GuildMessageIds[guildId] = messages;
            }

            messages.Add(messageId);
        }
    }

    private static async Task DeleteTrackedAsync(SocketSlashCommand context, ulong guildId)
    {
        List<ulong> messagesToDelete;
        lock (Gate)
        {
            if (!GuildMessageIds.TryGetValue(guildId, out var messages) || messages.Count == 0)
            {
                GuildMessageIds.TryAdd(guildId, []);
                return;
            }

            messagesToDelete = messages.ToList();
            messages.Clear();
        }

        foreach (var messageId in messagesToDelete)
        {
            try
            {
                var messageToDelete = await context.Channel.GetMessageAsync(messageId);
                if (messageToDelete != null)
                    await messageToDelete.DeleteAsync();
            }
            catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.NotFound)
            {
            }
        }
    }
}
