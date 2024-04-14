using Discord;
using Discord.WebSocket;
using Lavalink4NET.Tracks;

namespace Lunaris2.Handler.MusicPlayer;

public class MusicEmbed
{
    private Embed SendMusicEmbed(
        string imageUrl, 
        string title, 
        string length, 
        string artist, 
        string queuedBy)
    {
        return new EmbedBuilder()
            .WithAuthor("Lunaris", "https://media.tenor.com/GqAwMt01UXgAAAAi/cd.gif")
            .WithTitle(title)
            .WithDescription($"Length: {length}\nArtist: {artist}\nQueued by: {queuedBy}")
            .WithColor(Color.Magenta)
            .WithThumbnailUrl(imageUrl)
            .Build();
    }
    
    public async Task NowPlayingEmbed(
        LavalinkTrack player, 
        SocketSlashCommand context, 
        DiscordSocketClient client)
    {
        var artwork = player.ArtworkUri;
        var embed = SendMusicEmbed(
            artwork.ToString(), 
            player.Title, 
            player.Duration.ToString(), 
            player.Author, 
            context.User.Username);
        
        await context.SendMessageAsync(embed, client);
    }
}
