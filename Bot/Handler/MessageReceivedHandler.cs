using Lunaris2.Handler.GoodByeCommand;
using Lunaris2.Notification;
using Lunaris2.SlashCommand;
using MediatR;

namespace Lunaris2.Handler;

public class MessageReceivedHandler(ISender mediator) : INotificationHandler<MessageReceivedNotification>
{
    public async Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        switch (notification.Message.CommandName)
        {
            case Command.Hello.Name:
                await mediator.Send(new HelloCommand.HelloCommand(notification.Message), cancellationToken);
                break;
            case Command.Goodbye.Name:
                await mediator.Send(new GoodbyeCommand(notification.Message), cancellationToken);
                break;
            default:
                break;
        }
    }
}