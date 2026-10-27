using System.Globalization;
using Cronos;

namespace Lunaris2.Handler.Scheduler;

public static class ScheduleInput
{
    private static readonly string[] DateFormats =
    [
        "dd/MM/yyyy HH:mm:ss",
        "dd/MM/yyyy HH:mm",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd HH:mm"
    ];

    public static bool TryGetCron(string? text, out string cron)
    {
        cron = "";
        foreach (var candidate in Windows(text))
        {
            if (IsCron(candidate))
            {
                cron = candidate;
                return true;
            }
        }

        return false;
    }

    public static bool TryGetInstant(string? text, TimeZoneInfo timeZone, out DateTimeOffset instant)
    {
        instant = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var cleaned = text.Replace("```", " ", StringComparison.Ordinal);
        foreach (var candidate in DateCandidates(cleaned))
        {
            if (!DateTime.TryParseExact(candidate, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var unspecified))
                continue;

            if (timeZone.IsInvalidTime(unspecified))
                continue;

            var offset = timeZone.GetUtcOffset(unspecified);
            instant = new DateTimeOffset(unspecified, offset);
            return true;
        }

        return false;
    }

    public static bool TryGetDelay(DateTimeOffset instant, DateTimeOffset now, out TimeSpan delay, out bool alreadyPassed)
    {
        if (instant < now.AddSeconds(-30))
        {
            delay = default;
            alreadyPassed = true;
            return false;
        }

        delay = instant > now ? instant - now : TimeSpan.Zero;
        alreadyPassed = false;
        return true;
    }

    private static bool IsCron(string value) =>
        CronExpression.TryParse(value, CronFormat.Standard, out _)
        || CronExpression.TryParse(value, CronFormat.IncludeSeconds, out _);

    private static IEnumerable<string> Windows(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            yield break;

        var tokens = text
            .Replace("```", " ", StringComparison.Ordinal)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => token.Trim('`', '"', '\'', '.', ',', ';', ':'))
            .Where(token => token.Length > 0)
            .ToArray();

        if (tokens.Length is >= 5 and <= 6)
            yield return string.Join(' ', tokens);

        for (var length = 6; length >= 5; length--)
        {
            for (var start = 0; start + length <= tokens.Length; start++)
                yield return string.Join(' ', tokens.Skip(start).Take(length));
        }
    }

    private static IEnumerable<string> DateCandidates(string text)
    {
        var tokens = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var index = 0; index < tokens.Length - 1; index++)
        {
            var date = tokens[index].Trim('`', '"', '\'', '.', ',', ';', ':');
            var time = tokens[index + 1].Trim('`', '"', '\'', '.', ',', ';');
            if (date.Count(character => character == '/' || character == '-') == 2 && time.Contains(':'))
                yield return date + " " + time;
        }
    }
}
