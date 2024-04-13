using Discord.Commands;

namespace Lunaris2.Helpers
{
    public static class CommandServiceSetup
    {
        public static CommandService Setup()
        {
            return new CommandService();
        }
    }
}