using Discord.WebSocket;
using Lavalink4NET;
using MediatR;

namespace Lunaris2.Handler.MusicPlayer.ClearQueueCommand;

public record ClearQueueCommand(SocketSlashCommand Message) : IRequest;

public class DisconnectHandler(DiscordSocketClient client, IAudioService audioService) : IRequestHandler<ClearQueueCommand>
{
    public async Task Handle(ClearQueueCommand command, CancellationToken cancellationToken)
    {
        var context = command.Message;
        var player = await audioService.GetPlayerAsync(client, context, connectToVoiceChannel: true);

        if (player is null)
            return;

        await player.Queue.ClearAsync(cancellationToken).ConfigureAwait(false);
        await context.SendMessageAsync("Cleared queue. No songs are queued.", client).ConfigureAwait(false);
    }
}