### README.md

# Handlers

Handlers for the Lunaris2 bot, which is built using C#, Discord.Net, and Lavalink4NET. Below is a detailed description of each handler and their responsibilities.

## Handlers

### ClearQueueHandler

Handles the command to clear the music queue.

```csharp
public class ClearQueueHandler : IRequestHandler<ClearQueueCommand>
```

### DisconnectHandler

Handles the command to disconnect the bot from the voice channel.

```csharp
public class DisconnectHandler : IRequestHandler<DisconnectCommand>
```

### PauseHandler

Handles the command to pause the currently playing track.

```csharp
public class PauseHandler : IRequestHandler<PauseCommand>
```

### PlayHandler

Handles the command to play a track or playlist.

```csharp
public class PlayHandler : IRequestHandler<PlayCommand>
```

### ResumeHandler

Handles the command to resume the currently paused track.

```csharp
public class ResumeHandler : IRequestHandler<ResumeCommand>
```

### SkipHandler

Handles the command to skip the currently playing track.

```csharp
public class SkipHandler : IRequestHandler<SkipCommand>
```

### MessageReceivedHandler

Handles incoming messages and processes commands or statistics requests.

```csharp
public class MessageReceivedHandler : INotificationHandler<MessageReceivedNotification>
```

## Mermaid Diagrams

### Class Diagram

```mermaid
sequenceDiagram
    participant User as User
    participant DiscordSocketClient as DiscordSocketClient
    participant MessageReceivedHandler as MessageReceivedHandler
    participant MessageReceivedNotification as MessageReceivedNotification
    participant EmbedBuilder as EmbedBuilder
    participant Channel as Channel

    User->>DiscordSocketClient: Send message "!LunarisStats"
    DiscordSocketClient->>MessageReceivedHandler: MessageReceivedNotification
    MessageReceivedHandler->>MessageReceivedNotification: Handle(notification, cancellationToken)
    MessageReceivedNotification->>MessageReceivedHandler: BotMentioned(notification, cancellationToken)
    MessageReceivedHandler->>DiscordSocketClient: Get guilds and voice channels
    DiscordSocketClient-->>MessageReceivedHandler: List of guilds and voice channels
    MessageReceivedHandler->>EmbedBuilder: Create embed with statistics
    EmbedBuilder-->>MessageReceivedHandler: Embed
    MessageReceivedHandler->>Channel: Send embed message
```

### Sequence Diagram for PlayHandler

```mermaid
sequenceDiagram
    participant User
    participant Bot
    participant DiscordSocketClient
    participant IAudioService
    participant SocketSlashCommand
    participant LavalinkPlayer

    User->>Bot: /play [song]
    Bot->>DiscordSocketClient: Get user voice channel
    DiscordSocketClient-->>Bot: Voice channel info
    Bot->>IAudioService: Get or create player
    IAudioService-->>Bot: Player instance
    Bot->>SocketSlashCommand: Get search query
    SocketSlashCommand-->>Bot: Search query
    Bot->>IAudioService: Load tracks
    IAudioService-->>Bot: Track collection
    Bot->>LavalinkPlayer: Play track
    LavalinkPlayer-->>Bot: Track started
    Bot->>User: Now playing embed
```

### Sequence Diagram for MessageReceivedHandler

```mermaid
sequenceDiagram
    participant User
    participant Bot
    participant DiscordSocketClient
    participant ISender
    participant MessageReceivedNotification

    User->>Bot: Send message
    Bot->>MessageReceivedNotification: Create notification
    Bot->>DiscordSocketClient: Check if bot is mentioned
    DiscordSocketClient-->>Bot: Mention info
    alt Bot is mentioned
        Bot->>ISender: Send ChatCommand
    end
    Bot->>DiscordSocketClient: Check for statistics command
    alt Statistics command found
        Bot->>DiscordSocketClient: Get server and channel info
        DiscordSocketClient-->>Bot: Server and channel info
        Bot->>User: Send statistics embed
    end
```

This README provides an overview of the handlers and their responsibilities, along with class and sequence diagrams to illustrate the interactions and relationships between the components.
