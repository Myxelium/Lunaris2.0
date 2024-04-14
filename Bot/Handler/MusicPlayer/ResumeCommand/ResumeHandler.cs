using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.Players;
using MediatR;

namespace Lunaris2.Handler.MusicPlayer.ResumeCommand;

public record ResumeCommand(SocketSlashCommand Message) : IRequest;

public class ResumeHandler(DiscordSocketClient client, IAudioService audioService) : IRequestHandler<ResumeCommand>
{
    public async Task Handle(ResumeCommand command, CancellationToken cancellationToken)
    {
        var context = command.Message;
        var player = await audioService.GetPlayerAsync(client, context, connectToVoiceChannel: true);

        if (player is null)
            return;

        if (player.State is not PlayerState.Paused)
        {
            await context.SendMessageAsync("Player is not paused.", client);
            return;
        }

        await player.ResumeAsync(cancellationToken);
        await context.SendMessageAsync("Resumed.", client);
    }
}