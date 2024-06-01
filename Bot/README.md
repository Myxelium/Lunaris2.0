## How commands from Discord gets executed
```mermaid
flowchart TD
    Program[Program] -->|Register| EventListener
    EventListener[DiscordEventListener] --> A[MessageReceivedHandler]

    EventListener[DiscordEventListener] --> A2[SlashCommandReceivedHandler]

    A --> |Message| f{If bot is mentioned}
    f --> v[ChatHandler]

    A2[SlashCommandReceivedHandler] -->|Message| C{Send to correct command by 
            looking at commandName}

    C -->|JoinCommand| D[JoinHandler]
    C -->|PlayCommand| E[PlayHandler]
    C -->|HelloCommand| F[HelloHandler]
    C -->|GoodbyeCommand| G[GoodbyeHandler]
```
Program registers an event listener ```DiscordEventListener``` which publish a message :

```c#
await Mediator.Publish(new MessageReceivedNotification(arg), _cancellationToken);
```

## Handler integrations
```mermaid
flowchart TD
    D[JoinHandler] --> Disc[Discord Api]
    E[PlayHandler] --> Disc[Discord Api]
    F[HelloHandler] --> Disc[Discord Api]
    G[GoodbyeHandler] --> Disc[Discord Api]
    v[ChatHandler] --> Disc[Discord Api]
    v --> o[Ollama Server]
    o --> v
    E --> Lava[Lavalink]
```
