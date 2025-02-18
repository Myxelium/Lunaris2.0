using Discord;
using Discord.WebSocket;

namespace Lunaris2.SlashCommand;

public static class SlashCommandRegistration
{
    public static void RegisterCommands(this DiscordSocketClient client)
    {
        RegisterCommand(client, Command.Resume.Name, Command.Resume.Description);
        RegisterCommand(client, Command.Pause.Name, Command.Pause.Description);
        RegisterCommand(client, Command.Disconnect.Name, Command.Disconnect.Description);
        RegisterCommand(client, Command.Skip.Name, Command.Skip.Description);
        RegisterCommand(client, Command.Play.Name, Command.Play.Description, Command.Play.Options);
        RegisterCommand(client, Command.Resume.Name, Command.Resume.Description);
        RegisterCommand(client, Command.Clear.Name, Command.Clear.Description);
        RegisterCommand(client, Command.Scheduler.Name, Command.Scheduler.Description, Command.Scheduler.Options);
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
