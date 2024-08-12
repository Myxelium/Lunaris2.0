using Discord.WebSocket;

namespace Lunaris2.Service;

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
                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        });
    }

    private async Task CheckVoiceChannels()
    {
        foreach (var guild in _client.Guilds)
        {
            var voiceChannel = guild.VoiceChannels.FirstOrDefault(vc => vc.ConnectedUsers.Count == 1);
            if (voiceChannel != null)
            {
                if (!_timers.ContainsKey(voiceChannel.Id))
                {
                    _timers[voiceChannel.Id] = new Timer(async _ => await LeaveChannel(voiceChannel), null, TimeSpan.FromMinutes(3), Timeout.InfiniteTimeSpan);
                }
            }
            else
            {
                if (voiceChannel == null || !_timers.ContainsKey(voiceChannel.Id)) 
                    continue;
                
                await _timers[voiceChannel.Id].DisposeAsync();
                _timers.Remove(voiceChannel.Id);
            }
        }
    }

    private async Task LeaveChannel(SocketVoiceChannel voiceChannel)
    {
        if (voiceChannel.ConnectedUsers.Count == 1 && voiceChannel.Users.Any(u => u.Id == _client.CurrentUser.Id))
        {
            await voiceChannel.DisconnectAsync();
            await _timers[voiceChannel.Id].DisposeAsync();
            _timers.Remove(voiceChannel.Id);
        }
    }
}