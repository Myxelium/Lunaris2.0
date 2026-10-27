using Lunaris2.Handler.MusicPlayer.PlayCommand;
using Lunaris2.Service;
using Xunit;

namespace Lunaris2.Tests;

public class PlaybackPlannerTests
{
    [Fact]
    public void Empty_result_plays_nothing()
    {
        var plan = PlaybackPlanner.Plan(isPlaylist: false, isAbsoluteUrl: false, trackCount: 0, playlistName: null);

        Assert.False(plan.HasTracks);
        Assert.Equal(0, plan.TrackCount);
    }

    [Fact]
    public void Search_plays_only_the_best_match()
    {
        var plan = PlaybackPlanner.Plan(isPlaylist: false, isAbsoluteUrl: false, trackCount: 5, playlistName: null);

        Assert.Equal(1, plan.TrackCount);
        Assert.True(plan.PlayFirstOnly);
        Assert.False(plan.IsPlaylist);
    }

    [Fact]
    public void Free_text_playlist_does_not_queue_every_hit()
    {
        var plan = PlaybackPlanner.Plan(isPlaylist: true, isAbsoluteUrl: false, trackCount: 8, playlistName: "search");

        Assert.Equal(1, plan.TrackCount);
        Assert.True(plan.PlayFirstOnly);
        Assert.Null(plan.PlaylistName);
    }

    [Fact]
    public void Playlist_url_keeps_every_track_in_the_count()
    {
        var plan = PlaybackPlanner.Plan(isPlaylist: true, isAbsoluteUrl: true, trackCount: 3, playlistName: "Favorites");

        Assert.Equal(3, plan.TrackCount);
        Assert.False(plan.PlayFirstOnly);
        Assert.Equal("Queued playlist Favorites with 3 tracks.", PlaybackPlanner.PlaylistQueuedMessage(plan.PlaylistName, plan.TrackCount));
        Assert.Equal("Playlist: Favorites (3 tracks)", PlaybackPlanner.PlaylistNote(plan));
    }

    [Fact]
    public void Next_song_is_the_first_queued_track()
    {
        var next = PlaybackPlanner.NextQueuedTitle(["next", "after that"]);

        Assert.Equal("next", next);
    }

    [Fact]
    public void Queued_track_message_uses_the_returned_position()
    {
        Assert.Equal("Added to queue (#2): Song", PlaybackPlanner.QueuedTrackMessage(2, "Song"));
    }
}

public class VoicePresenceTests
{
    private const ulong BotId = 10;

    [Fact]
    public void Leaves_when_only_the_bot_remains()
    {
        var members = new[] { new VoiceMember(BotId, IsBot: true) };

        Assert.True(VoicePresence.ShouldLeave(members, BotId));
    }

    [Fact]
    public void Stays_when_a_person_is_listening()
    {
        var members = new[]
        {
            new VoiceMember(BotId, IsBot: true),
            new VoiceMember(20, IsBot: false)
        };

        Assert.False(VoicePresence.ShouldLeave(members, BotId));
    }

    [Fact]
    public void Leaves_when_only_other_bots_remain()
    {
        var members = new[]
        {
            new VoiceMember(BotId, IsBot: true),
            new VoiceMember(30, IsBot: true)
        };

        Assert.True(VoicePresence.ShouldLeave(members, BotId));
    }

    [Fact]
    public void Does_not_leave_a_channel_the_bot_is_not_in()
    {
        var members = new[] { new VoiceMember(20, IsBot: false) };

        Assert.False(VoicePresence.ShouldLeave(members, BotId));
    }

    [Fact]
    public void Bot_is_not_counted_as_a_listener_even_if_it_is_not_flagged_as_a_bot()
    {
        var members = new[] { new VoiceMember(BotId, IsBot: false) };

        Assert.True(VoicePresence.ShouldLeave(members, BotId));
    }
}

public class AloneLeaveSchedulerTests
{
    [Fact]
    public void Second_schedule_for_the_same_channel_is_rejected()
    {
        var scheduler = new AloneLeaveScheduler();

        Assert.True(scheduler.TrySchedule(1, out var first));
        Assert.False(scheduler.TrySchedule(1, out _));
        Assert.False(first!.IsCancellationRequested);
    }

    [Fact]
    public void Completing_a_cancelled_timer_does_not_drop_the_replacement()
    {
        var scheduler = new AloneLeaveScheduler();
        Assert.True(scheduler.TrySchedule(1, out var first));

        scheduler.Cancel(1);
        Assert.True(first!.IsCancellationRequested);

        Assert.True(scheduler.TrySchedule(1, out var second));
        scheduler.CompleteIfCurrent(1, first);

        Assert.True(scheduler.IsScheduled(1));
        Assert.False(second!.IsCancellationRequested);
    }

    [Fact]
    public void Cancel_all_cancels_pending_timers()
    {
        var scheduler = new AloneLeaveScheduler();
        Assert.True(scheduler.TrySchedule(1, out var schedule));

        scheduler.CancelAll();

        Assert.True(schedule!.IsCancellationRequested);
        Assert.False(scheduler.IsScheduled(1));
    }
}
