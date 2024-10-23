using Discord;
using Discord.WebSocket;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Tracks;

namespace Lunaris2.Handler.MusicPlayer;

public class MusicEmbed
{
    private Embed SendMusicEmbed(
        string imageUrl, 
        string title, 
        string length, 
        string artist, 
        string queuedBy,
        string? nextSong = null)
    {
        var getNextSong = nextSong is not null ? $"\nNext Song: {nextSong}" : string.Empty;
        
        return new EmbedBuilder()
            .WithAuthor("Lunaris", "https://media.tenor.com/GqAwMt01UXgAAAAi/cd.gif")
            .WithTitle(title)
            .WithDescription($"Length: {length}\nArtist: {artist}\nQueued by: {queuedBy}{getNextSong}")
            .WithColor(Color.Magenta)
            .WithThumbnailUrl(imageUrl)
            .Build();
    }
    
    public async Task NowPlayingEmbed(
        LavalinkTrack track, 
        SocketSlashCommand context, 
        DiscordSocketClient client,
        ITrackQueue? queue = null)
    {
        var duration = TimeSpan.Parse(track.Duration.ToString());

        var artwork = track.ArtworkUri;
        var nextSong = queue?.Count > 1 ? queue[1].Track?.Title : null;
        var embed = SendMusicEmbed(
            artwork.ToString(), 
            track.Title, 
            duration.ToString(@"hh\:mm\:ss"), 
            track.Author, 
            context.User.Username,
            nextSong);
    
        await context.SendMessageAsync(embed, client);
    }
}
