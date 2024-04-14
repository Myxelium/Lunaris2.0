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
        client.SlashCommandExecuted += OnMessageReceivedAsync;

        await Task.CompletedTask;
    }

    private async Task OnMessageReceivedAsync(SocketSlashCommand  arg)
    {
        await Mediator.Publish(new MessageReceivedNotification(arg), _cancellationToken);
    }
}