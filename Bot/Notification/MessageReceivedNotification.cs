using Discord.WebSocket;
using MediatR;

namespace Lunaris2.Notification;

public class MessageReceivedNotification(SocketMessage message) : INotification
{
    public SocketMessage Message { get; } = message ?? throw new ArgumentNullException(nameof(message));
}