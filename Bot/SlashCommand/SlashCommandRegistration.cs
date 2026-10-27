using Discord;
using Discord.WebSocket;

namespace Lunaris2.SlashCommand;

public static class SlashCommandRegistration
{
    public static void RegisterCommands(this DiscordSocketClient client, bool schedulerEnabled = false)
    {
        var retained = RetainedCommandNames(schedulerEnabled);
        RegisterCommand(client, retained, Command.Resume.Name, Command.Resume.Description);
        RegisterCommand(client, retained, Command.Pause.Name, Command.Pause.Description);
        RegisterCommand(client, retained, Command.Disconnect.Name, Command.Disconnect.Description);
        RegisterCommand(client, retained, Command.Skip.Name, Command.Skip.Description);
        RegisterCommand(client, retained, Command.Play.Name, Command.Play.Description, Command.Play.Options);
        RegisterCommand(client, retained, Command.Resume.Name, Command.Resume.Description);
        RegisterCommand(client, retained, Command.Clear.Name, Command.Clear.Description);
        if (schedulerEnabled)
            RegisterCommand(client, retained, Command.Scheduler.Name, Command.Scheduler.Description, Command.Scheduler.Options);
    }

    public static string[] RetainedCommandNames(bool schedulerEnabled)
    {
        return Command.GetAllCommands()
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Where(name => schedulerEnabled || !string.Equals(name, Command.Scheduler.Name, StringComparison.Ordinal))
            .ToArray();
    }

    private static void RegisterCommand(
        DiscordSocketClient client,
        string[] retainedCommands,
        string commandName, 
        string commandDescription, 
        List<SlashCommandOptionBuilder>? commandOptions = null)
    {
        var command = new SlashCommandBuilder(commandName, commandDescription, commandOptions, retainedCommands);
        _ = command.CreateSlashCommand(client);
    }
}
