using System.Collections.Immutable;
using Discord;
using Discord.WebSocket;
using Lunaris2.SlashCommand;
using MediatR;
using Lavalink4NET;
using Lavalink4NET.Integrations.SponsorBlock;
using Lavalink4NET.Integrations.SponsorBlock.Extensions;
using Lavalink4NET.Jellyfin;
using Lavalink4NET.Players;
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
    private readonly PlaybackNotifier _playbackNotifier;

    public PlayHandler(
        DiscordSocketClient client,
        MusicEmbed musicEmbed,
        IAudioService audioService,
        PlaybackNotifier playbackNotifier)
    {
        _client = client;
        _musicEmbed = musicEmbed;
        _audioService = audioService;
        _playbackNotifier = playbackNotifier;
    }

    public async Task Handle(PlayCommand command, CancellationToken cancellationToken)
    {
        var context = command.Message;

        try
        {
            if (context.GuildId is not ulong guildId)
            {
                await ShowAsync(context, "This command only works in a server.");
                return;
            }

            if ((context.User as SocketGuildUser)?.VoiceChannel == null)
            {
                await ShowAsync(context, "You must be in a voice channel to use this command.");
                return;
            }

            var searchQuery = context.GetOptionValueByName(Option.Input);
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                await ShowAsync(context, "Please provide search terms.");
                return;
            }

            await _audioService.StartAsync(cancellationToken);
            await ShowAsync(context, "Searching...");
            if (context.Channel is not null)
                _playbackNotifier.Remember(guildId, context.Channel, context.User.Username, playlistNote: null);

            var retrieval = await _audioService.GetPlayerAsync(_client, context, PlayerChannelBehavior.Move);
            if (retrieval.Player is null)
            {
                await ShowAsync(context, retrieval.Error ?? "Couldn't connect to the voice channel.");
                return;
            }

            var player = retrieval.Player;
            await TryApplyPlaybackOptions(cancellationToken, player);

            var (searchMode, queryToSearch) = SearchQueryParser.Parse(searchQuery, JellyfinSearchMode.Jellyfin);
            var trackLoadOptions = new TrackLoadOptions { SearchMode = searchMode };
            var trackCollection = await _audioService.Tracks.LoadTracksAsync(
                queryToSearch,
                trackLoadOptions,
                cancellationToken: cancellationToken);

            if (trackCollection.IsFailed || trackCollection.Tracks.IsDefaultOrEmpty)
            {
                var reason = trackCollection.Exception?.Message;
                await ShowAsync(context, string.IsNullOrWhiteSpace(reason) ? "No tracks found." : $"No tracks found. {reason}");
                return;
            }

            var tracks = trackCollection.Tracks;
            var isUrl = Uri.IsWellFormedUriString(searchQuery, UriKind.Absolute);
            var plan = PlaybackPlanner.Plan(trackCollection.IsPlaylist, isUrl, tracks.Length, trackCollection.Playlist?.Name);
            var selected = tracks.Take(plan.TrackCount).ToArray();
            if (selected.Length == 0)
            {
                await ShowAsync(context, "No tracks found.");
                return;
            }

            var playlistNote = PlaybackPlanner.PlaylistNote(plan);
            if (context.Channel is not null)
                _playbackNotifier.Remember(guildId, context.Channel, context.User.Username, playlistNote);

            var position = await player.PlayAsync(selected[0], cancellationToken: cancellationToken).ConfigureAwait(false);
            if (selected.Length > 1)
            {
                var queued = selected.Skip(1).Select(track => new TrackQueueItem(track)).ToList();
                await player.Queue.AddRangeAsync(queued, cancellationToken);
            }

            if (position > 0 && plan.IsPlaylist)
            {
                await ShowAsync(context, PlaybackPlanner.PlaylistQueuedMessage(plan.PlaylistName, plan.TrackCount));
                return;
            }

            if (position > 0)
            {
                await ShowAsync(context, PlaybackPlanner.QueuedTrackMessage(position, selected[0].Title));
                return;
            }

            var upcoming = player.Queue.Count == 0
                ? Array.Empty<string?>()
                : new[] { player.Queue[0].Track?.Title };
            var nextTitle = PlaybackPlanner.NextQueuedTitle(upcoming);
            var embed = _musicEmbed.BuildNowPlaying(selected[0], context.User.Username, nextTitle, playlistNote);
            await ShowAsync(context, "Now playing", embed);
        }
        catch (Exception error)
        {
            Console.WriteLine(error);
            await ShowAsync(context, "Something went wrong while trying to play that.");
        }
    }

    private static async Task TryApplyPlaybackOptions(CancellationToken cancellationToken, QueuedLavalinkPlayer player)
    {
        if (player.State is not PlayerState.NotPlaying)
            return;

        try
        {
            player.Filters.SetFilter(new NormalizationFilter(0.5, true));
            await player.Filters.CommitAsync(cancellationToken);
        }
        catch (Exception error)
        {
            Console.WriteLine($"Normalization filter skipped: {error.Message}");
        }

        try
        {
            var categories = ImmutableArray.Create(
                SegmentCategory.Intro,
                SegmentCategory.Sponsor,
                SegmentCategory.SelfPromotion,
                SegmentCategory.Outro,
                SegmentCategory.Filler);

            await player.UpdateSponsorBlockCategoriesAsync(categories, cancellationToken: cancellationToken);
        }
        catch (Exception error)
        {
            Console.WriteLine($"SponsorBlock setup skipped: {error.Message}");
        }
    }

    private async Task ShowAsync(SocketSlashCommand context, string content, Embed? embed = null)
    {
        await context.ModifyOriginalResponseAsync(properties =>
        {
            properties.Content = content;
            if (embed is null)
                properties.Embeds = Array.Empty<Embed>();
            else
                properties.Embed = embed;
        });

        if (context.GuildId is not ulong guildId)
            return;

        var message = await context.GetOriginalResponseAsync();
        _playbackNotifier.Track(guildId, message);
    }
}
