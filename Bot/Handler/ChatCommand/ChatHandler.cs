using System.Text;
using Discord.WebSocket;
using MediatR;
using Microsoft.Extensions.Options;
using OllamaSharp;

namespace Lunaris2.Handler.ChatCommand
{
    public record ChatCommand(SocketMessage Message, string FilteredMessage) : IRequest;

    public class ChatHandler : IRequestHandler<ChatCommand>
    {
        private readonly OllamaApiClient _ollama;
        private readonly Dictionary<ulong, Chat?> _chatContexts = new();
        private readonly ChatSettings _chatSettings;

        public ChatHandler(IOptions<ChatSettings> chatSettings)
        {
            _chatSettings = chatSettings.Value;
            var uri = new Uri(chatSettings.Value.Url);
            
            _ollama = new OllamaApiClient(uri)
            {
                SelectedModel = chatSettings.Value.Model
            };
        }
        
        public async Task Handle(ChatCommand command, CancellationToken cancellationToken)
        {
            var channelId = command.Message.Channel.Id;
            _chatContexts.TryAdd(channelId, null);

            var userMessage = command.FilteredMessage;

            var randomPersonality = _chatSettings.Personalities[new Random().Next(_chatSettings.Personalities.Count)];

            userMessage = $"{randomPersonality.Instruction} {userMessage}";
            
            using var setTyping = command.Message.Channel.EnterTypingState();
            
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                await command.Message.Channel.SendMessageAsync("Am I expected to read your mind?");
                setTyping.Dispose();
                return;
            }
            
            var response = await GenerateResponse(userMessage, channelId, cancellationToken);
            await command.Message.Channel.SendMessageAsync(response);
            
            setTyping.Dispose();
        }

        private async Task<string> GenerateResponse(string userMessage, ulong channelId, CancellationToken cancellationToken)
        {
            var response = new StringBuilder();

            if (_chatContexts[channelId] == null)
            {
                _chatContexts[channelId] = _ollama.Chat(stream => response.Append(stream.Message?.Content ?? ""));
            }

            await _chatContexts[channelId].Send(userMessage, cancellationToken);

            return response.ToString();
        }
    }
}
