using System.Text;
using Discord.WebSocket;
using MediatR;
using Microsoft.Extensions.Options;

namespace Lunaris2.Handler.ChatCommand
{
    public record ChatCommand(SocketMessage Message, string FilteredMessage) : IRequest;

    public class ChatHandler : IRequestHandler<ChatCommand>
    {
        private readonly ChatSessionStore _sessions;
        private readonly ChatSettings _chatSettings;
        private readonly Random _random = new();

        public ChatHandler(IOptions<ChatSettings> chatSettings, ChatSessionStore sessions)
        {
            _chatSettings = chatSettings.Value;
            _sessions = sessions;
        }

        public async Task Handle(ChatCommand command, CancellationToken cancellationToken)
        {
            var channel = command.Message.Channel;
            using var setTyping = channel.EnterTypingState();

            if (string.IsNullOrWhiteSpace(command.FilteredMessage))
            {
                await channel.SendMessageAsync("Am I expected to read your mind?");
                return;
            }

            var userMessage = command.FilteredMessage;
            if (_chatSettings.Personalities is { Count: > 0 })
            {
                var personality = _chatSettings.Personalities[_random.Next(_chatSettings.Personalities.Count)];
                userMessage = $"{personality.Instruction} {userMessage}";
            }

            var response = await GenerateResponse(userMessage, channel.Id, cancellationToken);
            if (response.Length > 2000)
                response = response[..1997] + "...";

            if (string.IsNullOrWhiteSpace(response))
                response = "I don't have an answer for that.";

            await channel.SendMessageAsync(response);
        }

        private async Task<string> GenerateResponse(string userMessage, ulong channelId, CancellationToken cancellationToken)
        {
            var response = new StringBuilder();
            var chat = _sessions.GetOrCreate(channelId);

            await foreach (var token in chat.SendAsync(userMessage, cancellationToken))
                response.Append(token);

            return response.ToString();
        }
    }
}
