using Discord.WebSocket;
using MediatR;

namespace Lunaris2.Handler.Scheduler;

public class ProcessMessageCommand : IRequest
{
    public ulong? Context { get; set; }
    public string Content { get; set; }
}

public class ProcessMessageHandler(DiscordSocketClient client) : IRequestHandler<ProcessMessageCommand>
{
    public Task Handle(ProcessMessageCommand request, CancellationToken cancellationToken)
    {
        if (request.Context == null) 
            return Task.FromResult(Unit.Value);
        
        var channel = client.GetChannel(request.Context.Value) as ISocketMessageChannel;
        
        if (channel == null)
            return Task.FromResult(Unit.Value);
        
        using var setTyping = channel.EnterTypingState();
        channel.SendMessageAsync(request.Content);
        setTyping.Dispose();

        return Task.FromResult(Unit.Value);
    }
}