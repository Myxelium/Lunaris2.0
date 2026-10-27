using Discord;
using Discord.WebSocket;
using Hangfire;

namespace Lunaris2.Handler.Scheduler;

public interface IMessageScheduler
{
    void ScheduleOnce(ulong channelId, string content, TimeSpan delay);

    void ScheduleRecurring(string jobId, ulong channelId, string content, string cron, TimeZoneInfo timeZone);
}

public sealed class DisabledMessageScheduler : IMessageScheduler
{
    public void ScheduleOnce(ulong channelId, string content, TimeSpan delay) =>
        throw new InvalidOperationException("Scheduler is disabled.");

    public void ScheduleRecurring(string jobId, ulong channelId, string content, string cron, TimeZoneInfo timeZone) =>
        throw new InvalidOperationException("Scheduler is disabled.");
}

public sealed class HangfireMessageScheduler(IBackgroundJobClient jobs, IRecurringJobManager recurring) : IMessageScheduler
{
    public void ScheduleOnce(ulong channelId, string content, TimeSpan delay)
    {
        jobs.Schedule<ScheduledMessageJob>(
            job => job.SendAsync(channelId, content, CancellationToken.None),
            delay);
    }

    public void ScheduleRecurring(string jobId, ulong channelId, string content, string cron, TimeZoneInfo timeZone)
    {
        recurring.AddOrUpdate<ScheduledMessageJob>(
            jobId,
            job => job.SendAsync(channelId, content, CancellationToken.None),
            cron,
            new RecurringJobOptions { TimeZone = timeZone });
    }
}

public sealed class ScheduledMessageJob(DiscordSocketClient client)
{
    public async Task SendAsync(ulong channelId, string content, CancellationToken cancellationToken)
    {
        if (client.ConnectionState != ConnectionState.Connected)
            throw new InvalidOperationException("Discord is not connected yet.");

        if (string.IsNullOrWhiteSpace(content))
            return;

        if (client.GetChannel(channelId) is not IMessageChannel channel)
        {
            Console.WriteLine($"Scheduled message skipped. Channel {channelId} is gone.");
            return;
        }

        if (content.Length > 2000)
            content = content[..1997] + "...";

        using var typing = channel.EnterTypingState();
        await channel.SendMessageAsync(content, options: new RequestOptions { CancelToken = cancellationToken });
    }
}
