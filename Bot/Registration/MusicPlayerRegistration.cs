using Lavalink4NET.Extensions;
using Lunaris2.Handler.MusicPlayer;
using Lunaris2.Service;

namespace Lunaris2.Registration;

public static class MusicPlayerRegistration
{
    public static IServiceCollection AddMusicPlayer(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddLavalink()
            .ConfigureLavalink(options =>
            {
                options.BaseAddress = new Uri(
                    $"http://{configuration["LavaLinkHostname"]}:{configuration["LavaLinkPort"]}"
                );
                options.WebSocketUri = new Uri($"ws://{configuration["LavaLinkHostname"]}:{configuration["LavaLinkPort"]}/v4/websocket");
                options.Passphrase = configuration["LavaLinkPassword"] ?? "youshallnotpass";
                options.Label = "Node";
            })
            .AddSingleton<MusicEmbed>()
            .AddSingleton<VoiceChannelMonitorService>();

        return services;
    }
}