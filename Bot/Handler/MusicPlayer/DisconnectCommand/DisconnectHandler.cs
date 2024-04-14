using Discord.WebSocket;
using Lavalink4NET;
using MediatR;

namespace Lunaris2.Handler.MusicPlayer.DisconnectCommand;

public record DisconnectCommand(SocketSlashCommand Message) : IRequest;

public class DisconnectHandler(DiscordSocketClient client, IAudioService audioService) : IRequestHandler<DisconnectCommand>
{
    public async Task Handle(DisconnectCommand command, CancellationToken cancellationToken)
    {
        var context = command.Message;
        var player = await audioService.GetPlayerAsync(client, context, connectToVoiceChannel: true);

        if (player is null)
            return;

        await player.DisconnectAsync(cancellationToken).ConfigureAwait(false);
        await context.RespondAsync("Disconnected.").ConfigureAwait(false);
    }
}