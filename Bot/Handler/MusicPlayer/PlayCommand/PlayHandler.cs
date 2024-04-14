using Discord;
using Discord.WebSocket;
using Lunaris2.SlashCommand;
using MediatR;
using Victoria.Node;
using Victoria.Node.EventArgs;
using Victoria.Player;
using Victoria.Responses.Search;

namespace Lunaris2.Handler.MusicPlayer.PlayCommand;

public record PlayCommand(SocketSlashCommand Message) : IRequest;

public class PlayHandler : IRequestHandler<PlayCommand>
{
    private readonly LavaNode _lavaNode;
    private readonly DiscordSocketClient _client;
    
    public PlayHandler(LavaNode lavaNode, DiscordSocketClient client)
    {
        _lavaNode = lavaNode;
        _client = client;
    }
    
    public async Task Handle(PlayCommand command, CancellationToken cancellationToken)
    {
        var context = command.Message;

        await _lavaNode.EnsureConnected();

        var songName = context.GetOptionValueByName(Option.Input);

        if (string.IsNullOrWhiteSpace(songName)) {
            await context.RespondAsync("Please provide search terms.");
            return;
        }

        var player = await GetPlayer(context);
        
        if (player == null) 
            return;

        var searchResponse = await _lavaNode.SearchAsync(
            Uri.IsWellFormedUriString(songName, UriKind.Absolute)
                ? SearchType.Direct
                : SearchType.YouTube, songName);
        
        if (!await HandleSearchResponse(searchResponse, player, context, songName)) 
            return;

        await PlayTrack(player);

        _lavaNode.OnTrackEnd += OnTrackEnd;
    }

    private async Task OnTrackEnd(TrackEndEventArg<LavaPlayer<LavaTrack>, LavaTrack> arg)
    {
        var player = arg.Player;
        if (!player.Vueue.TryDequeue(out var nextTrack))
        {
            await player.TextChannel.SendMessageAsync("Queue completed!");
            return;
        }

        await player.PlayAsync(nextTrack);
    }
    
    private async Task PlayTrack(LavaPlayer<LavaTrack> player)
    {
        if (player.PlayerState is PlayerState.Playing or PlayerState.Paused) {
            return;
        }

        player.Vueue.TryDequeue(out var lavaTrack);
        await player.PlayAsync(lavaTrack);
    }
    
    private async Task<LavaPlayer<LavaTrack>?> GetPlayer(SocketSlashCommand context)
    {
        var voiceState = context.User as IVoiceState;

        if (voiceState?.VoiceChannel != null)
            return await _lavaNode.JoinAsync(voiceState.VoiceChannel, context.Channel as ITextChannel);
        
        await context.RespondAsync("You must be connected to a voice channel!");
        return null;
    }
    
    private async Task<bool> HandleSearchResponse(SearchResponse searchResponse, LavaPlayer<LavaTrack> player, SocketSlashCommand context, string songName)
    {
        if (searchResponse.Status is SearchStatus.LoadFailed or SearchStatus.NoMatches) {
            await context.RespondAsync($"I wasn't able to find anything for `{songName}`.");
            return false;
        }

        if (!string.IsNullOrWhiteSpace(searchResponse.Playlist.Name)) {
            player.Vueue.Enqueue(searchResponse.Tracks);
            
            await context.RespondAsync($"Enqueued {searchResponse.Tracks.Count} songs.");
        }
        else {
            var track = searchResponse.Tracks.FirstOrDefault()!;
            player.Vueue.Enqueue(track);

            await context.RespondAsync($"Enqueued {track?.Title}");
        }

        return true;
    }
}