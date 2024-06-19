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

## Usage

- `/play <song>`: Plays the specified song in the voice channel you're currently in.
- `/skip`: Skips the currently playing song.

## Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.
