using System.Collections.Immutable;
using Discord.WebSocket;
using Lunaris2.SlashCommand;
using MediatR;
using Lavalink4NET;
using Lavalink4NET.Events.Players;
using Lavalink4NET.Integrations.SponsorBlock;
using Lavalink4NET.Integrations.SponsorBlock.Extensions;
using Lavalink4NET.Jellyfin;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Rest.Entities.Tracks;
using Lavalink4NET.Tracks;

namespace Lunaris2.Handler.MusicPlayer.PlayCommand;

public record PlayCommand(SocketSlashCommand Message) : IRequest;

public class PlayHandler : IRequestHandler<PlayCommand>
{
    private readonly MusicEmbed _musicEmbed;
    private readonly DiscordSocketClient _client;
    private readonly IAudioService _audioService;
    private SocketSlashCommand _context = default!; // ensure initialized before use
    private LavalinkTrack? _previousTrack;
    private static readonly HashSet<ulong> SubscribedGuilds = new();
    
    public PlayHandler(
        DiscordSocketClient client, 
        MusicEmbed musicEmbed, 
        IAudioService audioService)
    {
        _client = client;
        _musicEmbed = musicEmbed;
        _audioService = audioService;
    }
    
    private Task OnTrackEnded(object sender, TrackEndedEventArgs eventargs)
    {
        // Reset the previous track when the track ends.
        _previousTrack = null;
        return Task.CompletedTask;
    }
    
    private async Task OnTrackStarted(object sender, TrackStartedEventArgs eventargs)
    {
        var player = eventargs.Player as QueuedLavalinkPlayer;

        if (player?.CurrentTrack is null)
        {
            return; // No track is currently playing.
        }

        var currentTrack = player.CurrentTrack;

        // Check if the current track is the same as the previous one
        if (_previousTrack?.Identifier == currentTrack.Identifier)
        {
            // The same track is playing, so we don't need to create a new embed.
            return;
        }

        // Track has changed, update the previous track and send the embed
        _previousTrack = currentTrack;
        if (_context != null)
        {
            await _musicEmbed.NowPlayingEmbed(currentTrack, _context, _client);
        }
    }
    
    public async Task Handle(PlayCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var context = command.Message;
            _context = context;

            if ((context.User as SocketGuildUser)?.VoiceChannel == null)
            {
                await context.SendMessageAsync("You must be in a voice channel to use this command.", _client);
                return;
            }

            var searchQuery = context.GetOptionValueByName(Option.Input);

            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                await context.SendMessageAsync("Please provide search terms.", _client);
                return;
            }

            RegisterTrackStartedEventListerner(command);

            await _audioService.StartAsync(cancellationToken);

            await context.SendMessageAsync("📻 Searching...", _client);

            var player = await _audioService.GetPlayerAsync(_client, context, connectToVoiceChannel: true);

            if (player is null)
                return;

            await ApplyFilters(cancellationToken, player);
            await ConfigureSponsorBlock(cancellationToken, player);

            // Parse the query to extract search mode and clean query
            // Supports prefixes like jfsearch:, ytsearch:, scsearch:, etc.
            // Default: Jellyfin (jfsearch:) when no prefix is specified
            var (searchMode, queryToSearch) = SearchQueryParser.Parse(searchQuery, JellyfinSearchMode.Jellyfin);

            var trackLoadOptions = new TrackLoadOptions
            {
                SearchMode = searchMode,
            };

            var trackCollection = await _audioService.Tracks.LoadTracksAsync(queryToSearch, trackLoadOptions, cancellationToken: cancellationToken);

            // Check if the result is a playlist or just a single track from the search result
            if (trackCollection.IsPlaylist)
            {
                // If it's a playlist, check if it's a free-text input.
                if (!Uri.IsWellFormedUriString(searchQuery, UriKind.Absolute))
                {
                    // Free text was used (not a direct URL to a playlist), let's prevent queueing the whole playlist.
                    // Queue only the first track of the search result
                    // var firstTrack = trackCollection.Tracks.FirstOrDefault();
                    if (trackCollection.Track != null)
                    {
                        await player.PlayAsync(trackCollection.Track, cancellationToken: cancellationToken).ConfigureAwait(false);
                        // rely on TrackStarted event to send Now Playing
                    }
                    else
                    {
                        await context.SendMessageAsync("No tracks found.", _client);
                    }
                }
                else
                {
                    // It's a playlist and a URL was used, so we can queue all tracks as usual
                    var queueTracks = trackCollection.Tracks
                        .Skip(1)
                        .Select(t => new TrackQueueItem(t))
                        .ToList();

                    await player.PlayAsync(trackCollection.Tracks.First(), cancellationToken: cancellationToken).ConfigureAwait(false);
                    await player.Queue.AddRangeAsync(queueTracks, cancellationToken);
                    await context.SendMessageAsync($"Queued playlist {trackCollection.Playlist?.Name} with {queueTracks.Count} tracks.", _client);
                }
            }
            else
            {
                // It's just a single track or a search result.
                var track = trackCollection.Track;

                if (track != null)
                {
                    await player.PlayAsync(track, cancellationToken: cancellationToken).ConfigureAwait(false);
                    // rely on TrackStarted event to send Now Playing
                }
                else
                {
                    await context.SendMessageAsync("No tracks found.", _client);
                }
            }
        }
        catch (Exception error)
        {
            throw new Exception("Error occured in the Play handler!", error);
        }
    }

    private void RegisterTrackStartedEventListerner(PlayCommand command)
    {
        var guildId = command.Message.GuildId;
        if (!guildId.HasValue)
        {
            // Ignore registration for DMs or contexts without a guild.
            return;
        }

        var gid = guildId.Value;
        if (SubscribedGuilds.Contains(gid))
            return;

        // Ensure we don't accidentally double-subscribe.
        _audioService.TrackStarted -= OnTrackStarted;
        _audioService.TrackEnded -= OnTrackEnded;
        _audioService.TrackStarted += OnTrackStarted;
        _audioService.TrackEnded += OnTrackEnded;
        SubscribedGuilds.Add(gid);
    }

    private static async Task ApplyFilters(CancellationToken cancellationToken, QueuedLavalinkPlayer player)
    {
        var normalizationFilter = new NormalizationFilter(0.5, true);
        player.Filters.SetFilter(normalizationFilter);
        await player.Filters.CommitAsync(cancellationToken);
    }

    private static async Task ConfigureSponsorBlock(CancellationToken cancellationToken, QueuedLavalinkPlayer player)
    {
        var categories = ImmutableArray.Create(
            SegmentCategory.Intro,
            SegmentCategory.Sponsor,
            SegmentCategory.SelfPromotion,
            SegmentCategory.Outro,
            SegmentCategory.Filler);

        await player.UpdateSponsorBlockCategoriesAsync(categories, cancellationToken: cancellationToken);
    }
}
