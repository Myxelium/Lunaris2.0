# Lunaris2 - Discord Music Bot

Lunaris2 is a Discord bot designed to play music in your server's voice channels. It's built using C# and the Discord.Net library, and it uses the LavaLink music client for audio streaming.

## Features

- Play music from YouTube directly in your Discord server.
- Skip tracks, pause, and resume playback.
- Queue system to line up your favorite tracks.
- Local LLM (AI chatbot) that answers on @mentions in Discord chat. See more about it below.

## Setup

1. Clone the repo.
2. Extract.
3. If there isn't already a appsettings.json file in there, create one.
4. Set the discord bot token. How the file should look (without token): [appsettings.json](https://github.com/Myxelium/Lunaris2.0/blob/master/Bot/appsettings.json)]
5. Make sure you got docker installed. And run the file ``start-services.sh``, make sure you got git-bash installed.
6. Now you can start the project and run the application.

## LLM
Lunaris supports AI chat using a large language model, this is done by hosting the LLM locally, in this case Docker will set it up for you when you run the start-services script.

The LLM is run using Ollama see more about Ollama [here](https://ollama.com/). Running LLM locally requires much resources from your system, minimum requirements is at least 8GB of ram. If your don't have enought ram, select a LLM model in the [appsettings file](https://github.com/Myxelium/Lunaris2.0/blob/master/Bot/appsettings.json#L15) that requires less of your system.

*NOTE: you need to download the model from the Ollama ui, the model name which is preselected in the code is called ``gemma``.*

## PM2 Setup
- Install PM2 and configure it following their setup guide
#### Lavalink
* Download Lavalink 4.X.X (.jar)
* Install Java 17

If using Linux run following command to start Lavalink with PM2:
``pm2 start "sudo java -Xmx1G -jar Lavalink.jar" --name Lavalink4.0.7``

For me I have Lavalink.jar downloaded in ``/opt`` folder from Linux root. By running Lavalink using PM2, you can monitor it and manage it from a page in your browser instead of having to access the server terminal.
#### Lunaris
* Install dotnet

Register the Lunaris bot with PM2:
``pm2 start "dotnet Lunaris2.dll"``

## Usage

- `/play <song>`: Plays the specified song in the voice channel you're currently in.
- `/skip`: Skips the currently playing song.

## Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.
