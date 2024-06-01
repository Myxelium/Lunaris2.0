using System.Text;
using Discord.WebSocket;
using Lunaris2.Handler.MusicPlayer;
using Lunaris2.SlashCommand;
using MediatR;
using OllamaSharp;
using OllamaSharp.Models;

namespace Lunaris2.Handler.ChatCommand
{
    public record ChatCommand(SocketSlashCommand Message) : IRequest;

    public class ChatHandler : IRequestHandler<ChatCommand>
    {        
        private readonly Uri _uri = new("http://192.168.50.54:11434");
        private readonly OllamaApiClient _ollama;
        private SocketSlashCommand _context;
        
        public ChatHandler()
        {
            _ollama = new OllamaApiClient(_uri)
            {
                SelectedModel = "lunaris"
            };
        }
        
        public async Task Handle(ChatCommand command, CancellationToken cancellationToken)
        {
            _context = command.Message;
            
            var userMessage = _context.GetOptionValueByName(Option.Input);
            
            using var setTyping = command.Message.Channel.EnterTypingState();
            await command.Message.DeferAsync();
            
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                await command.Message.ModifyOriginalResponseAsync(properties => properties.Content = "Am I expected to read your mind?");
                setTyping.Dispose();
                return;
            }
            
            var response = await GenerateResponse(userMessage, cancellationToken);
            await command.Message.ModifyOriginalResponseAsync(properties => properties.Content = response);
        }

        private async Task<string> GenerateResponse(string userMessage, CancellationToken cancellationToken)
        {
            var response = new StringBuilder();
            ConversationContext? chatContext = null;

            chatContext = await _ollama.StreamCompletion(
                userMessage, 
                chatContext, 
                Streamer, 
                cancellationToken: cancellationToken);
            
            return response.ToString();

            void Streamer(GenerateCompletionResponseStream stream) => 
                response.Append(stream.Response);
        }
    }
}