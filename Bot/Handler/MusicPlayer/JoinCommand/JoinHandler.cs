using Discord;
using Discord.WebSocket;
using MediatR;
using Victoria.Node;

namespace Lunaris2.Handler.MusicPlayer.JoinCommand;

public record JoinCommand(SocketSlashCommand Message) : IRequest;

public class JoinHandler : IRequestHandler<JoinCommand>
{
    private readonly LavaNode _lavaNode;
    private readonly DiscordSocketClient _client;
    
    public JoinHandler(LavaNode lavaNode, DiscordSocketClient client)
    {
        _lavaNode = lavaNode;
        _client = client;
    }
    
    public async Task Handle(JoinCommand command, CancellationToken cancellationToken)
    {
        var context = command.Message;
        
        await _lavaNode.EnsureConnected();

        if (_lavaNode.HasPlayer(context.GetGuild(_client))) {
            await context.RespondAsync("I'm already connected to a voice channel!");
            return;
        }
        
        await context.JoinVoiceChannel(_lavaNode);
    }
}