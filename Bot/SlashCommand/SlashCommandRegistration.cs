using Discord;
using Discord.WebSocket;

namespace Lunaris2.SlashCommand;

public static class SlashCommandRegistration
{
    public static void RegisterCommands(this DiscordSocketClient client)
    {
        RegisterCommand(client, Command.Hello.Name, Command.Hello.Description);
        RegisterCommand(client, Command.Goodbye.Name, Command.Goodbye.Description);
        RegisterCommand(client, Command.Join.Name, Command.Join.Description);
        RegisterCommand(client, Command.Skip.Name, Command.Skip.Description);
        RegisterCommand(client, Command.Play.Name, Command.Play.Description, Command.Play.Options);
        RegisterCommand(client, Command.Stop.Name, Command.Stop.Description); 
    }

    private static void RegisterCommand(
        DiscordSocketClient client, 
        string commandName, 
        string commandDescription, 
        List<SlashCommandOptionBuilder>? commandOptions = null)
    {
        var command = new SlashCommandBuilder(commandName, commandDescription, commandOptions);
        _ = command.CreateSlashCommand(client);
    }
}
