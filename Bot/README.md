## How commands from Discord gets executed
```mermaid
flowchart TD
    Program[Program] -->|Register| EventListener
    Program --> Intervals[VoiceChannelMonitorService]
    Intervals --> SetStatus[SetStatus, Updates status with amount of playing bots]
    Intervals --> LeaveChannel[LeaveOnAlone, Leaves channel when alone for a time]
    EventListener[DiscordEventListener] --> A[MessageReceivedHandler]

    EventListener[DiscordEventListener] --> A2[SlashCommandReceivedHandler]

    A --> |Message| f{If bot is mentioned}
    A --> |Message '!LunarisStats'| p[Responds with Server and Channel Statistics.]
    f --> |ChatCommand| v[ChatHandler]

    A2[SlashCommandReceivedHandler] -->|Message| C{Send to correct command by 
            looking at commandName}

    C -->|JoinCommand| D[JoinHandler]
    C -->|PlayCommand| E[PlayHandler]
    C -->|PauseCommand| F[PauseHandler]
    C -->|DisconnectCommand| H[DisconnectHandler]
    C -->|ResumeCommand| J[ResumeHandler]
    C -->|SkipCommand| K[SkipHandler]
    C -->|ClearQueueCommand| L[ClearQueueHandler]
```
Program registers an event listener ```DiscordEventListener``` which publish a message :

```c#
await Mediator.Publish(new MessageReceivedNotification(arg), _cancellationToken);
```

|Name| Description |
|--|--|
| SlashCommandReceivedHandler | Handles commands using ``/`` from any Discord Guild/Server. |
| MessageReceivedHandler| Listens to **all** messages. |

## Handler integrations
```mermaid
flowchart LR
    D[JoinHandler] --> Disc[Discord Api]
    E[PlayHandler] --> Disc[Discord Api]
    F[SkipHandler] --> Disc[Discord Api]
    G[PauseHandler] --> Disc[Discord Api]
    v[ChatHandler] --> Disc[Discord Api]
    ClearQueueHandler --> Disc
    ClearQueuehandler --> Lava
    DisconnectHandler --> Disc
    Resumehandler --> Disc
    v --> o[Ollama Server]
    o --> v
    E --> Lava[Lavalink]
    F --> Lava
    G --> Lava
```
|Name| Description |
|--|--|
| JoinHandler| Handles the logic for **just** joining a voice channel. |
| PlayHandler| Handles the logic for joining and playing music in a voice channel. |
| PauseHandler | Handles the logic for pausing currently playing track. |
| DisconnectHandler | Handles the logic for disconnecting from voicechannels. |
| ClearQueueHandler | Handles the logic for clearing the queued songs, except the currently playing one. |
| SkipHandler | Handles the logic for skipping tracks that are queued. If 0 trackS is in queue, it stops the current one.|
| Resumehandler | Resumes paused tracks. |
| ChatHandler| Handles the logic for LLM chat with user. |




