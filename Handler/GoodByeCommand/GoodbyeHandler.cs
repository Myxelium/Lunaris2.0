using Discord.WebSocket;
using MediatR;

namespace Lunaris2.Handler.GoodByeCommand
{
    public record GoodbyeCommand(SocketSlashCommand Message) : IRequest;

    public class GoodbyeHandler : IRequestHandler<GoodbyeCommand>
    {
        public async Task Handle(GoodbyeCommand message, CancellationToken cancellationToken)
        {
            await message.Message.RespondAsync($"Goodbye, {message.Message.User.Username}! :c");
        }
    }
}