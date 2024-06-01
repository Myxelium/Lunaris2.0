using Discord.WebSocket;
using MediatR;

namespace Lunaris2.Notification;

public class SlashCommandReceivedNotification(SocketSlashCommand message) : INotification
{
    public SocketSlashCommand Message { get; } = message ?? throw new ArgumentNullException(nameof(message));
}