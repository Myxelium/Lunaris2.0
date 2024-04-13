namespace Lunaris2.SlashCommand;

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
    
    public static string[] GetAllCommands()
    {
        return typeof(Command)
            .GetNestedTypes()
            .Select(command => command.GetField("Name")?.GetValue(null)?.ToString())
            .ToArray()!;
    }
}
