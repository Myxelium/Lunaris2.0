using Discord;
using Discord.WebSocket;

namespace Lunaris2.Service
{
    public class VoiceChannelMonitorService
    {
        private readonly DiscordSocketClient _client;
        // Track a cancellation source per voice channel when the bot is alone
        private readonly Dictionary<ulong, CancellationTokenSource> _leaveCtsByChannel = new();

        public VoiceChannelMonitorService(DiscordSocketClient client)
        {
            _client = client;
            // Subscribe to voice state updates to react immediately
            _client.UserVoiceStateUpdated += OnUserVoiceStateUpdated;
        }

        public void StartMonitoring()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await CheckVoiceChannels();
                    await Task.Delay(TimeSpan.FromMinutes(1)); // Status refresh every minute
                }
            });
        }

        private async Task CheckVoiceChannels()
        {
            SetStatus();
            await EnsureCurrentAloneStatesScheduled();
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

        // Monitor existing alone states during the periodic check to ensure timers exist
        private async Task EnsureCurrentAloneStatesScheduled()
        {
            foreach (var guild in _client.Guilds)
            {
                foreach (var voiceChannel in guild.VoiceChannels)
                {
                    var botInChannel = voiceChannel.ConnectedUsers.Any(u => u.Id == _client.CurrentUser.Id);
                    var userCount = voiceChannel.ConnectedUsers.Count;

                    if (botInChannel && userCount == 1)
                    {
                        // Schedule leave if not already scheduled
                        if (!_leaveCtsByChannel.ContainsKey(voiceChannel.Id))
                        {
                            ScheduleLeave(voiceChannel);
                        }
                    }
                    else
                    {
                        // Cancel if a schedule exists but the bot is not alone anymore
                        if (_leaveCtsByChannel.TryGetValue(voiceChannel.Id, out var cts))
                        {
                            cts.Cancel();
                            _leaveCtsByChannel.Remove(voiceChannel.Id);
                        }
                    }
                }
            }

            await Task.CompletedTask;
        }

        private Task OnUserVoiceStateUpdated(SocketUser user, SocketVoiceState before, SocketVoiceState after)
        {
            // React only when events relate to the guild(s) and voice channels where the bot might be
            var botId = _client.CurrentUser.Id;

            // Determine affected channels
            var beforeChannelId = before.VoiceChannel?.Id;
            var afterChannelId = after.VoiceChannel?.Id;

            // If the bot itself moved, we should cancel any old schedule and possibly set a new one
            if (user.Id == botId)
            {
                if (beforeChannelId.HasValue && _leaveCtsByChannel.TryGetValue(beforeChannelId.Value, out var oldCts))
                {
                    oldCts.Cancel();
                    _leaveCtsByChannel.Remove(beforeChannelId.Value);
                }

                if (afterChannelId.HasValue)
                {
                    var channel = after.VoiceChannel!;
                    var isAlone = channel.ConnectedUsers.Count == 1 && channel.ConnectedUsers.Any(u => u.Id == botId);
                    if (isAlone && !_leaveCtsByChannel.ContainsKey(channel.Id))
                    {
                        ScheduleLeave(channel);
                    }
                }

                return Task.CompletedTask;
            }

            // For other users, if they join the bot's channel, cancel the leave; if they leave and bot becomes alone, schedule leave
            if (afterChannelId.HasValue)
            {
                var channel = after.VoiceChannel!;
                var botInChannel = channel.ConnectedUsers.Any(u => u.Id == botId);
                var userCount = channel.ConnectedUsers.Count;

                if (botInChannel && userCount > 1)
                {
                    // Cancel any pending leave
                    if (_leaveCtsByChannel.TryGetValue(channel.Id, out var cts))
                    {
                        cts.Cancel();
                        _leaveCtsByChannel.Remove(channel.Id);
                    }
                }
            }

            if (beforeChannelId.HasValue)
            {
                var channel = before.VoiceChannel!; // user left this channel
                var botInChannel = channel.ConnectedUsers.Any(u => u.Id == botId);
                var userCount = channel.ConnectedUsers.Count;

                if (botInChannel && userCount == 1)
                {
                    // Bot became alone, schedule leave
                    if (!_leaveCtsByChannel.ContainsKey(channel.Id))
                    {
                        ScheduleLeave(channel);
                    }
                }
            }

            return Task.CompletedTask;
        }

        private void ScheduleLeave(SocketVoiceChannel voiceChannel)
        {
            var cts = new CancellationTokenSource();
            _leaveCtsByChannel[voiceChannel.Id] = cts;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(3), cts.Token);

                    // After delay, verify still alone
                    var botId = _client.CurrentUser.Id;
                    var isStillAlone = voiceChannel.ConnectedUsers.Count == 1 && voiceChannel.ConnectedUsers.Any(u => u.Id == botId);
                    if (isStillAlone)
                    {
                        Console.WriteLine($"Leaving channel {voiceChannel.Name} due to inactivity...");
                        await voiceChannel.DisconnectAsync();
                    }
                }
                catch (OperationCanceledException)
                {
                    // Cancelled because someone joined or bot moved
                }
                finally
                {
                    _leaveCtsByChannel.Remove(voiceChannel.Id);
                    cts.Dispose();
                }
            });
        }
    }
}
