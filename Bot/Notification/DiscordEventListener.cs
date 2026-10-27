using Discord.WebSocket;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Lunaris2.Notification;

public class DiscordEventListener(DiscordSocketClient client, IServiceScopeFactory serviceScope)
{
    private readonly CancellationToken _cancellationToken = new CancellationTokenSource().Token;

    private IMediator Mediator
    {
        get
        {
            var scope = serviceScope.CreateScope();
            return scope.ServiceProvider.GetRequiredService<IMediator>();
        }
    }

    public async Task StartAsync()
    {
        client.SlashCommandExecuted += OnSlashCommandRecievedAsync;
        client.MessageReceived += OnMessageReceivedAsync;

        await Task.CompletedTask;
    }

    private Task OnMessageReceivedAsync(SocketMessage arg)
    {
        _ = Task.Run(() => Mediator.Publish(new MessageReceivedNotification(arg), _cancellationToken), _cancellationToken);
        return Task.CompletedTask;
    }
    
    private async Task OnSlashCommandRecievedAsync(SocketSlashCommand arg)
    {
        await Mediator.Publish(new SlashCommandReceivedNotification(arg), _cancellationToken);
    }
}