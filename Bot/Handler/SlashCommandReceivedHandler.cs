using Lunaris2.Handler.MusicPlayer.ClearQueueCommand;
using Lunaris2.Handler.MusicPlayer.DisconnectCommand;
using Lunaris2.Handler.MusicPlayer.PauseCommand;
using Lunaris2.Handler.MusicPlayer.PlayCommand;
using Lunaris2.Handler.MusicPlayer.ResumeCommand;
using Lunaris2.Handler.MusicPlayer.SkipCommand;
using Lunaris2.Notification;
using Lunaris2.SlashCommand;
using MediatR;

namespace Lunaris2.Handler;

public class SlashCommandReceivedHandler(ISender mediator) : INotificationHandler<SlashCommandReceivedNotification>
{
    public async Task Handle(SlashCommandReceivedNotification notification, CancellationToken cancellationToken)
    {
        await notification.Message.DeferAsync();
        
        switch (notification.Message.CommandName)
        {
            case Command.Resume.Name:
                await mediator.Send(new ResumeCommand(notification.Message), cancellationToken);
                break;
            case Command.Pause.Name:
                await mediator.Send(new PauseCommand(notification.Message), cancellationToken);
                break;
            case Command.Disconnect.Name:
                await mediator.Send(new DisconnectCommand(notification.Message), cancellationToken);
                break;
            case Command.Play.Name:
                await mediator.Send(new PlayCommand(notification.Message), cancellationToken);
                break;
            case Command.Skip.Name:
                await mediator.Send(new SkipCommand(notification.Message), cancellationToken);
                break;
            case Command.Clear.Name:
                await mediator.Send(new ClearQueueCommand(notification.Message), cancellationToken);
                break;
        }
    }
}