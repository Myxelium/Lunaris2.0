## How commands from Discord gets executed
```mermaid
flowchart TD
    Program[Program] -->|Register| EventListener
    EventListener[DiscordEventListener] --> A
    A[MessageReceivedHandler] --> B(Message)
    B --> C{Send to correct command by 
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
