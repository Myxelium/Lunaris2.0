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
classDiagram
    class ClearQueueHandler {
        +Task Handle(ClearQueueCommand command, CancellationToken cancellationToken)
    }
    class DisconnectHandler {
        +Task Handle(DisconnectCommand command, CancellationToken cancellationToken)
    }
    class PauseHandler {
        +Task Handle(PauseCommand command, CancellationToken cancellationToken)
    }
    class PlayHandler {
        +Task Handle(PlayCommand command, CancellationToken cancellationToken)
    }
    class ResumeHandler {
        +Task Handle(ResumeCommand command, CancellationToken cancellationToken)
    }
    class SkipHandler {
        +Task Handle(SkipCommand command, CancellationToken cancellationToken)
    }
    class MessageReceivedHandler {
        +Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
    }
    class IAudioService
    class DiscordSocketClient
    class SocketSlashCommand
    class CancellationToken
    class Task
    class IRequestHandler
    class INotificationHandler

    ClearQueueHandler ..|> IRequestHandler
    DisconnectHandler ..|> IRequestHandler
    PauseHandler ..|> IRequestHandler
    PlayHandler ..|> IRequestHandler
    ResumeHandler ..|> IRequestHandler
    SkipHandler ..|> IRequestHandler
    MessageReceivedHandler ..|> INotificationHandler
    ClearQueueHandler --> IAudioService
    DisconnectHandler --> IAudioService
    PauseHandler --> IAudioService
    PlayHandler --> IAudioService
    ResumeHandler --> IAudioService
    SkipHandler --> IAudioService
    ClearQueueHandler --> DiscordSocketClient
    DisconnectHandler --> DiscordSocketClient
    PauseHandler --> DiscordSocketClient
    PlayHandler --> DiscordSocketClient
    ResumeHandler --> DiscordSocketClient
    SkipHandler --> DiscordSocketClient
    ClearQueueHandler --> SocketSlashCommand
    DisconnectHandler --> SocketSlashCommand
    PauseHandler --> SocketSlashCommand
    PlayHandler --> SocketSlashCommand
    ResumeHandler --> SocketSlashCommand
    SkipHandler --> SocketSlashCommand
    ClearQueueHandler --> CancellationToken
    DisconnectHandler --> CancellationToken
    PauseHandler --> CancellationToken
    PlayHandler --> CancellationToken
    ResumeHandler --> CancellationToken
    SkipHandler --> CancellationToken
    ClearQueueHandler --> Task
    DisconnectHandler --> Task
    PauseHandler --> Task
    PlayHandler --> Task
    ResumeHandler --> Task
    SkipHandler --> Task
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