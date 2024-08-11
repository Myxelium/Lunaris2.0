using Discord;

namespace Lunaris2.SlashCommand;

public static class Option
{
    public const string Input = "input";
}

public static class Command
{
    // public static class Hello
    // {
    //     public const string Name = "hello";
    //     public const string Description = "Say hello to the bot!";
    // }
    //
    // public static class Goodbye
    // {
    //     public const string Name = "goodbye";
    //     public const string Description = "Say goodbye to the bot!";
    // }
    
    public static class Disconnect
    {
        public const string Name = "disconnect";
        public const string Description = "Disconnect from the voice channel!";
    }
    
    public static class Skip
    {
        public const string Name = "skip";
        public const string Description = "Skip the current song!";
    }
    
    public static class Resume
    {
        public const string Name = "resume";
        public const string Description = "Resume the music!";
    }
    
    public static class Pause
    {
        public const string Name = "pause";
        public const string Description = "Pause the music!";
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
