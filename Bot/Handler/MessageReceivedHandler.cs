using System.Text.RegularExpressions;
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
    }

    private async Task BotMentioned(MessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Message.MentionedUsers.Any(user => user.Id == _client.CurrentUser.Id))
        {
            // The bot was mentioned
            const string pattern = "<.*?>";
            const string replacement = "";
            var regex = new Regex(pattern);
            var messageContent = regex.Replace(notification.Message.Content, replacement);
            
            await _mediatir.Send(new ChatCommand.ChatCommand(notification.Message, messageContent), cancellationToken);
        }
    }
}