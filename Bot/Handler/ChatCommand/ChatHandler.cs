using Discord;
using Discord.WebSocket;
using Lunaris2.Handler.MusicPlayer;
using Lunaris2.SlashCommand;
using MediatR;
using OllamaSharp;
using OllamaSharp.Models;

namespace Lunaris2.Handler.ChatCommand;

public record ChatCommand(SocketSlashCommand Message) : IRequest;

public class ChatHandler : IRequestHandler<ChatCommand>
{        
    private readonly Uri _uri = new("http://192.168.50.54:11434");
    private OllamaApiClient _ollama;
    private SocketSlashCommand _context;
    
    public ChatHandler()
    {
        _ollama = new OllamaApiClient(_uri);
        _ollama.SelectedModel = "lunaris";
    }
    
    public async Task Handle(ChatCommand command, CancellationToken cancellationToken)
    {
        _context = command.Message;
        var userMessage = _context.GetOptionValueByName(Option.Input);
        var setTyping = command.Message.Channel.EnterTypingState();
        await command.Message.DeferAsync();
        
        var response = "";
        ConversationContext chatContext = null;

        async void Streamer(GenerateCompletionResponseStream stream)
        {
            response += stream.Response;
            await command.Message.ModifyOriginalResponseAsync(properties => properties.Content = response);
        }

        chatContext = await _ollama.StreamCompletion(userMessage, chatContext, Streamer, cancellationToken: cancellationToken);
        setTyping.Dispose();
    }
}