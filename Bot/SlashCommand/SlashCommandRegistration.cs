using Discord.WebSocket;

namespace Lunaris2.SlashCommand;

public static class SlashCommandRegistration
{
    public static void RegisterCommands(this DiscordSocketClient client)
    {
        RegisterCommand(client, Command.Hello.Name, Command.Hello.Description);
        RegisterCommand(client, Command.Goodbye.Name, Command.Goodbye.Description);
    }

    private static void RegisterCommand(DiscordSocketClient client, string commandName, string commandDescription)
    {
        var command = new SlashCommandBuilder(commandName, commandDescription);
        _ = command.CreateSlashCommand(client);
    }
}
