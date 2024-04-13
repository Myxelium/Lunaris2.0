using Discord;

namespace Lunaris2.Helpers
{
    public static class LogHandler
    {
        public static Task Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }
    }
}