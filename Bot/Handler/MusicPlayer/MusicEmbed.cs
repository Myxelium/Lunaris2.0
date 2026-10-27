using Discord;
using Lavalink4NET.Tracks;

namespace Lunaris2.Handler.MusicPlayer;

public class MusicEmbed
{
    public Embed BuildNowPlaying(
        LavalinkTrack track,
        string queuedBy,
        string? nextTitle = null,
        string? playlistNote = null)
    {
        var length = track.IsLiveStream ? "live" : track.Duration.ToString(@"hh\:mm\:ss");
        var artist = string.IsNullOrWhiteSpace(track.Author) ? "Unknown" : track.Author;
        var nextSong = string.IsNullOrWhiteSpace(nextTitle) ? string.Empty : $"\nNext song: {nextTitle}";
        var playlist = string.IsNullOrWhiteSpace(playlistNote) ? string.Empty : $"\n{playlistNote}";

        var builder = new EmbedBuilder()
            .WithAuthor("Lunaris", "https://media.tenor.com/GqAwMt01UXgAAAAi/cd.gif")
            .WithTitle(track.Title)
            .WithDescription($"Length: {length}\nArtist: {artist}\nQueued by: {queuedBy}{nextSong}{playlist}")
            .WithColor(Color.Magenta);

        if (track.ArtworkUri is not null)
            builder.WithThumbnailUrl(track.ArtworkUri.ToString());

        return builder.Build();
    }
}
