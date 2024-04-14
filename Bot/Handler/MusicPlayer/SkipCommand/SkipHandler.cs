using Discord.WebSocket;
using MediatR;
using Victoria.Node;
using Victoria.Player;

namespace Lunaris2.Handler.MusicPlayer.SkipCommand;

public record SkipCommand(SocketSlashCommand Message) : IRequest;

public class SkipHandler : IRequestHandler<SkipCommand>
{
    private readonly LavaNode _lavaNode;
    private readonly DiscordSocketClient _client;
    private readonly MusicEmbed _musicEmbed;

    public SkipHandler(LavaNode lavaNode, DiscordSocketClient client, MusicEmbed musicEmbed)
    {
        _lavaNode = lavaNode;
        _client = client;
        _musicEmbed = musicEmbed;
    }

    public async Task Handle(SkipCommand message, CancellationToken cancellationToken)
    {
        var context = message.Message;
        
        await _lavaNode.EnsureConnected();
        
        if (!_lavaNode.TryGetPlayer(context.GetGuild(_client), out var player)) {
            await context.RespondAsync("I'm not connected to a voice channel.");
            return;
        }

        if (player.PlayerState != PlayerState.Playing) {
            await context.RespondAsync("Woaaah there, I can't skip when nothing is playing.");
            return;
        }
        
        try {
            await player.SkipAsync();
            await _musicEmbed.NowPlayingEmbed(player, context, _client);
        }
        catch (Exception exception) {
            await context.RespondAsync("There is not more tracks to skip.");
            Console.WriteLine(exception);
        }
    }
}