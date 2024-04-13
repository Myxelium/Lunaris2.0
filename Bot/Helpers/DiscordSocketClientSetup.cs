using Discord;
using Discord.WebSocket;

namespace Lunaris2.Helpers
{
    public static class DiscordSocketClientSetup
    {
        public static DiscordSocketClient Setup()
        {
            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.All
            };
            return new DiscordSocketClient(config);
        }
    }
}