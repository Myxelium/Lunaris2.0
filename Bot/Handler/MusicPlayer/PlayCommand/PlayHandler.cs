using Discord.WebSocket;
using Lunaris2.SlashCommand;
using MediatR;
using Lavalink4NET;
using Lavalink4NET.Events.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Rest.Entities.Tracks;
using System.Threading;

namespace Lunaris2.Handler.MusicPlayer.PlayCommand;

public record PlayCommand(SocketSlashCommand Message) : IRequest;

public class PlayHandler : IRequestHandler<PlayCommand>
{
    private readonly MusicEmbed _musicEmbed;
    private readonly DiscordSocketClient _client;
    private readonly IAudioService _audioService;
    private SocketSlashCommand _context;
    
    public PlayHandler(
        DiscordSocketClient client, 
        MusicEmbed musicEmbed, 
        IAudioService audioService)
    {
        _client = client;
        _musicEmbed = musicEmbed;
        _audioService = audioService;
        _audioService.TrackStarted += OnTrackStarted;
    }

    private async Task OnTrackStarted(object sender, TrackStartedEventArgs eventargs)
    {
        var player = sender as QueuedLavalinkPlayer;
        var track = player?.CurrentTrack;

        if (track != null) 
            await _musicEmbed.NowPlayingEmbed(track, _context, _client);
    }

    public Task Handle(PlayCommand command, CancellationToken cancellationToken)
    {
        new Thread(PlayMusic).Start();
        return Task.CompletedTask;

        async void PlayMusic()
        {
            var context = command.Message;
            _context = context;
            
            if ((context.User as SocketGuildUser)?.VoiceChannel == null)
            {
                await context.SendMessageAsync("You must be in a voice channel to use this command.", _client);
                return;
            }
            
            await _audioService.StartAsync(cancellationToken);

            var searchQuery = context.GetOptionValueByName(Option.Input);

            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                await context.SendMessageAsync("Please provide search terms.", _client);
                return;
            }
            
            var player = await _audioService.GetPlayerAsync(_client, context, connectToVoiceChannel: true);

            if (player is null) return;

            var trackLoadOptions = new TrackLoadOptions { SearchMode = TrackSearchMode.YouTube, };

            var track = await _audioService.Tracks.LoadTrackAsync(searchQuery, trackLoadOptions, cancellationToken: cancellationToken);

            if (track is null) await context.SendMessageAsync("😖 No results.", _client);

            if (player.CurrentTrack is null)
            {
                await player.PlayAsync(track, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                await _musicEmbed.NowPlayingEmbed(track, context, _client);
            }
            else
            {
                if (track != null)
                {
                    var queueTracks = new[] { new TrackQueueItem(track) };
                    await player.Queue.AddRangeAsync(queueTracks, cancellationToken);
                    await context.SendMessageAsync($"🔈 Added to queue: {track.Title}", _client);
                }
                else
                {
                    await context.SendMessageAsync($"Couldn't read song information", _client);
                }
            }
        }
    }
}