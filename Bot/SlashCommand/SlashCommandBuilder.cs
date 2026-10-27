using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace Lunaris2.SlashCommand;

public class SlashCommandBuilder(
    string commandName, 
    string commandDescription, 
    List<SlashCommandOptionBuilder>? commandOptions = null,
    string[]? retainedCommands = null)
{
    private string CommandName { get; set; } = commandName;
    private string CommandDescription { get; set; } = commandDescription;
    private List<SlashCommandOptionBuilder>? CommandOptions { get; set; } = commandOptions;

    public async Task CreateSlashCommand(DiscordSocketClient client)
    {
        var registeredCommands = await client.GetGlobalApplicationCommandsAsync();
        await RemoveUnusedCommands(retainedCommands ?? Command.GetAllCommands(), registeredCommands);

        var existing = registeredCommands.FirstOrDefault(command => command.Name == CommandName);
        if (existing is not null && SameShape(existing))
        {
            Console.WriteLine($"Command {CommandName} already exists.");
            return;
        }

        if (existing is not null)
            await DeleteCommand(existing);

        var globalCommand = new Discord.SlashCommandBuilder();
        globalCommand.WithName(CommandName);
        globalCommand.WithDescription(CommandDescription);
        
        CommandOptions?.ForEach(option => globalCommand.AddOption(option));

        try
        {
            await client.CreateGlobalApplicationCommandAsync(globalCommand.Build());
            Console.WriteLine($"Command {CommandName} has been registered.");
        }
        catch(HttpException exception)
        {
            var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
            Console.WriteLine(json);
        }
    }

    private bool SameShape(SocketApplicationCommand existing)
    {
        if (existing.Description != CommandDescription)
            return false;

        var expected = (CommandOptions ?? []).Select(option => option.Name).OrderBy(name => name);
        var actual = (existing.Options ?? []).Select(option => option.Name).OrderBy(name => name);
        return expected.SequenceEqual(actual);
    }

    private static async Task RemoveUnusedCommands(string[] commands, IEnumerable<SocketApplicationCommand> registeredCommands)
    {
        foreach(var command in registeredCommands)
        {
            if (commands.Contains(command.Name)) 
                continue;

            await DeleteCommand(command);
        }
    }

    private static async Task DeleteCommand(SocketApplicationCommand command)
    {
        try
        {
            await command.DeleteAsync();
            Console.WriteLine($"Command {command.Name} has been removed.");
        }
        catch (HttpException exception)
        {
            Console.WriteLine($"Command {command.Name} could not be removed: {exception.Message}");
        }
    }
}