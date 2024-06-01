using Discord;

namespace Lunaris2.SlashCommand;

public static class Option
{
    public const string Input = "input";
}

public static class Command
{
    public static class Hello
    {
        public const string Name = "hello";
        public const string Description = "Say hello to the bot!";
    }

    public static class Goodbye
    {
        public const string Name = "goodbye";
        public const string Description = "Say goodbye to the bot!";
    }
    
    public static class Chat
    {
        public const string Name = "chat";
        public const string Description = "Chat with the bot!";
        
        public static readonly List<SlashCommandOptionBuilder>? Options = new()
        {
            new SlashCommandOptionBuilder
            {
                Name = "message",
                Description = "Chat with Lunaris",
                Type = ApplicationCommandOptionType.String,
                IsRequired = true
            }
        };
    }
    
    public static class Join
    {
        public const string Name = "join";
        public const string Description = "Join the voice channel!";
    }
    
    public static class Skip
    {
        public const string Name = "skip";
        public const string Description = "Skip the current song!";
    }
    
    public static class Stop
    {
        public const string Name = "stop";
        public const string Description = "Stop the music!";
    }
    
    public static class Play
    {
        public const string Name = "play";
        public const string Description = "Play a song!";

        public static readonly List<SlashCommandOptionBuilder>? Options = new()
        {
            new SlashCommandOptionBuilder
            {
                Name = "input",
                Description = "The song you want to play",
                Type = ApplicationCommandOptionType.String,
                IsRequired = true
            },
        };
    }
    
    public static string[] GetAllCommands()
    {
        return typeof(Command)
            .GetNestedTypes()
            .Select(command => command.GetField("Name")?.GetValue(null)?.ToString())
            .ToArray()!;
    }
}
