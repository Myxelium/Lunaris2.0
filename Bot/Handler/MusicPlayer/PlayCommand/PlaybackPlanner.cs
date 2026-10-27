namespace Lunaris2.Handler.MusicPlayer.PlayCommand;

public readonly record struct PlaybackPlan(int TrackCount, bool PlayFirstOnly, string? PlaylistName)
{
    public bool HasTracks => TrackCount > 0;

    public bool IsPlaylist => PlaylistName is not null;
}

public static class PlaybackPlanner
{
    public static PlaybackPlan Plan(bool isPlaylist, bool isAbsoluteUrl, int trackCount, string? playlistName)
    {
        if (trackCount <= 0)
            return new PlaybackPlan(0, false, null);

        // A free-text search can come back as a playlist. Queue the best match, not every hit.
        if (isPlaylist && !isAbsoluteUrl)
            return new PlaybackPlan(1, true, null);

        if (isPlaylist)
            return new PlaybackPlan(trackCount, false, playlistName);

        return new PlaybackPlan(1, true, null);
    }

    public static string PlaylistQueuedMessage(string? name, int trackCount)
        => $"Queued playlist {name} with {trackCount} tracks.";

    public static string QueuedTrackMessage(int position, string title)
        => $"Added to queue (#{position}): {title}";

    public static string? PlaylistNote(PlaybackPlan plan)
        => plan.IsPlaylist ? $"Playlist: {plan.PlaylistName} ({plan.TrackCount} tracks)" : null;

    /// <summary>
    /// The queue holds upcoming tracks only. The next one is the first entry.
    /// </summary>
    public static string? NextQueuedTitle(IReadOnlyList<string?> queuedTitles)
        => queuedTitles.Count == 0 ? null : queuedTitles[0];
}
