using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using OllamaSharp;

namespace Lunaris2.Handler.ChatCommand;

public sealed class ChatSessionStore
{
    private readonly ConcurrentDictionary<ulong, Chat> _chats = new();
    private readonly OllamaApiClient _ollama;

    public ChatSessionStore(IOptions<ChatSettings> chatSettings)
    {
        var settings = chatSettings.Value;
        _ollama = new OllamaApiClient(settings.Url, settings.Model);
    }

    public Chat GetOrCreate(ulong channelId) => _chats.GetOrAdd(channelId, _ => new Chat(_ollama));
}
