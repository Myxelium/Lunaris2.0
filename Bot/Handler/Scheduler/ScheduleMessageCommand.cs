using System.Globalization;
using System.Text;
using Discord.WebSocket;
using Lunaris2.Handler.ChatCommand;
using Lunaris2.Handler.MusicPlayer;
using Lunaris2.SlashCommand;
using MediatR;
using Microsoft.Extensions.Options;
using OllamaSharp;

namespace Lunaris2.Handler.Scheduler;

public record ScheduleMessageCommand(SocketSlashCommand Message) : IRequest;

public sealed class ScheduleMessageHandler(
    IOptions<SchedulerSettings> schedulerSettings,
    IOptions<ChatSettings> chatSettings,
    IMessageScheduler scheduler) : IRequestHandler<ScheduleMessageCommand>
{
    private const int ModelAttempts = 3;

    public async Task Handle(ScheduleMessageCommand request, CancellationToken cancellationToken)
    {
        var command = request.Message;
        var settings = schedulerSettings.Value;
        if (!settings.Enabled)
        {
            await Reply(command, "Scheduling is turned off. Set Scheduler:Enabled to true in appsettings.json and restart the bot.");
            return;
        }

        var content = command.GetOptionValueByName(Option.Message);
        var whenText = command.GetOptionValueByName(Option.Time);
        if (string.IsNullOrWhiteSpace(content))
        {
            await Reply(command, "Tell me what the message should say.");
            return;
        }

        if (content.Length > 2000)
        {
            await Reply(command, "That message is too long. Discord messages can be at most 2000 characters.");
            return;
        }

        if (string.IsNullOrWhiteSpace(whenText))
        {
            await Reply(command, "Tell me when to send it.");
            return;
        }

        var channelId = command.ChannelId ?? command.Channel?.Id;
        if (channelId is null or 0)
        {
            await Reply(command, "I can only schedule messages in a channel.");
            return;
        }

        try
        {
            var zone = settings.ResolveTimeZone();
            if (IsRepeating(command))
                await ScheduleRecurring(command, zone, channelId.Value, content, whenText, cancellationToken);
            else
                await ScheduleOnce(command, zone, channelId.Value, content, whenText, cancellationToken);
        }
        catch (Exception error)
        {
            Console.WriteLine(error);
            try
            {
                await Reply(command, "I couldn't schedule that message.");
            }
            catch (Exception respondError)
            {
                Console.WriteLine(respondError);
            }
        }
    }

    private async Task ScheduleOnce(
        SocketSlashCommand command,
        TimeZoneInfo zone,
        ulong channelId,
        string content,
        string whenText,
        CancellationToken cancellationToken)
    {
        if (!ScheduleInput.TryGetInstant(whenText, zone, out var instant))
        {
            var generated = await AskUntil(DateInstruction(zone), whenText, text => ScheduleInput.TryGetInstant(text, zone, out _), cancellationToken);
            if (generated is null || !ScheduleInput.TryGetInstant(generated, zone, out instant))
            {
                await Reply(command, UnreadableTimeMessage());
                return;
            }
        }

        if (!ScheduleInput.TryGetDelay(instant, DateTimeOffset.UtcNow, out var delay, out _))
        {
            await Reply(command, "That time has already passed.");
            return;
        }

        scheduler.ScheduleOnce(channelId, content, delay);
        var local = TimeZoneInfo.ConvertTime(instant, zone);
        await Reply(command, $"Message scheduled for {local.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture)} ({zone.Id}).");
    }

    private async Task ScheduleRecurring(
        SocketSlashCommand command,
        TimeZoneInfo zone,
        ulong channelId,
        string content,
        string whenText,
        CancellationToken cancellationToken)
    {
        if (!ScheduleInput.TryGetCron(whenText, out var cron))
        {
            var generated = await AskUntil(CronInstruction(zone), whenText, text => ScheduleInput.TryGetCron(text, out _), cancellationToken);
            if (generated is null || !ScheduleInput.TryGetCron(generated, out cron))
            {
                await Reply(command, UnreadableCronMessage());
                return;
            }
        }

        var jobId = $"lunaris-{channelId}-{command.Id}";
        scheduler.ScheduleRecurring(jobId, channelId, content, cron, zone);
        await Reply(command, $"Repeating message scheduled ({cron}, {zone.Id}).");
    }

    private string UnreadableTimeMessage() =>
        IsModelConfigured()
            ? "I couldn't read that time. Use a timestamp like 27/10/2026 18:00:00."
            : "I couldn't read that time. Use a timestamp like 27/10/2026 18:00:00, or set LLM:Url and LLM:Model so I can interpret it.";

    private string UnreadableCronMessage() =>
        IsModelConfigured()
            ? "I couldn't read that schedule. Use a cron expression like 0 9 * * 1."
            : "I couldn't read that schedule. Use a cron expression like 0 9 * * 1, or set LLM:Url and LLM:Model so I can interpret it.";

    private bool IsModelConfigured()
    {
        var settings = chatSettings.Value;
        return !string.IsNullOrWhiteSpace(settings.Url) && !string.IsNullOrWhiteSpace(settings.Model);
    }

    private static string DateInstruction(TimeZoneInfo zone)
    {
        var now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone);
        return "You convert a time description into one timestamp. " +
               $"Current time is {now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture)} ({now.DayOfWeek}) in {zone.Id}. " +
               "Reply with only dd/MM/yyyy HH:mm:ss. No explanation.";
    }

    private static string CronInstruction(TimeZoneInfo zone)
    {
        var now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone);
        return "You convert a schedule description into a 5-field cron expression. " +
               $"Current time is {now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)} ({now.DayOfWeek}) in {zone.Id}. " +
               "Reply with only the cron expression, for example 0 9 * * 1. No explanation.";
    }

    private async Task<string?> AskUntil(string instruction, string userText, Func<string, bool> accept, CancellationToken cancellationToken)
    {
        if (!IsModelConfigured())
            return null;

        try
        {
            var settings = chatSettings.Value;
            using var client = new OllamaApiClient(settings.Url, settings.Model);
            var chat = new Chat(client);
            var prompt = instruction + "\n" + userText;
            for (var attempt = 0; attempt < ModelAttempts; attempt++)
            {
                var response = new StringBuilder();
                await foreach (var token in chat.SendAsync(prompt, cancellationToken))
                    response.Append(token);

                var text = response.ToString();
                if (accept(text))
                    return text;

                prompt = "That was not valid. Reply with only the requested value and nothing else.";
            }
        }
        catch (Exception error)
        {
            Console.WriteLine(error);
        }

        return null;
    }

    private static bool IsRepeating(SocketSlashCommand command)
    {
        var value = command.Data.Options.FirstOrDefault(option => option.Name == Option.IsRecurring)?.Value;
        return value is bool repeating && repeating;
    }

    private static Task Reply(SocketSlashCommand command, string content) =>
        command.ModifyOriginalResponseAsync(properties => properties.Content = content);
}
