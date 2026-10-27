namespace Lunaris2.Handler.Scheduler;

public sealed class SchedulerSettings
{
    public const string DashboardPath = "/hangfire";

    public bool Enabled { get; set; }

    public string ConnectionString { get; set; } = "";

    public string DashboardUrl { get; set; } = "http://127.0.0.1:5055";

    public string TimeZone { get; set; } = "UTC";

    public string ListenUrl =>
        string.IsNullOrWhiteSpace(DashboardUrl) ? "http://127.0.0.1:5055" : DashboardUrl.TrimEnd('/');

    public TimeZoneInfo ResolveTimeZone()
    {
        var id = string.IsNullOrWhiteSpace(TimeZone) ? "UTC" : TimeZone;
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception error) when (error is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new InvalidOperationException($"Scheduler:TimeZone '{id}' is not a valid time zone id.", error);
        }
    }
}
