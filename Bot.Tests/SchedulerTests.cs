using System.Text.Json;
using Lunaris2.Handler.Scheduler;
using Lunaris2.Registration;
using Lunaris2.SlashCommand;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lunaris2.Tests;

public class ScheduleInputTests
{
    [Fact]
    public void Plain_cron_is_accepted()
    {
        Assert.True(ScheduleInput.TryGetCron("0 9 * * 1", out var cron));
        Assert.Equal("0 9 * * 1", cron);
    }

    [Fact]
    public void Cron_inside_a_sentence_is_extracted()
    {
        Assert.True(ScheduleInput.TryGetCron("use this cron:\n```\n0 9 * * 1\n```", out var cron));
        Assert.Equal("0 9 * * 1", cron);
    }

    [Fact]
    public void Punctuation_around_a_cron_expression_is_ignored()
    {
        Assert.True(ScheduleInput.TryGetCron("0 9 * * 1.", out var cron));
        Assert.Equal("0 9 * * 1", cron);
    }

    [Fact]
    public void Prose_is_not_a_cron_expression()
    {
        Assert.False(ScheduleInput.TryGetCron("every weekday morning", out _));
    }

    [Fact]
    public void Six_field_cron_is_accepted()
    {
        Assert.True(ScheduleInput.TryGetCron("0 0 9 * * *", out var cron));
        Assert.Equal("0 0 9 * * *", cron);
    }

    [Fact]
    public void Timestamp_uses_the_configured_time_zone()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm");

        Assert.True(ScheduleInput.TryGetInstant("the time is 27/10/2026 18:00:00.", zone, out var instant));

        Assert.Equal(new DateTimeOffset(2026, 10, 27, 17, 0, 0, TimeSpan.Zero), instant.ToUniversalTime());
    }

    [Fact]
    public void Iso_time_without_seconds_is_accepted()
    {
        Assert.True(ScheduleInput.TryGetInstant("2026-10-27 18:00", TimeZoneInfo.Utc, out var instant));
        Assert.Equal(new DateTimeOffset(2026, 10, 27, 18, 0, 0, TimeSpan.Zero), instant);
    }

    [Fact]
    public void Natural_language_is_not_a_timestamp()
    {
        Assert.False(ScheduleInput.TryGetInstant("tomorrow morning", TimeZoneInfo.Utc, out _));
    }

    [Theory]
    [InlineData(-120, false)]
    [InlineData(-10, true)]
    [InlineData(300, true)]
    public void Delay_rejects_only_times_that_are_already_past(int offsetSeconds, bool expected)
    {
        var now = new DateTimeOffset(2026, 10, 27, 18, 0, 0, TimeSpan.Zero);

        var accepted = ScheduleInput.TryGetDelay(now.AddSeconds(offsetSeconds), now, out var delay, out var alreadyPassed);

        Assert.Equal(expected, accepted);
        Assert.Equal(!expected, alreadyPassed);
        if (accepted)
            Assert.Equal(offsetSeconds > 0 ? TimeSpan.FromSeconds(offsetSeconds) : TimeSpan.Zero, delay);
    }
}

public class SchedulerFeatureTests
{
    [Fact]
    public void Scheduler_command_is_withheld_until_the_feature_is_enabled()
    {
        Assert.DoesNotContain(Command.Scheduler.Name, SlashCommandRegistration.RetainedCommandNames(false));
        Assert.Contains(Command.Scheduler.Name, SlashCommandRegistration.RetainedCommandNames(true));
        Assert.Contains(Command.Play.Name, SlashCommandRegistration.RetainedCommandNames(false));
    }

    [Fact]
    public void Disabled_scheduler_does_not_register_hangfire()
    {
        var services = new ServiceCollection();

        services.AddLunarisScheduler(new SchedulerSettings { Enabled = false });

        var registration = Assert.Single(services, service => service.ServiceType == typeof(IMessageScheduler));
        Assert.Equal(typeof(DisabledMessageScheduler), registration.ImplementationType);
        Assert.DoesNotContain(services, service => service.ServiceType.FullName?.Contains("Hangfire", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void Enabled_scheduler_requires_a_connection_string()
    {
        var services = new ServiceCollection();

        var error = Assert.Throws<InvalidOperationException>(() =>
            services.AddLunarisScheduler(new SchedulerSettings { Enabled = true, ConnectionString = "  " }));

        Assert.Contains("ConnectionString", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Enabled_scheduler_rejects_an_unknown_time_zone_before_touching_sql()
    {
        var services = new ServiceCollection();

        var error = Assert.Throws<InvalidOperationException>(() =>
            services.AddLunarisScheduler(new SchedulerSettings
            {
                Enabled = true,
                ConnectionString = "Server=localhost;Database=Hangfire;User Id=sa;Password=x;TrustServerCertificate=True",
                TimeZone = "Not/AZone"
            }));

        Assert.Contains("TimeZone", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Enabled_scheduler_registers_hangfire_without_opening_a_connection()
    {
        var services = new ServiceCollection();

        services.AddLunarisScheduler(new SchedulerSettings
        {
            Enabled = true,
            ConnectionString = "Server=localhost,1433;Database=Hangfire;User Id=sa;Password=ChangeThisPassword1!;TrustServerCertificate=True",
            TimeZone = "UTC"
        });

        var registration = Assert.Single(services, service => service.ServiceType == typeof(IMessageScheduler));
        Assert.Equal(typeof(HangfireMessageScheduler), registration.ImplementationType);
    }

    [Fact]
    public void Appsettings_ships_with_the_scheduler_turned_off()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(RepoFile("Bot/appsettings.json")));
        var scheduler = document.RootElement.GetProperty("Scheduler");

        Assert.False(scheduler.GetProperty("Enabled").GetBoolean());
        Assert.Equal("", scheduler.GetProperty("ConnectionString").GetString());
    }

    private static string RepoFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Lunaris2.sln")))
                return Path.Combine(directory.FullName, relativePath);

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Couldn't find the repository root.");
    }
}
