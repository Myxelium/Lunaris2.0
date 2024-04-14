using Discord.WebSocket;
using Lavalink4NET;
using MediatR;

namespace Lunaris2.Handler.MusicPlayer.SkipCommand;

public record SkipCommand(SocketSlashCommand Message) : IRequest;

public class SkipHandler(DiscordSocketClient client, IAudioService audioService) : IRequestHandler<SkipCommand>
{
    public async Task Handle(SkipCommand command, CancellationToken cancellationToken)
    {
        var context = command.Message;
        var player = await audioService.GetPlayerAsync(client, context, connectToVoiceChannel: true);

        if (player is null)
            return;

        if (player.CurrentItem is null)
        {
            await context.SendMessageAsync("Nothing playing!", client).ConfigureAwait(false);
            return;
        }

        await player.SkipAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        var track = player.CurrentItem;

        if (track is not null)
            await context.SendMessageAsync($"Skipped. Now playing: {track.Track!.Title}", client).ConfigureAwait(false);
        else
            await context.SendMessageAsync("Skipped. Stopped playing because the queue is now empty.", client).ConfigureAwait(false);
    }
}