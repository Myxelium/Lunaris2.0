using Discord;
using Discord.WebSocket;
using Victoria;
using Victoria.Player;

namespace Lunaris2.Handler.MusicPlayer;

public class MusicEmbed
{
    private Embed SendMusicEmbed(
        string imageUrl, 
        string title, 
        string length, 
        string artist, 
        string queuedBy, 
        string? nextInQueue)
    {
        return new EmbedBuilder()
            .WithAuthor("Lunaris", "https://media.tenor.com/GqAwMt01UXgAAAAi/cd.gif")
            .WithTitle(title)
            .WithDescription($"Length: {length}\nArtist: {artist}\nQueued by: {queuedBy}\nNext in queue: {nextInQueue}")
            .WithColor(Color.Magenta)
            .WithThumbnailUrl(imageUrl)
            .Build();
    }
    
    public async Task NowPlayingEmbed(
        LavaPlayer<LavaTrack> player, 
        SocketSlashCommand context, 
        DiscordSocketClient client)
    {
        var artwork = await player.Track.FetchArtworkAsync();
        var getNextTrack = player.Vueue.Count > 1 ? player.Vueue.ToArray()[1].Title : "No songs in queue.";
        var embed = SendMusicEmbed(
            artwork, 
            player.Track.Title, 
            player.Track.Duration.ToString(), 
            player.Track.Author, 
            context.User.Username, 
            getNextTrack);

        await context.SendMessageAsync(embed, client);
    }
}
