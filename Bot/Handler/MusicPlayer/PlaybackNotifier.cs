using Discord;
using Lavalink4NET;
using Lavalink4NET.Events.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Tracks;
using Lunaris2.Handler.MusicPlayer.PlayCommand;

namespace Lunaris2.Handler.MusicPlayer;

public sealed class PlaybackSession
{
    public IMessageChannel? Channel { get; set; }
    public string RequestedBy { get; set; } = "someone";
    public string? PlaylistNote { get; set; }
    public IUserMessage? Message { get; set; }
}

public sealed class PlaybackNotifier
{
    private readonly MusicEmbed _musicEmbed;
    private readonly object _gate = new();
    private readonly Dictionary<ulong, PlaybackSession> _sessions = new();

    public PlaybackNotifier(IAudioService audioService, MusicEmbed musicEmbed)
    {
        _musicEmbed = musicEmbed;
        audioService.TrackStarted += OnTrackStarted;
    }

    public void Remember(ulong guildId, IMessageChannel channel, string requestedBy, string? playlistNote)
    {
        lock (_gate)
        {
            var session = GetOrCreate(guildId);
            session.Channel = channel;
            session.RequestedBy = requestedBy;
            session.PlaylistNote = playlistNote;
        }
    }

    public void Track(ulong guildId, IUserMessage message)
    {
        lock (_gate)
            GetOrCreate(guildId).Message = message;
    }

    private async Task OnTrackStarted(object sender, TrackStartedEventArgs eventArgs)
    {
        try
        {
            var player = eventArgs.Player;
            PlaybackSession snapshot;
            lock (_gate)
            {
                if (!_sessions.TryGetValue(player.GuildId, out var session) || session.Channel is null)
                    return;

                snapshot = new PlaybackSession
                {
                    Channel = session.Channel,
                    RequestedBy = session.RequestedBy,
                    PlaylistNote = session.PlaylistNote,
                    Message = session.Message
                };
            }

            var queue = (player as QueuedLavalinkPlayer)?.Queue;
            var upcoming = new List<string?>();
            if (queue is not null)
            {
                for (var index = 0; index < Math.Min(queue.Count, 2); index++)
                    upcoming.Add(queue[index].Track?.Title);
            }

            var embed = _musicEmbed.BuildNowPlaying(
                eventArgs.Track,
                snapshot.RequestedBy,
                PlaybackPlanner.NextQueuedTitle(upcoming),
                snapshot.PlaylistNote);
            await PublishAsync(player.GuildId, snapshot, embed);
        }
        catch (Exception error)
        {
            Console.WriteLine(error);
        }
    }

    private async Task PublishAsync(ulong guildId, PlaybackSession snapshot, Embed embed)
    {
        if (snapshot.Message is not null)
        {
            try
            {
                await snapshot.Message.ModifyAsync(properties =>
                {
                    properties.Content = "Now playing";
                    properties.Embed = embed;
                });
                return;
            }
            catch (Exception error)
            {
                Console.WriteLine($"Couldn't update the now-playing message: {error.Message}");
            }
        }

        if (snapshot.Channel is null)
            return;

        try
        {
            var sent = await snapshot.Channel.SendMessageAsync(embed: embed);
            Track(guildId, sent);
        }
        catch (Exception error)
        {
            Console.WriteLine($"Couldn't send the now-playing message: {error.Message}");
        }
    }

    private PlaybackSession GetOrCreate(ulong guildId)
    {
        if (!_sessions.TryGetValue(guildId, out var session))
        {
            session = new PlaybackSession();
            _sessions[guildId] = session;
        }

        return session;
    }
}
