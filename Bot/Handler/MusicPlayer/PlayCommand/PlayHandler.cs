using Discord;
using Discord.Commands;
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
    private readonly MusicEmbed _musicEmbed;
    private readonly LavaNode _lavaNode;
    private readonly DiscordSocketClient _client;
    private SocketSlashCommand _context;
    
    public PlayHandler(
        LavaNode lavaNode, 
        DiscordSocketClient client, 
        MusicEmbed musicEmbed)
    {
        _lavaNode = lavaNode;
        _client = client;
        _musicEmbed = musicEmbed;
    }
    
    [Command(RunMode = RunMode.Async)]
    public async Task Handle(PlayCommand command, CancellationToken cancellationToken)
    {
        _context = command.Message;

        await _lavaNode.EnsureConnected();

        var songName = _context.GetOptionValueByName(Option.Input);

        if (string.IsNullOrWhiteSpace(songName)) {
            await _context.RespondAsync("Please provide search terms.");
            return;
        }

        var player = await GetPlayer();
        
        if (player == null) 
            return;

        var searchResponse = await _lavaNode.SearchAsync(
            Uri.IsWellFormedUriString(songName, UriKind.Absolute)
                ? SearchType.Direct
                : SearchType.YouTube, songName);
        
        if (!await SearchResponse(searchResponse, player, songName)) 
            return;

        await PlayTrack(player);
        
        await _musicEmbed.NowPlayingEmbed(player, _context, _client);

        _lavaNode.OnTrackEnd += OnTrackEnd;
    }

    private async Task OnTrackEnd(TrackEndEventArg<LavaPlayer<LavaTrack>, LavaTrack> arg)
    {
        var player = arg.Player;
        if (!player.Vueue.TryDequeue(out var nextTrack))
            return;

        await player.PlayAsync(nextTrack);
        
        await _musicEmbed.NowPlayingEmbed(player, _context, _client);
    }
    
    private static async Task PlayTrack(LavaPlayer<LavaTrack> player)
    {
        if (player.PlayerState is PlayerState.Playing or PlayerState.Paused) {
            return;
        }

        player.Vueue.TryDequeue(out var lavaTrack);
        await player.PlayAsync(lavaTrack);
    }
    
    private async Task<LavaPlayer<LavaTrack>?> GetPlayer()
    {
        var voiceState = _context.User as IVoiceState;

        if (voiceState?.VoiceChannel != null)
            return await _lavaNode.JoinAsync(voiceState.VoiceChannel, _context.Channel as ITextChannel);
        
        await _context.RespondAsync("You must be connected to a voice channel!");
        return null;
    }
    
    private async Task<bool> SearchResponse(
        SearchResponse searchResponse, LavaPlayer<LavaTrack> player, 
        string songName)
    {
        if (searchResponse.Status is SearchStatus.LoadFailed or SearchStatus.NoMatches) {
            await _context.RespondAsync($"I wasn't able to find anything for `{songName}`.");
            return false;
        }

        if (!string.IsNullOrWhiteSpace(searchResponse.Playlist.Name)) {
            player.Vueue.Enqueue(searchResponse.Tracks);
            
            await _context.RespondAsync($"Enqueued {searchResponse.Tracks.Count} songs.");
        }
        else {
            var track = searchResponse.Tracks.FirstOrDefault()!;
            player.Vueue.Enqueue(track);
        }

        return true;
    }
}