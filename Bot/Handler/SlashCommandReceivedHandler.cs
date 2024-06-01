using Lunaris2.Handler.GoodByeCommand;
using Lunaris2.Handler.MusicPlayer.JoinCommand;
using Lunaris2.Handler.MusicPlayer.PlayCommand;
using Lunaris2.Handler.MusicPlayer.SkipCommand;
using Lunaris2.Notification;
using Lunaris2.SlashCommand;
using MediatR;

namespace Lunaris2.Handler;

public class SlashCommandReceivedHandler(ISender mediator) : INotificationHandler<SlashCommandReceivedNotification>
{
    public async Task Handle(SlashCommandReceivedNotification notification, CancellationToken cancellationToken)
    {
        switch (notification.Message.CommandName)
        {
            case Command.Hello.Name:
                await mediator.Send(new HelloCommand.HelloCommand(notification.Message), cancellationToken);
                break;
            case Command.Goodbye.Name:
                await mediator.Send(new GoodbyeCommand(notification.Message), cancellationToken);
                break;
            case Command.Join.Name:
                await mediator.Send(new JoinCommand(notification.Message), cancellationToken);
                break;
            case Command.Play.Name:
                await mediator.Send(new PlayCommand(notification.Message), cancellationToken);
                break;
            case Command.Skip.Name:
                await mediator.Send(new SkipCommand(notification.Message), cancellationToken);
                break;
        }
    }
}