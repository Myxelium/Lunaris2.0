# Lunaris2 - Discord Music Bot

Lunaris2 is a Discord bot designed to play music in your server's voice channels. It's built using C# and the Discord.Net library, and it uses the LavaLink music client for audio streaming.

## Features

- Play music from YouTube directly in your Discord server.
- Skip tracks, pause, and resume playback.
- Queue system to line up your favorite tracks.

## Setup

1. Clone the repo.
2. Extract.
3. If there isn't already a appsettings.json file in there, create one.
4. Set the discord bot token. How the file should look (without token): [appsettings.json](https://github.com/Myxelium/Lunaris2.0/blob/master/Bot/appsettings.json)]
5. Make sure you got docker installed. And run the file ``start-services.sh``, make sure you got git-bash installed.
6. Now you can start the project and run the application.

## Usage

- `/play <song>`: Plays the specified song in the voice channel you're currently in.
- `/skip`: Skips the currently playing song.

## Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

## License

[MIT](https://choosealicense.com/licenses/mit/)
