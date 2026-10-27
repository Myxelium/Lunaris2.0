using Discord;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Microsoft.Extensions.Hosting;

namespace Lunaris2.Service;

public sealed class VoiceChannelMonitorService : IHostedService
{
    public static readonly TimeSpan LeaveAfter = TimeSpan.FromMinutes(3);
    public static readonly TimeSpan StatusInterval = TimeSpan.FromMinutes(1);

    private readonly DiscordSocketClient _client;
    private readonly IAudioService _audioService;
    private readonly AloneLeaveScheduler _scheduler;
    private readonly SemaphoreSlim _checkGate = new(1, 1);
    private readonly CancellationTokenSource _stopping = new();
    private Task? _loop;

    public VoiceChannelMonitorService(
        DiscordSocketClient client,
        IAudioService audioService,
        AloneLeaveScheduler scheduler)
    {
        _client = client;
        _audioService = audioService;
        _scheduler = scheduler;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _client.UserVoiceStateUpdated += OnUserVoiceStateUpdated;
        _client.Ready += OnReady;
        _loop = Task.Run(() => MonitorAsync(_stopping.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _client.UserVoiceStateUpdated -= OnUserVoiceStateUpdated;
        _client.Ready -= OnReady;
        await _stopping.CancelAsync();
        _scheduler.CancelAll();

        if (_loop is not null)
        {
            try
            {
                await _loop.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    private Task OnReady()
    {
        _ = CheckVoiceChannelsAsync();
        return Task.CompletedTask;
    }

    private async Task MonitorAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await CheckVoiceChannelsAsync();

            try
            {
                await Task.Delay(StatusInterval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task CheckVoiceChannelsAsync()
    {
        if (!await _checkGate.WaitAsync(0))
            return;

        try
        {
            if (_client.CurrentUser is null)
                return;

            await SetStatusAsync();
            EnsureAloneStatesScheduled();
        }
        catch (Exception error)
        {
            Console.WriteLine(error);
        }
        finally
        {
            _checkGate.Release();
        }
    }

    private async Task SetStatusAsync()
    {
        var botId = _client.CurrentUser.Id;
        var channels = _client.Guilds
            .SelectMany(guild => guild.VoiceChannels)
            .Count(channel => IsPlayingForHumans(channel.ConnectedUsers, botId));

        if (channels == 0)
        {
            var version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString();
            await _client.SetGameAsync(version, type: ActivityType.CustomStatus);
        }
        else if (channels == 1)
        {
            await _client.SetGameAsync("in 1 server", type: ActivityType.Playing);
        }
        else
        {
            await _client.SetGameAsync($"in {channels} servers", type: ActivityType.Playing);
        }
    }

    private void EnsureAloneStatesScheduled()
    {
        var botId = _client.CurrentUser.Id;
        foreach (var guild in _client.Guilds)
        {
            foreach (var voiceChannel in guild.VoiceChannels)
            {
                if (ShouldLeave(voiceChannel.ConnectedUsers, botId))
                    ScheduleLeave(voiceChannel);
                else
                    _scheduler.Cancel(voiceChannel.Id);
            }
        }
    }

    private Task OnUserVoiceStateUpdated(SocketUser user, SocketVoiceState before, SocketVoiceState after)
    {
        if (_client.CurrentUser is null)
            return Task.CompletedTask;

        var botId = _client.CurrentUser.Id;
        UpdateChannel(before.VoiceChannel, botId);
        if (after.VoiceChannel?.Id != before.VoiceChannel?.Id)
            UpdateChannel(after.VoiceChannel, botId);

        return Task.CompletedTask;
    }

    private void UpdateChannel(SocketVoiceChannel? channel, ulong botId)
    {
        if (channel is null)
            return;

        if (ShouldLeave(channel.ConnectedUsers, botId))
            ScheduleLeave(channel);
        else
            _scheduler.Cancel(channel.Id);
    }

    private void ScheduleLeave(SocketVoiceChannel voiceChannel)
    {
        if (!_scheduler.TrySchedule(voiceChannel.Id, out var schedule) || schedule is null)
            return;

        var guildId = voiceChannel.Guild.Id;
        var channelId = voiceChannel.Id;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(LeaveAfter, schedule.Token);

                var channel = _client.GetGuild(guildId)?.GetVoiceChannel(channelId);
                var botId = _client.CurrentUser?.Id;
                if (channel is null || botId is null || !ShouldLeave(channel.ConnectedUsers, botId.Value))
                    return;

                Console.WriteLine($"Leaving {channel.Name} because nobody is listening.");
                await LeaveAsync(guildId, channel);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception error)
            {
                Console.WriteLine(error);
            }
            finally
            {
                _scheduler.CompleteIfCurrent(channelId, schedule);
            }
        });
    }

    private async Task LeaveAsync(ulong guildId, SocketVoiceChannel channel)
    {
        QueuedLavalinkPlayer? player = null;
        try
        {
            player = await _audioService.Players.GetPlayerAsync<QueuedLavalinkPlayer>(guildId);
        }
        catch (Exception error)
        {
            Console.WriteLine($"Lavalink player lookup failed: {error.Message}");
        }

        var current = _client.GetGuild(guildId)?.GetVoiceChannel(channel.Id) ?? channel;
        var botId = _client.CurrentUser?.Id;
        if (botId is null || !ShouldLeave(current.ConnectedUsers, botId.Value))
            return;

        if (player is not null)
        {
            try
            {
                await player.DisconnectAsync();
                return;
            }
            catch (Exception error)
            {
                Console.WriteLine($"Lavalink disconnect failed: {error.Message}");
            }
        }

        try
        {
            await current.DisconnectAsync();
        }
        catch (Exception error)
        {
            Console.WriteLine($"Discord disconnect failed: {error.Message}");
        }
    }

    private static bool ShouldLeave(IEnumerable<SocketGuildUser> users, ulong botId)
        => VoicePresence.ShouldLeave(users.Select(user => new VoiceMember(user.Id, user.IsBot)), botId);

    private static bool IsPlayingForHumans(IEnumerable<SocketGuildUser> users, ulong botId)
    {
        var members = users.Select(user => new VoiceMember(user.Id, user.IsBot)).ToArray();
        return members.Any(member => member.Id == botId) && !VoicePresence.ShouldLeave(members, botId);
    }
}
