using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace Lunaris2.SlashCommand;

public class SlashCommandBuilder(string commandName, string commandDescription, List<SlashCommandOptionBuilder> commandOptions = null)
{
    private string CommandName { get; set; } = commandName;
    private string CommandDescription { get; set; } = commandDescription;
    private List<SlashCommandOptionBuilder> CommandOptions { get; set; }

    public async Task CreateSlashCommand(DiscordSocketClient client)
    {
        var registeredCommands = await client.GetGlobalApplicationCommandsAsync();
        await RemoveUnusedCommands(Command.GetAllCommands(), registeredCommands);
        
        if (await CommandExists(registeredCommands))
            return;

        var globalCommand = new Discord.SlashCommandBuilder();
        globalCommand.WithName(CommandName);
        globalCommand.WithDescription(CommandDescription);
        
        commandOptions.ForEach(option => globalCommand.AddOption(option));

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

    private static async Task RemoveUnusedCommands(string[] commands, IEnumerable<SocketApplicationCommand> registeredCommands)
    {
        // Remove commands from Discord(registeredCommands) that are not in the list of commands
        foreach(var command in registeredCommands)
        {
            if (commands.Contains(command.Name)) 
                continue;
            
            await command.DeleteAsync();
            Console.WriteLine($"Command {command.Name} has been removed.");
        }
    }
    
    private Task<bool> CommandExists(IEnumerable<SocketApplicationCommand> registeredCommands)
    {
        if (!registeredCommands.Any(command => command.Name == CommandName && command.Description == CommandDescription))
            return Task.FromResult(false);
        
        Console.WriteLine($"Command {CommandName} already exists.");
        return Task.FromResult(true);
    }
}