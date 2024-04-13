using System.Threading.Tasks;
using Discord.WebSocket;
using Lunaris2.SlashCommand;

namespace Lunaris2.Helpers
{
    public static class ClientReadyHandler
    {
        public static Task Client_Ready(this DiscordSocketClient client)
        {
            client.RegisterCommands();
            return Task.CompletedTask;
        }
    }
}