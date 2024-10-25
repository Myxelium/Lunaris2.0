using Discord;
using Discord.WebSocket;

namespace Lunaris2.Service
{
    public class VoiceChannelMonitorService
    {
        private readonly DiscordSocketClient _client;
        private readonly Dictionary<ulong, Timer> _timers = new();

        public VoiceChannelMonitorService(DiscordSocketClient client)
        {
            _client = client;
        }

        public void StartMonitoring()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await CheckVoiceChannels();
                    await Task.Delay(TimeSpan.FromMinutes(1)); // Monitor every minute
                }
            });
        }

        private async Task CheckVoiceChannels()
        {
            SetStatus();
            await LeaveOnAlone();
        }
        
        private void SetStatus()
        {
            var channels = _client.Guilds
                .SelectMany(guild => guild.VoiceChannels)
                .Count(channel => 
                    channel.ConnectedUsers
                        .Any(guildUser => guildUser.Id == _client.CurrentUser.Id) && 
                    channel.Users.Count > 1
                );
            
            if (channels == 0)
                _client.SetGameAsync(System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString(), type: ActivityType.CustomStatus);
            else if(channels == 1)
                _client.SetGameAsync("in 1 server", type: ActivityType.Playing);
            else if(channels > 1)
                _client.SetGameAsync($" in {channels} servers", type: ActivityType.Playing);
        }

        private async Task LeaveOnAlone()
        {
            foreach (var guild in _client.Guilds)
            {
                // Find voice channels where only the bot is left
                var voiceChannel = guild.VoiceChannels.FirstOrDefault(vc => 
                    vc.ConnectedUsers.Count == 1 && 
                    vc.Users.Any(u => u.Id == _client.CurrentUser.Id));

                if (voiceChannel != null)
                {
                    // If timer not set for this channel, start one
                    if (!_timers.ContainsKey(voiceChannel.Id))
                    {
                        Console.WriteLine($"Bot is alone in channel {voiceChannel.Name}, starting timer...");
                        _timers[voiceChannel.Id] = new Timer(async _ => await LeaveChannel(voiceChannel), null, 
                            TimeSpan.FromMinutes(3), Timeout.InfiniteTimeSpan);  // Set delay before leaving
                    }
                }
                else
                {
                    // Clean up timer if channel is no longer active
                    var timersToDispose = _timers.Where(t => guild.VoiceChannels.All(vc => vc.Id != t.Key)).ToList();
                    foreach (var timer in timersToDispose)
                    {
                        await timer.Value.DisposeAsync();
                        _timers.Remove(timer.Key);
                        Console.WriteLine($"Disposed timer for inactive voice channel ID: {timer.Key}");
                    }
                }
            }
        }

        private async Task LeaveChannel(SocketVoiceChannel voiceChannel)
        {
            if (voiceChannel.ConnectedUsers.Count == 1 && voiceChannel.Users.Any(u => u.Id == _client.CurrentUser.Id))
            {
                Console.WriteLine($"Leaving channel {voiceChannel.Name} due to inactivity...");
                await voiceChannel.DisconnectAsync();
                await _timers[voiceChannel.Id].DisposeAsync();
                _timers.Remove(voiceChannel.Id);  // Clean up after leaving
            }
        }
    }
}
