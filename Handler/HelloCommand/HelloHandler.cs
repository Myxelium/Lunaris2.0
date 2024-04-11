using Discord.WebSocket;
using Lunaris2.SlashCommand;
using MediatR;
using Newtonsoft.Json;

namespace Lunaris2.Handler.HelloCommand
{
    public record HelloCommand(SocketSlashCommand Message) : IRequest;

    public class HelloHandler : IRequestHandler<HelloCommand>
    {
        public async Task Handle(HelloCommand message, CancellationToken cancellationToken)
        {
            Console.WriteLine(JsonConvert.SerializeObject(Command.GetAllCommands()));
            
            await message.Message.RespondAsync($"Hello, {message.Message.User.Username}!");
        }
    }
}