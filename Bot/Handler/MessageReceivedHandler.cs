using System.Text;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using Lunaris2.Notification;
using MediatR;

namespace Lunaris2.Handler;

public class MessageReceivedHandler : INotificationHandler<MessageReceivedNotification>
{
    private readonly DiscordSocketClient _client;
    private readonly ISender _mediatir;

    public MessageReceivedHandler(DiscordSocketClient client, ISender mediatir)
    {
        _client = client;
        _mediatir = mediatir;
    }

    public async Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        await BotMentioned(notification, cancellationToken);
        await Statistics(notification, cancellationToken);
    }
    
    private async Task Statistics(MessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Message.Content.Contains("!LunarisStats"))
        {
            var servers = _client.Guilds.Select(guild => guild.Name);
            var channels = _client.Guilds
                .SelectMany(guild => guild.VoiceChannels)
                .Where(channel => channel.Users.Any(user => user.IsBot));
            
            var table = new StringBuilder();
            var serverColumnWidth = 25;  // Width for server column
            var channelColumnWidth = 25; // Width for channel column
            table.AppendLine($"{"Servers".PadRight(serverColumnWidth - 1)}|{"Channels".PadRight(channelColumnWidth - 1)}");
            table.AppendLine($"{new string('-', serverColumnWidth - 1)}|{new string('-', channelColumnWidth - 1)}");
            foreach (var (server, channel) in servers.Zip(channels))
            {
                table.AppendLine($"{server.PadRight(serverColumnWidth - 1)}|{channel.Name.PadRight(channelColumnWidth - 1)}");
            }
            
            var embed = new EmbedBuilder()
                .WithTitle("Lunaris Statistics")
                .WithDescription(table.ToString())
                .Build();
            
            await notification.Message.Channel.SendMessageAsync(embed: embed);
        }
    }

    private async Task BotMentioned(MessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Message.MentionedUsers.Any(user => user.Id == _client.CurrentUser.Id))
        {
            const string pattern = "<.*?>";
            const string replacement = "";
            var regex = new Regex(pattern);
            var messageContent = regex.Replace(notification.Message.Content, replacement);
            
            await _mediatir.Send(new ChatCommand.ChatCommand(notification.Message, messageContent), cancellationToken);
        }
    }
}