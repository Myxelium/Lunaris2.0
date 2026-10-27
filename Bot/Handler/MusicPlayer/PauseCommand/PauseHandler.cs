using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.Players;
using MediatR;

namespace Lunaris2.Handler.MusicPlayer.PauseCommand;

public record PauseCommand(SocketSlashCommand Message) : IRequest;

public class PauseHandler(DiscordSocketClient client, IAudioService audioService) : IRequestHandler<PauseCommand>
{
    public async Task Handle(PauseCommand command, CancellationToken cancellationToken)
    {
        var context = command.Message;
        var player = await audioService.GetPlayerOrNotifyAsync(client, context, PlayerChannelBehavior.None);

        if (player is null)
            return;

        if (player.State is not PlayerState.Playing)
        {
            var message = player.State is PlayerState.Paused ? "Player is already paused." : "Nothing playing!";
            await context.SendMessageAsync(message, client);
            return;
        }

        await player.PauseAsync(cancellationToken);
        await context.SendMessageAsync("Paused.", client);
    }
}